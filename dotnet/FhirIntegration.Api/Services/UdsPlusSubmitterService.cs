using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FhirIntegration.Api.Models;
using Microsoft.Extensions.Options;

namespace FhirIntegration.Api.Services;

/// <summary>
/// Orchestrates the UDS+ "Data Submitter" actor end-to-end:
///
///   HAPI (Data Source) --$export--> [this: Submitter] --de-identify (Trust Service Provider role)-->
///   NDJSON files + Import Manifest --validate (HL7 validator, UDS+ IG)--> --$import--> Data Receiver (mock)
///
/// Each run is written to OutputDirectory/udsplus/{runId}/ so reviewers can inspect every artefact:
///   source/{Type}.ndjson      identifiable export as received from HAPI
///   {Type}.ndjson             de-identified, UDS+-profiled resources (served at /udsplus/files/{runId}/{file})
///   manifest.json             UDS+ Import Manifest (Parameters)
///   validation-report.json    raw validator output (when validate=true)
///   run-summary.json          the same object returned by the API
/// </summary>
public sealed class UdsPlusSubmitterService
{
    private readonly BulkDataExportClient _export;
    private readonly DeIdentifier _deIdentifier;
    private readonly UdsPlusManifestBuilder _manifestBuilder;
    private readonly ValidatorClient _validator;
    private readonly FhirSerialization _fhir;
    private readonly OutputPaths _paths;
    private readonly IHttpClientFactory _httpFactory;
    private readonly FhirOptions _fhirOptions;
    private readonly UdsPlusOptions _udsOptions;
    private readonly ILogger<UdsPlusSubmitterService> _logger;

    public UdsPlusSubmitterService(
        BulkDataExportClient export,
        DeIdentifier deIdentifier,
        UdsPlusManifestBuilder manifestBuilder,
        ValidatorClient validator,
        FhirSerialization fhir,
        OutputPaths paths,
        IHttpClientFactory httpFactory,
        IOptions<FhirOptions> fhirOptions,
        IOptions<UdsPlusOptions> udsOptions,
        ILogger<UdsPlusSubmitterService> logger)
    {
        _export = export;
        _deIdentifier = deIdentifier;
        _manifestBuilder = manifestBuilder;
        _validator = validator;
        _fhir = fhir;
        _paths = paths;
        _httpFactory = httpFactory;
        _fhirOptions = fhirOptions.Value;
        _udsOptions = udsOptions.Value;
        _logger = logger;
    }

    public async Task<UdsPlusSubmitResult> SubmitAsync(string groupId, bool validate, bool submit, CancellationToken ct)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var runId = $"{startedAt:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..40];
        var runDir = _paths.RunDirectory(runId);
        var sourceDir = Path.Combine(runDir, "source");
        var notes = new List<string>();
        var sw = Stopwatch.StartNew();

        // ---- a. Bulk export from the Data Source --------------------------------------------
        var export = await _export.ExportGroupAsync(groupId, _udsOptions.EffectiveExportResourceTypes, ct);
        Directory.CreateDirectory(sourceDir); // only materialise the run folder once the export succeeded
        var exportedCounts = new Dictionary<string, int>();
        var byType = new Dictionary<string, List<JsonObject>>();
        foreach (var file in export.Files)
        {
            if (!byType.TryGetValue(file.Type, out var list)) byType[file.Type] = list = new List<JsonObject>();
            list.AddRange(file.Resources);
        }
        foreach (var (type, resources) in byType)
        {
            exportedCounts[type] = resources.Count;
            await File.WriteAllLinesAsync(Path.Combine(sourceDir, $"{type}.ndjson"), resources.Select(r => r.ToJsonString()), ct);
        }
        _logger.LogInformation("Run {RunId}: exported {Summary} in {Elapsed:F1}s", runId, Summarise(exportedCounts), sw.Elapsed.TotalSeconds);

        // ---- b. De-identify (Patients first so ages/pseudonyms are known) -----------------
        var context = new DeIdentifier.Context { ReportDate = startedAt.UtcDateTime.Date };
        var deidentified = new Dictionary<string, List<JsonObject>>();
        foreach (var type in byType.Keys.OrderBy(t => t == "Patient" ? 0 : 1).ThenBy(t => t))
        {
            var output = new List<JsonObject>();
            foreach (var resource in byType[type])
            {
                try
                {
                    var clone = resource.DeepClone().AsObject();
                    var deidentifiedResource = _deIdentifier.DeIdentify(clone, context);
                    // Round-trip through the Firely parser to prove the output is still well-formed FHIR.
                    _fhir.Parse(deidentifiedResource.ToJsonString());
                    output.Add(deidentifiedResource);
                }
                catch (Exception ex)
                {
                    notes.Add($"{type}/{resource["id"]}: skipped ({ex.GetType().Name}: {ex.Message})");
                    _logger.LogWarning(ex, "De-identification of {Type}/{Id} failed", type, resource["id"]);
                }
            }
            deidentified[type] = output;
        }
        notes.AddRange(context.Notes);
        var deidentifiedCounts = deidentified.ToDictionary(kv => kv.Key, kv => kv.Value.Count);

