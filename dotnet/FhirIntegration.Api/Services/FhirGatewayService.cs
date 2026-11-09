using FhirIntegration.Api.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Utility;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task; // Hl7.Fhir.Model also defines a Task resource

namespace FhirIntegration.Api.Services;

/// <summary>
/// Gateway / facade over the HAPI FHIR server, built on the Firely <see cref="FhirClient"/>.
///
/// Architecture note: consumers of this API never talk to HAPI directly. This service owns the
/// FHIR REST semantics (search parameters, operations such as $everything / $validate,
/// transaction bundles, profile tagging) and exposes either simplified DTOs or raw FHIR to the
/// endpoint layer. Swapping HAPI for another R4 server is a configuration change.
///
/// Lifetime: scoped. A <see cref="FhirClient"/> is cheap to construct when it reuses the pooled
/// <see cref="HttpClient"/> from <see cref="IHttpClientFactory"/>.
/// </summary>
public sealed class FhirGatewayService
{
    public const string UsCorePatientProfile = "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient";
    public const string UsCoreEncounterProfile = "http://hl7.org/fhir/us/core/StructureDefinition/us-core-encounter";
    private const string MrnTypeSystem = "http://terminology.hl7.org/CodeSystem/v2-0203";
    private const string DemoMrnSystem = "urn:oid:1.2.36.146.595.217.0.1";
    private const string ActCodeSystem = "http://terminology.hl7.org/CodeSystem/v3-ActCode";

    private readonly FhirClient _client;
    private readonly FhirOptions _options;
    private readonly ILogger<FhirGatewayService> _logger;

