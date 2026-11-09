using System.Text.Json.Nodes;

namespace FhirIntegration.Api.Models;

// ---------------------------------------------------------------------------
// Simplified request/response contracts exposed by the gateway. These hide the
// verbosity of raw FHIR from API consumers that only need a few fields, while
// the "raw FHIR" endpoints still pass full resources through untouched.
// ---------------------------------------------------------------------------

/// <summary>Flattened view of a FHIR Patient for list/search responses.</summary>
public sealed record PatientSummaryDto(
    string Id,
    string? Name,
    string? Gender,
    string? BirthDate,
    string? Mrn,
    string? LastUpdated);

/// <summary>Minimal input for creating a US Core Patient.</summary>
public sealed record CreatePatientRequest(
    string Family,
    string Given,
    string? Gender,
    string? BirthDate,
    string? Mrn);

/// <summary>Minimal input for creating a US Core Encounter.</summary>
public sealed record CreateEncounterRequest(
    string PatientId,
    DateTimeOffset? Start,
    DateTimeOffset? End,
    string ClassCode = "AMB",
    string? TypeText = "Office Visit",
    string Status = "finished");

/// <summary>Result of a create/update against HAPI.</summary>
public sealed record ResourceWriteResult(string ResourceType, string Id, string? VersionId, string Location);

/// <summary>Summary of HAPI's CapabilityStatement.</summary>
public sealed record CapabilitySummary(
    string? FhirVersion,
    string? SoftwareName,
    string? SoftwareVersion,
    int ResourceTypeCount,
    IReadOnlyList<string> ResourceTypes,
    string Url);

/// <summary>Request body for POST /api/ecr/launch.</summary>
public sealed record EcrLaunchRequest(string PatientId, string EncounterId, bool ValidationMode = false);

/// <summary>Result of proxying a call to an upstream HTTP system (eCRNow, mock receiver, etc.).</summary>
public sealed record UpstreamResponse(int StatusCode, string? ContentType, JsonNode? Body, string? RawBody, string Url);

/// <summary>Per-dependency health check result.</summary>
public sealed record DependencyStatus(string Name, string Url, bool Healthy, int? StatusCode, long ElapsedMs, string? Detail);

public sealed record HealthReport(string Status, DateTimeOffset CheckedAt, IReadOnlyList<DependencyStatus> Dependencies);

/// <summary>Metadata for an eICR bundle received by the PHA mock.</summary>
public sealed record ReceivedEicr(string FileName, long SizeBytes, DateTimeOffset ReceivedAt, string Url);

// ---------------------------------------------------------------------------
// UDS+ pipeline contracts
// ---------------------------------------------------------------------------

public sealed record ValidationFileSummary(string FileName, int Errors, int Warnings, int Information);

public sealed record ValidationSummary(
    string? SessionId,
    int Errors,
    int Warnings,
    int Information,
    IReadOnlyList<ValidationFileSummary> Files,
    string ReportUrl,
    string? Error);

public sealed record UdsPlusSubmitResult(
    string RunId,
    string GroupId,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    IReadOnlyDictionary<string, int> ExportedCounts,
    IReadOnlyDictionary<string, int> DeidentifiedCounts,
    IReadOnlyList<string> Files,
    string ManifestUrl,
    ValidationSummary? Validation,
    UpstreamResponse? ImportResponse,
    JsonNode? ImportStatus,
    IReadOnlyList<string> Notes);

/// <summary>State of a submission received by the mock HRSA Data Receiver ($import).</summary>
public sealed class ImportSubmission
{
    public string Id { get; init; } = "";
    public string Status { get; set; } = "in-progress";
    public string? SubmissionId { get; set; }
    public DateTimeOffset ReceivedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public Dictionary<string, int> Counts { get; } = new();
    public List<ImportFileOutcome> Outcomes { get; } = new();
}

public sealed record ImportFileOutcome(string Type, string Url, int ResourceCount, int StatusCode, string? Error);
