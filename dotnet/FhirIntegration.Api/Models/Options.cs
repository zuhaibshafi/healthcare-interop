namespace FhirIntegration.Api.Models;

/// <summary>
/// Connection settings for the external systems this layer integrates with.
/// Bound from the "Fhir" section of appsettings.json.
/// </summary>
public sealed class FhirOptions
{
    public const string SectionName = "Fhir";

    /// <summary>HAPI FHIR R4 base URL (the "Data Source" in UDS+ terms).</summary>
    public string BaseUrl { get; set; } = "http://localhost:8080/fhir";

    /// <summary>HL7 FHIR validator wrapper service.</summary>
    public string ValidatorUrl { get; set; } = "http://localhost:8082";

    /// <summary>eCRNow (electronic case reporting) application.</summary>
    public string EcrNowUrl { get; set; } = "http://localhost:8081";

    /// <summary>Root folder for everything this service persists (eICRs, UDS+ runs, received submissions).</summary>
    public string OutputDirectory { get; set; } = "C:/git/interop-assessment/results/dotnet-output";

    /// <summary>
    /// The URL under which *this* service is reachable. Used to build absolute URLs
    /// (UDS+ NDJSON file links in the import manifest, Content-Location headers).
    /// </summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5080";
}

/// <summary>Settings for the OAuth2 client-credentials token facade (section "Auth").</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "http://localhost:5080";

    /// <summary>HMAC-SHA256 key used to sign issued JWTs. Dev-only default; override via environment in real deployments.</summary>
    public string SigningKey { get; set; } = "dev-only-hs256-signing-key-please-change-0123456789";

    public int TokenLifetimeSeconds { get; set; } = 3600;

    public List<OAuthClient> Clients { get; set; } = new();
}

public sealed class OAuthClient
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";

    /// <summary>Space-separated SMART "system/" scopes granted to this client.</summary>
    public string Scopes { get; set; } = "system/*.read";
}

/// <summary>Settings for the UDS+ Data Submitter pipeline (section "UdsPlus").</summary>
public sealed class UdsPlusOptions
{
    public const string SectionName = "UdsPlus";

    // Note: the configuration binder *appends* to pre-populated lists, so defaults live in the
    // Effective* properties instead of in initialisers (otherwise appsettings values would be duplicated).
    public List<string> ExportResourceTypes { get; set; } = new();

    public List<string> ValidatorIgs { get; set; } = new();

    public IReadOnlyList<string> EffectiveExportResourceTypes => ExportResourceTypes.Count > 0
        ? ExportResourceTypes.Distinct().ToList()
        : new[] { "Patient", "Encounter", "Condition", "Procedure", "Observation", "Immunization", "MedicationRequest", "AllergyIntolerance" };

    public IReadOnlyList<string> EffectiveValidatorIgs => ValidatorIgs.Count > 0
        ? ValidatorIgs.Distinct().ToList()
        : new[] { "fhir.hrsa.uds-plus#2.0.0" };

    public int ExportPollIntervalSeconds { get; set; } = 2;

    public int ExportTimeoutSeconds { get; set; } = 180;

    /// <summary>Upper bound on resources per type sent to the validator (keeps the validator call bounded).</summary>
    public int MaxResourcesToValidatePerType { get; set; } = 50;

    public HealthCenterInfo HealthCenter { get; set; } = new();
}

/// <summary>
/// Health-center identity carried in the manifest's "hcInformation" parameter
/// (UEI, HRSA grant number, contact). All values are demo placeholders.
/// </summary>
public sealed class HealthCenterInfo
{
    public string Uei { get; set; } = "DEMO12345678";
    public string GrantNumber { get; set; } = "H80CS00000";
    public string Name { get; set; } = "Demo Community Health Center";
    public string AddressLine { get; set; } = "100 Interop Way";
    public string City { get; set; } = "Atlanta";
    public string State { get; set; } = "GA";
    public string PostalCode { get; set; } = "30301";
    public string ContactFamily { get; set; } = "Reporter";
    public string ContactGiven { get; set; } = "Uds";
    public string ContactPhone { get; set; } = "555-010-0000";
    public bool PrenatalCareByReferralOnly { get; set; }
}

/// <summary>Named HttpClient registrations (see Program.cs).</summary>
public static class HttpClientNames
{
    public const string Hapi = "hapi";
    public const string Validator = "validator";
    public const string EcrNow = "ecrnow";
    public const string Self = "self";
}
