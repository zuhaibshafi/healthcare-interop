using FhirIntegration.Api.Models;
using Microsoft.Extensions.Options;

namespace FhirIntegration.Api.Services;

/// <summary>
/// Central place for the on-disk layout under Fhir:OutputDirectory:
///   eicr/                 eICR message bundles received by the PHA mock
///   udsplus/{runId}/      de-identified NDJSON, manifest.json, validation-report.json, run-summary.json
///   udsplus-received/{id} submissions received by the mock HRSA Data Receiver
/// </summary>
public sealed class OutputPaths
{
    public OutputPaths(IOptions<FhirOptions> options)
    {
        Root = Path.GetFullPath(options.Value.OutputDirectory);
        Eicr = Path.Combine(Root, "eicr");
        UdsPlusRuns = Path.Combine(Root, "udsplus");
        UdsPlusReceived = Path.Combine(Root, "udsplus-received");
        foreach (var dir in new[] { Root, Eicr, UdsPlusRuns, UdsPlusReceived }) Directory.CreateDirectory(dir);
    }

    public string Root { get; }
    public string Eicr { get; }
    public string UdsPlusRuns { get; }
    public string UdsPlusReceived { get; }

    public string RunDirectory(string runId) => Path.Combine(UdsPlusRuns, SafeSegment(runId));

    public string ReceivedDirectory(string id) => Path.Combine(UdsPlusReceived, SafeSegment(id));

    /// <summary>Rejects path traversal: only [A-Za-z0-9._-] survive, and ".." is not allowed.</summary>
    public static string SafeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains("..") || value.Any(c => !(char.IsLetterOrDigit(c) || c is '.' or '_' or '-')))
            throw new ArgumentException($"Invalid path segment '{value}'.");
        return value;
    }
}
