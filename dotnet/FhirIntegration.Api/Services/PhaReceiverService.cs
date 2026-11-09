using System.Text;
using System.Text.Json.Nodes;
using FhirIntegration.Api.Models;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;

namespace FhirIntegration.Api.Services;

/// <summary>
/// Mock Public Health Agency (PHA) endpoint that receives eICR message bundles from eCRNow.
///
/// Persists the raw bundle to OutputDirectory/eicr/, logs MessageHeader event + patient context,
/// and answers with a minimal "message" Bundle whose MessageHeader.response.code = ok — a
/// simplified Reportability Response acknowledgement.
/// </summary>
public sealed class PhaReceiverService
{
    private readonly OutputPaths _paths;
    private readonly FhirSerialization _fhir;
    private readonly FhirOptions _options;
    private readonly ILogger<PhaReceiverService> _logger;

    public PhaReceiverService(OutputPaths paths, FhirSerialization fhir, IOptions<FhirOptions> options, ILogger<PhaReceiverService> logger)
    {
        _paths = paths;
        _fhir = fhir;
        _options = options.Value;
        _logger = logger;
    }

    public sealed record ReceiveResult(string FileName, Bundle Acknowledgement, string? BundleId, string? Event, string? PatientSummary);

    public async Task<ReceiveResult> ReceiveAsync(string rawBody, CancellationToken ct)
    {
        // Parse leniently: we want to persist whatever arrived even if it is not perfect FHIR.
        Bundle? bundle = null;
        string? parseError = null;
        try { bundle = _fhir.Parse<Bundle>(rawBody); }
        catch (Exception ex) { parseError = ex.Message; }

        var bundleId = bundle?.Id ?? TryReadId(rawBody) ?? Guid.NewGuid().ToString("N")[..8];
        var fileName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{SanitizeForFile(bundleId)}.json";
        await File.WriteAllTextAsync(Path.Combine(_paths.Eicr, fileName), rawBody, new UTF8Encoding(false), ct);

        var header = bundle?.Entry.Select(e => e.Resource).OfType<MessageHeader>().FirstOrDefault();
        var patient = bundle?.Entry.Select(e => e.Resource).OfType<Patient>().FirstOrDefault();
        var eventDescription = header?.Event switch
        {
            Coding c => $"{c.System}|{c.Code}",
            FhirUri u => u.Value,
            _ => null
        };
        var patientSummary = patient is null ? null
            : $"Patient/{patient.Id} {string.Join(' ', patient.Name.FirstOrDefault()?.Given ?? Array.Empty<string>())} {patient.Name.FirstOrDefault()?.Family} ({patient.Gender}, {patient.BirthDate})".Trim();

        if (parseError is not null)
            _logger.LogWarning("PHA received a body that is not a parseable FHIR Bundle ({Error}); stored as {File}", parseError, fileName);
        else
            _logger.LogInformation("PHA received eICR bundle {BundleId} (type {Type}, {Entries} entries) event={Event} patient={Patient} -> {File}",
                bundleId, bundle?.Type, bundle?.Entry.Count, eventDescription ?? "n/a", patientSummary ?? "n/a", fileName);

        var ack = BuildAcknowledgement(header, bundleId, parseError);
        return new ReceiveResult(fileName, ack, bundleId, eventDescription, patientSummary);
    }