        // ---- c. Write NDJSON files, served read-only by /udsplus/files/{runId}/{file} -------
        var fileUrls = new List<string>();
        var manifestInputs = new List<UdsPlusManifestBuilder.ManifestInput>();
        foreach (var (type, resources) in deidentified.Where(kv => kv.Value.Count > 0).OrderBy(kv => kv.Key))
        {
            var fileName = $"{type}.ndjson";
            await File.WriteAllLinesAsync(Path.Combine(runDir, fileName), resources.Select(r => r.ToJsonString()), new UTF8Encoding(false), ct);
            var url = FileUrl(runId, fileName);
            fileUrls.Add(url);
            manifestInputs.Add(new UdsPlusManifestBuilder.ManifestInput(type, url));
        }

        // ---- d. Import manifest ------------------------------------------------------------
        var manifest = _manifestBuilder.Build(runId, startedAt, manifestInputs, bearerToken: null);
        var manifestJson = _fhir.ToJson(manifest);
        await File.WriteAllTextAsync(Path.Combine(runDir, "manifest.json"), manifestJson, new UTF8Encoding(false), ct);
        var manifestUrl = FileUrl(runId, "manifest.json");

        // ---- e. Validate against the UDS+ IG -----------------------------------------------
        ValidationSummary? validation = null;
        if (validate)
        {
            validation = await ValidateRunAsync(runId, runDir, deidentified, manifestJson, ct);
        }

        // ---- f. Submit the manifest to the Data Receiver ($import) -------------------------
        UpstreamResponse? importResponse = null;
        JsonNode? importStatus = null;
        if (submit)
        {
            (importResponse, importStatus) = await SubmitToReceiverAsync(manifestJson, ct);
        }

        var completedAt = DateTimeOffset.UtcNow;
        var result = new UdsPlusSubmitResult(runId, groupId, startedAt, completedAt, exportedCounts, deidentifiedCounts,
            fileUrls, manifestUrl, validation, importResponse, importStatus, notes);

        await File.WriteAllTextAsync(Path.Combine(runDir, "run-summary.json"),
            JsonSerializer.Serialize(result, FhirSerialization.ApiJsonOptions), new UTF8Encoding(false), ct);

        _logger.LogInformation("Run {RunId} complete in {Elapsed:F1}s", runId, sw.Elapsed.TotalSeconds);
        return result;
    }

    private async Task<ValidationSummary> ValidateRunAsync(string runId, string runDir, Dictionary<string, List<JsonObject>> deidentified, string manifestJson, CancellationToken ct)
    {
        var files = new List<ValidatorClient.FileToValidate> { new("manifest.json", manifestJson) };
        foreach (var (type, resources) in deidentified.OrderBy(kv => kv.Key))
        {
            foreach (var resource in resources.Take(_udsOptions.MaxResourcesToValidatePerType))
                files.Add(new ValidatorClient.FileToValidate($"{type}-{resource["id"]?.GetValue<string>()}.json", resource.ToJsonString()));
        }

        var reportUrl = FileUrl(runId, "validation-report.json");
        try
        {
            var result = await _validator.ValidateAsync(files, _udsOptions.EffectiveValidatorIgs, ct);
            await File.WriteAllTextAsync(Path.Combine(runDir, "validation-report.json"),
                FhirSerialization.PrettyPrint(result.RawResponse), new UTF8Encoding(false), ct);

            var perFile = result.Outcomes.Select(o => new ValidationFileSummary(o.FileName, o.Errors, o.Warnings, o.Information)).ToList();
            return new ValidationSummary(result.SessionId, perFile.Sum(f => f.Errors), perFile.Sum(f => f.Warnings), perFile.Sum(f => f.Information), perFile, reportUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed for run {RunId}", runId);
            await File.WriteAllTextAsync(Path.Combine(runDir, "validation-report.json"),
                JsonSerializer.Serialize(new { error = ex.Message }, FhirSerialization.ApiJsonOptions), ct);
            return new ValidationSummary(null, 0, 0, 0, Array.Empty<ValidationFileSummary>(), reportUrl, ex.Message);
        }
    }

    /// <summary>POSTs the manifest to our own /udsplus/$import (mock HRSA receiver) and follows Content-Location.</summary>
    private async Task<(UpstreamResponse, JsonNode?)> SubmitToReceiverAsync(string manifestJson, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient(HttpClientNames.Self);
        var importUrl = $"{_fhirOptions.PublicBaseUrl.TrimEnd('/')}/udsplus/$import";

        using var content = new StringContent(manifestJson, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue(FhirSerialization.FhirJsonMediaType);
        using var response = await http.PostAsync(importUrl, content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        JsonNode? bodyNode = TryParse(body);

        var statusUrl = BulkDataExportClient.ContentLocation(response);
        var upstream = new UpstreamResponse((int)response.StatusCode, response.Content.Headers.ContentType?.ToString(), bodyNode, bodyNode is null ? body : null, importUrl);

        JsonNode? status = null;
        if (statusUrl is not null)
        {
            using var statusResponse = await http.GetAsync(statusUrl, ct);
            status = TryParse(await statusResponse.Content.ReadAsStringAsync(ct));
        }
        return (upstream, status);
    }

    private string FileUrl(string runId, string fileName) => $"{_fhirOptions.PublicBaseUrl.TrimEnd('/')}/udsplus/files/{runId}/{fileName}";

    private static JsonNode? TryParse(string body)
    {
        try { return string.IsNullOrWhiteSpace(body) ? null : JsonNode.Parse(body); }
        catch (JsonException) { return null; }
    }

    private static string Summarise(Dictionary<string, int> counts) => string.Join(", ", counts.Select(kv => $"{kv.Key}={kv.Value}"));
}
