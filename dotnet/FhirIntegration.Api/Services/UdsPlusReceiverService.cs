using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using FhirIntegration.Api.Models;
using Hl7.Fhir.Model;

namespace FhirIntegration.Api.Services;

/// <summary>
/// Mock of the HRSA "Data Receiver" actor ($import operation).
///
/// It is intentionally small but real: it parses the Import Manifest (Parameters), fetches every
/// udsData.url over HTTP, counts resources per type, persists what it received under
/// OutputDirectory/udsplus-received/{id}/ and exposes the result via $import-status/{id}.
/// Submissions are tracked in memory (restart = empty) plus a status.json on disk.
/// </summary>
public sealed class UdsPlusReceiverService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly OutputPaths _paths;
    private readonly ILogger<UdsPlusReceiverService> _logger;
    private readonly ConcurrentDictionary<string, ImportSubmission> _submissions = new();

    public UdsPlusReceiverService(IHttpClientFactory httpFactory, OutputPaths paths, ILogger<UdsPlusReceiverService> logger)
    {
        _httpFactory = httpFactory;
        _paths = paths;
        _logger = logger;
    }

    public ImportSubmission? Get(string id) => _submissions.TryGetValue(id, out var s) ? s : null;

    public IReadOnlyCollection<ImportSubmission> All() => _submissions.Values.OrderByDescending(s => s.ReceivedAt).ToList();

    /// <summary>Processes a manifest synchronously and returns the submission record.</summary>
    public async Task<ImportSubmission> ProcessAsync(Parameters manifest, string rawManifestJson, CancellationToken ct)
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        var submission = new ImportSubmission { Id = id, SubmissionId = manifest.Parameter.FirstOrDefault(p => p.Name == "submissionId")?.Value?.ToString() };
        _submissions[id] = submission;

        var dir = _paths.ReceivedDirectory(id);
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "manifest.json"), rawManifestJson, new UTF8Encoding(false), ct);

        var inputFormat = manifest.Parameter.FirstOrDefault(p => p.Name == "inputFormat")?.Value?.ToString();
        if (inputFormat is not null && !inputFormat.Contains("ndjson", StringComparison.OrdinalIgnoreCase))
            _logger.LogWarning("Submission {Id}: unexpected inputFormat '{Format}'", id, inputFormat);

        var http = _httpFactory.CreateClient(HttpClientNames.Self);
        foreach (var data in manifest.Parameter.Where(p => p.Name == "udsData"))
        {
            var type = data.Part.FirstOrDefault(x => x.Name == "type")?.Value?.ToString() ?? "Unknown";
            var url = data.Part.FirstOrDefault(x => x.Name == "url")?.Value?.ToString();
            if (url is null)
            {
                submission.Outcomes.Add(new ImportFileOutcome(type, "", 0, 0, "udsData without url"));
                continue;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("Accept", FhirSerialization.FhirNdjsonMediaType);
                var bearer = data.Part.FirstOrDefault(x => x.Name == "bearer")?.Value?.ToString();
                if (!string.IsNullOrEmpty(bearer)) request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {bearer}");

                using var response = await http.SendAsync(request, ct);
                var text = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                {
                    submission.Outcomes.Add(new ImportFileOutcome(type, url, 0, (int)response.StatusCode, "download failed"));
                    continue;
                }

                var resources = BulkDataExportClient.ParseNdjson(text);
                var mismatched = resources.Count(r => r["resourceType"]?.GetValue<string>() != type);
                await File.WriteAllTextAsync(Path.Combine(dir, $"{OutputPaths.SafeSegment(type)}.ndjson"), text, new UTF8Encoding(false), ct);

                submission.Counts[type] = submission.Counts.GetValueOrDefault(type) + resources.Count;
                submission.Outcomes.Add(new ImportFileOutcome(type, url, resources.Count, (int)response.StatusCode,
                    mismatched > 0 ? $"{mismatched} line(s) had a different resourceType than declared" : null));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Submission {Id}: failed to fetch {Url}", id, url);
                submission.Outcomes.Add(new ImportFileOutcome(type, url, 0, 0, ex.Message));
            }
        }

        submission.Status = submission.Outcomes.Any(o => o.Error is not null && o.ResourceCount == 0) ? "complete-with-errors" : "complete";
        submission.CompletedAt = DateTimeOffset.UtcNow;
        await File.WriteAllTextAsync(Path.Combine(dir, "status.json"), JsonSerializer.Serialize(submission, FhirSerialization.ApiJsonOptions), new UTF8Encoding(false), ct);

        _logger.LogInformation("Submission {Id} ({SubmissionId}): {Status}, counts {Counts}", id, submission.SubmissionId, submission.Status,
            string.Join(", ", submission.Counts.Select(kv => $"{kv.Key}={kv.Value}")));
        return submission;
    }
}