    /// <summary>
    /// Minimal R4 CapabilityStatement describing this receiver as a FHIR server that supports the
    /// $process-message operation (what eCRNow's FHIR client expects to find at {base}/metadata).
    /// </summary>
    public CapabilityStatement BuildCapabilityStatement()
    {
        var baseUrl = $"{_options.PublicBaseUrl.TrimEnd('/')}/pha/r4/fhir";
        return new CapabilityStatement
        {
            Id = "pha-receiver",
            Status = PublicationStatus.Active,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Kind = CapabilityStatementKind.Instance,
            Publisher = "FhirIntegration.Api PHA receiver mock",
            Software = new CapabilityStatement.SoftwareComponent { Name = "FhirIntegration.Api", Version = "1.0" },
            Implementation = new CapabilityStatement.ImplementationComponent { Description = "Mock Public Health Agency eICR receiver", Url = baseUrl },
            FhirVersion = FHIRVersion.N4_0_1,
            Format = new[] { "json", "application/fhir+json" },
            Rest =
            {
                new CapabilityStatement.RestComponent
                {
                    Mode = CapabilityStatement.RestfulCapabilityMode.Server,
                    Documentation = "Accepts eICR message Bundles via $process-message and returns a Reportability Response acknowledgement.",
                    Operation =
                    {
                        new CapabilityStatement.OperationComponent
                        {
                            Name = "process-message",
                            Definition = "http://hl7.org/fhir/OperationDefinition/MessageHeader-process-message"
                        }
                    }
                }
            }
        };
    }

    /// <summary>Lists the eICR files received so far, newest first.</summary>
    public IReadOnlyList<ReceivedEicr> ListReceived() =>
        new DirectoryInfo(_paths.Eicr).EnumerateFiles("*.json")
            .OrderByDescending(f => f.CreationTimeUtc)
            .Select(f => new ReceivedEicr(f.Name, f.Length, f.CreationTimeUtc, $"{_options.PublicBaseUrl.TrimEnd('/')}/pha/received/{f.Name}"))
            .ToList();

    public string? ReadReceived(string fileName)
    {
        var path = Path.Combine(_paths.Eicr, OutputPaths.SafeSegment(fileName));
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    private Bundle BuildAcknowledgement(MessageHeader? incoming, string incomingBundleId, string? parseError)
    {
        var responseHeader = new MessageHeader
        {
            Id = Guid.NewGuid().ToString(),
            Event = new Coding("http://hl7.org/fhir/us/ecr/CodeSystem/us-ph-messageheader-message-types", "reportability-response", "Reportability Response"),
            Source = new MessageHeader.MessageSourceComponent { Name = "FhirIntegration PHA mock", Endpoint = $"{_options.PublicBaseUrl.TrimEnd('/')}/pha/r4/fhir/$process-message" },
            Response = new MessageHeader.ResponseComponent
            {
                Identifier = incoming?.Id ?? incomingBundleId,
                Code = parseError is null ? MessageHeader.ResponseType.Ok : MessageHeader.ResponseType.FatalError
            }
        };
        if (incoming?.Source?.Endpoint is { } endpoint)
            responseHeader.Destination.Add(new MessageHeader.MessageDestinationComponent { Endpoint = endpoint, Name = incoming.Source.Name });

        var ack = new Bundle
        {
            Id = Guid.NewGuid().ToString(),
            Type = Bundle.BundleType.Message,
            Timestamp = DateTimeOffset.UtcNow,
            Entry = { new Bundle.EntryComponent { FullUrl = $"urn:uuid:{responseHeader.Id}", Resource = responseHeader } }
        };

        if (parseError is not null)
        {
            var outcome = new OperationOutcome
            {
                Id = Guid.NewGuid().ToString(),
                Issue = { new OperationOutcome.IssueComponent { Severity = OperationOutcome.IssueSeverity.Error, Code = OperationOutcome.IssueType.Structure, Diagnostics = parseError } }
            };
            responseHeader.Response.Details = new ResourceReference($"urn:uuid:{outcome.Id}");
            ack.Entry.Add(new Bundle.EntryComponent { FullUrl = $"urn:uuid:{outcome.Id}", Resource = outcome });
        }
        return ack;
    }

    private static string? TryReadId(string raw)
    {
        try { return JsonNode.Parse(raw)?["id"]?.GetValue<string>(); } catch { return null; }
    }

    private static string SanitizeForFile(string value)
    {
        var cleaned = new string(value.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.').ToArray());
        return cleaned.Length == 0 ? "bundle" : cleaned[..Math.Min(cleaned.Length, 60)];
    }
}
