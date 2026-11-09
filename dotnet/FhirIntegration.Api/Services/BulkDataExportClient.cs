using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using FhirIntegration.Api.Models;
using Microsoft.Extensions.Options;

namespace FhirIntegration.Api.Services;

/// <summary>
/// Client for the FHIR Bulk Data Access ($export) asynchronous request pattern against HAPI.
///
/// The Firely FhirClient does not model the async kick-off / poll / download protocol, so this
/// uses a raw HttpClient:
///   1. GET Group/{id}/$export?_type=...   with Prefer: respond-async     -> 202 + Content-Location
///   2. GET {Content-Location} every N seconds                           -> 202 (in progress) ... 200 (complete)
///   3. GET each output[].url with Accept: application/fhir+ndjson        -> NDJSON lines
/// </summary>
public sealed class BulkDataExportClient
{
    private readonly HttpClient _http;
    private readonly UdsPlusOptions _options;
    private readonly ILogger<BulkDataExportClient> _logger;

    public BulkDataExportClient(IHttpClientFactory factory, IOptions<UdsPlusOptions> options, ILogger<BulkDataExportClient> logger)
    {
        _http = factory.CreateClient(HttpClientNames.Hapi);
        _options = options.Value;
        _logger = logger;
    }

    public sealed record ExportFile(string Type, string Url, IReadOnlyList<JsonObject> Resources);

    public sealed record ExportResult(string StatusUrl, string? TransactionTime, IReadOnlyList<ExportFile> Files);

    /// <summary>Runs a complete Group-level export and downloads all NDJSON outputs into memory.</summary>
    public async Task<ExportResult> ExportGroupAsync(string groupId, IEnumerable<string> resourceTypes, CancellationToken ct)
    {
        var kickoffUrl = $"Group/{Uri.EscapeDataString(groupId)}/$export?_type={string.Join(',', resourceTypes)}";
        _logger.LogInformation("Bulk export kick-off: {Url}", kickoffUrl);

        using var kickoff = new HttpRequestMessage(HttpMethod.Get, kickoffUrl);
        kickoff.Headers.TryAddWithoutValidation("Accept", FhirSerialization.FhirJsonMediaType);
        kickoff.Headers.TryAddWithoutValidation("Prefer", "respond-async");

        using var kickoffResponse = await _http.SendAsync(kickoff, ct);
        if (kickoffResponse.StatusCode != HttpStatusCode.Accepted)
        {
            var body = await kickoffResponse.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"$export kick-off returned {(int)kickoffResponse.StatusCode}: {Truncate(body)}");
        }

        var statusUrl = ContentLocation(kickoffResponse);
        if (string.IsNullOrEmpty(statusUrl)) throw new InvalidOperationException("$export kick-off did not return a Content-Location header.");

        var manifest = await PollUntilCompleteAsync(statusUrl, ct);

        var files = new List<ExportFile>();
        foreach (var output in manifest["output"]?.AsArray().OfType<JsonObject>() ?? Enumerable.Empty<JsonObject>())
        {
            var type = output["type"]?.GetValue<string>() ?? "Unknown";
            var url = output["url"]?.GetValue<string>() ?? throw new InvalidOperationException("Export output entry has no url.");
            var resources = await DownloadNdjsonAsync(url, ct);
            _logger.LogInformation("Downloaded {Count} {Type} resources from {Url}", resources.Count, type, url);
            files.Add(new ExportFile(type, url, resources));
        }

        return new ExportResult(statusUrl, manifest["transactionTime"]?.GetValue<string>(), files);
    }

    private async Task<JsonObject> PollUntilCompleteAsync(string statusUrl, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(_options.ExportTimeoutSeconds);
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.ExportPollIntervalSeconds));

        while (true)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, statusUrl);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            using var response = await _http.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                return JsonNode.Parse(json)?.AsObject() ?? throw new InvalidOperationException("Export status returned empty body.");
            }

            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"$export status poll returned {(int)response.StatusCode}: {Truncate(body)}");
            }

            var progress = response.Headers.TryGetValues("X-Progress", out var p) ? p.FirstOrDefault() : null;
            _logger.LogDebug("Export in progress ({Progress}); polling again in {Interval}s", progress ?? "no progress header", interval.TotalSeconds);

            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"$export did not complete within {_options.ExportTimeoutSeconds}s (status URL {statusUrl}).");

            await Task.Delay(interval, ct);
        }
    }

    private async Task<List<JsonObject>> DownloadNdjsonAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept", FhirSerialization.FhirNdjsonMediaType);
        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var text = await response.Content.ReadAsStringAsync(ct);
        return ParseNdjson(text);
    }

    public static List<JsonObject> ParseNdjson(string text)
    {
        var list = new List<JsonObject>();
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;
            try
            {
                if (JsonNode.Parse(trimmed) is JsonObject obj) list.Add(obj);
            }
            catch (JsonException)
            {
                // skip malformed lines rather than failing the whole export
            }
        }
        return list;
    }

    /// <summary>
    /// Content-Location is classified as a *content* header by System.Net.Http, so it lives on
    /// response.Content.Headers; fall back to the general header collection for non-compliant servers.
    /// </summary>
    public static string? ContentLocation(HttpResponseMessage response) =>
        response.Content.Headers.ContentLocation?.ToString()
        ?? (response.Headers.TryGetValues("Content-Location", out var values) ? values.FirstOrDefault() : null);

    private static string Truncate(string s) => s.Length <= 500 ? s : s[..500] + "...";
}