    public FhirGatewayService(IHttpClientFactory httpClientFactory, IOptions<FhirOptions> options, ILogger<FhirGatewayService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json,
            ReturnPreference = ReturnPreference.Representation,
            VerifyFhirVersion = false,
            Timeout = 120_000
        };
        _client = new FhirClient(new Uri(_options.BaseUrl), httpClientFactory.CreateClient(HttpClientNames.Hapi), settings);
    }

    public string BaseUrl => _options.BaseUrl;

    // ------------------------------------------------------------------ metadata

    public async Task<CapabilitySummary> GetCapabilitySummaryAsync(CancellationToken ct)
    {
        var cs = await _client.CapabilityStatementAsync() ?? throw new InvalidOperationException("HAPI returned no CapabilityStatement.");
        var rest = cs.Rest.FirstOrDefault();
        var types = rest?.Resource.Select(r => r.Type?.ToString() ?? "").Where(t => t.Length > 0).OrderBy(t => t).ToList() ?? new List<string>();

        return new CapabilitySummary(
            cs.FhirVersion?.GetLiteral(),
            cs.Software?.Name,
            cs.Software?.Version,
            types.Count,
            types,
            _options.BaseUrl);
    }

    // ------------------------------------------------------------------ patients

    public async Task<IReadOnlyList<PatientSummaryDto>> SearchPatientsAsync(string? family, string? given, string? identifier, int count, CancellationToken ct)
    {
        var q = new SearchParams().LimitTo(count);
        if (!string.IsNullOrWhiteSpace(family)) q.Add("family", family);
        if (!string.IsNullOrWhiteSpace(given)) q.Add("given", given);
        if (!string.IsNullOrWhiteSpace(identifier)) q.Add("identifier", identifier);

        var bundle = await _client.SearchAsync<Patient>(q);
        return bundle?.Entry.Select(e => e.Resource).OfType<Patient>().Select(ToSummary).ToList() ?? new List<PatientSummaryDto>();
    }

    public Task<Patient?> GetPatientAsync(string id, CancellationToken ct) => _client.ReadAsync<Patient>($"Patient/{id}");

    /// <summary>Builds a US Core-profiled Patient from the simplified DTO and creates it on HAPI.</summary>
    public async Task<ResourceWriteResult> CreatePatientAsync(CreatePatientRequest request, CancellationToken ct)
    {
        var patient = new Patient
        {
            Meta = new Meta { Profile = new[] { UsCorePatientProfile } },
            Active = true,
            Name = { new HumanName { Use = HumanName.NameUse.Official, Family = request.Family, Given = new[] { request.Given } } },
            Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : EnumUtility.ParseLiteral<AdministrativeGender>(request.Gender.ToLowerInvariant()),
            BirthDate = string.IsNullOrWhiteSpace(request.BirthDate) ? null : request.BirthDate
        };

        if (!string.IsNullOrWhiteSpace(request.Mrn))
        {
            patient.Identifier.Add(new Identifier(DemoMrnSystem, request.Mrn)
            {
                Use = Identifier.IdentifierUse.Usual,
                Type = new CodeableConcept(MrnTypeSystem, "MR", "Medical record number")
            });
        }

        var created = await _client.CreateAsync(patient) ?? throw new InvalidOperationException("HAPI did not return the created Patient.");
        _logger.LogInformation("Created Patient/{Id} (version {Version})", created.Id, created.Meta?.VersionId);
        return ToWriteResult(created);
    }

    public async Task<ResourceWriteResult> UpdatePatientAsync(string id, string patientJson, FhirSerialization fhir, CancellationToken ct)
    {
        var patient = fhir.Parse<Patient>(patientJson);
        patient.Id = id; // the URL wins over the body, per FHIR update semantics
        var updated = await _client.UpdateAsync(patient) ?? throw new InvalidOperationException("HAPI did not return the updated Patient.");
        return ToWriteResult(updated);
    }

    public Task DeletePatientAsync(string id, CancellationToken ct) => _client.DeleteAsync($"Patient/{id}");

    // ------------------------------------------------------------------ patient-scoped searches

    public Task<Bundle?> SearchEncountersAsync(string patientId, CancellationToken ct) =>
        _client.SearchAsync<Encounter>(new SearchParams().Add("patient", patientId).LimitTo(100));

    public Task<Bundle?> SearchConditionsAsync(string patientId, CancellationToken ct) =>
        _client.SearchAsync<Condition>(new SearchParams().Add("patient", patientId).LimitTo(100));

    public Task<Bundle?> SearchObservationsAsync(string patientId, string? category, CancellationToken ct)
    {
        var q = new SearchParams().Add("patient", patientId).LimitTo(100);
        if (!string.IsNullOrWhiteSpace(category)) q.Add("category", category);
        return _client.SearchAsync<Observation>(q);
    }

    /// <summary>
    /// Calls Patient/{id}/$everything and follows "next" links so the caller gets the complete
    /// patient compartment in a single Bundle.
    /// </summary>
    public async Task<Bundle> GetEverythingAsync(string patientId, int maxPages, CancellationToken ct)
    {
        var result = await _client.InstanceOperationAsync(new Uri($"Patient/{patientId}", UriKind.Relative), "everything") as Bundle
                     ?? throw new InvalidOperationException("$everything did not return a Bundle.");

        var page = result;
        var pages = 1;
        while (page.NextLink is not null && pages < maxPages)
        {
            page = await _client.ContinueAsync(page);
            if (page is null) break;
            result.Entry.AddRange(page.Entry);
            pages++;
        }
        result.Link.Clear();
        return result;
    }

    // ------------------------------------------------------------------ encounters

    public async Task<ResourceWriteResult> CreateEncounterAsync(CreateEncounterRequest request, CancellationToken ct)
    {
        var status = EnumUtility.ParseLiteral<Encounter.EncounterStatus>(request.Status.ToLowerInvariant())
                     ?? throw new ArgumentException($"Unknown Encounter.status '{request.Status}'.");

        var classCode = request.ClassCode.ToUpperInvariant();
        var classDisplay = classCode switch
        {
            "AMB" => "ambulatory",
            "IMP" => "inpatient encounter",
            "EMER" => "emergency",
            "VR" => "virtual",
            "HH" => "home health",
            _ => throw new ArgumentException($"Unsupported Encounter.class '{request.ClassCode}' (use AMB, IMP, EMER, VR or HH).")
        };

        var encounter = new Encounter
        {
            Meta = new Meta { Profile = new[] { UsCoreEncounterProfile } },
            Status = status,
            Class = new Coding(ActCodeSystem, classCode, classDisplay),
            Subject = new ResourceReference($"Patient/{request.PatientId}"),
            Period = request.Start is null && request.End is null ? null : new Period
            {
                StartElement = request.Start is null ? null : new FhirDateTime(request.Start.Value),
                EndElement = request.End is null ? null : new FhirDateTime(request.End.Value)
            }
        };
        if (!string.IsNullOrWhiteSpace(request.TypeText))
        {
            encounter.Type.Add(new CodeableConcept { Text = request.TypeText });
        }

        var created = await _client.CreateAsync(encounter) ?? throw new InvalidOperationException("HAPI did not return the created Encounter.");
        return ToWriteResult(created);
    }

    // ------------------------------------------------------------------ transaction / validate

    public async Task<Bundle> TransactionAsync(string bundleJson, FhirSerialization fhir, CancellationToken ct)
    {
        var bundle = fhir.Parse<Bundle>(bundleJson);
        if (bundle.Type is not (Bundle.BundleType.Transaction or Bundle.BundleType.Batch))
            throw new ArgumentException("Bundle.type must be 'transaction' or 'batch'.");

        return await _client.TransactionAsync(bundle) ?? throw new InvalidOperationException("HAPI returned no transaction response.");
    }

    /// <summary>
    /// Invokes HAPI's server-side $validate for the resource. HAPI reports validation failures
    /// either as a 200 OperationOutcome or as a 4xx with an OperationOutcome body; both are
    /// returned to the caller as the OperationOutcome.
    /// </summary>
    public async Task<OperationOutcome> ValidateAsync(string resourceJson, string? profile, FhirSerialization fhir, CancellationToken ct)
    {
        var resource = fhir.Parse(resourceJson);
        var parameters = new Parameters().Add("resource", resource);
        if (!string.IsNullOrWhiteSpace(profile)) parameters.Add("profile", new FhirUri(profile));

        try
        {
            var outcome = await _client.TypeOperationAsync("validate", resource.TypeName, parameters);
            return outcome as OperationOutcome ?? new OperationOutcome
            {
                Issue = { new OperationOutcome.IssueComponent { Severity = OperationOutcome.IssueSeverity.Information, Code = OperationOutcome.IssueType.Informational, Diagnostics = "Validation returned no OperationOutcome." } }
            };
        }
        catch (FhirOperationException ex) when (ex.Outcome is not null)
        {
            return ex.Outcome;
        }
    }

    // ------------------------------------------------------------------ helpers

    private static PatientSummaryDto ToSummary(Patient p)
    {
        var name = p.Name.FirstOrDefault(n => n.Use == HumanName.NameUse.Official) ?? p.Name.FirstOrDefault();
        var mrn = p.Identifier.FirstOrDefault(i => i.Type?.Coding.Any(c => c.Code == "MR") == true) ?? p.Identifier.FirstOrDefault();
        return new PatientSummaryDto(
            p.Id,
            name is null ? null : $"{string.Join(' ', name.Given)} {name.Family}".Trim(),
            p.Gender?.GetLiteral(),
            p.BirthDate,
            mrn?.Value,
            p.Meta?.LastUpdated?.ToString("o"));
    }

    private ResourceWriteResult ToWriteResult(Resource r) =>
        new(r.TypeName, r.Id, r.Meta?.VersionId, $"{_options.BaseUrl}/{r.TypeName}/{r.Id}" + (r.Meta?.VersionId is null ? "" : $"/_history/{r.Meta.VersionId}"));
}
