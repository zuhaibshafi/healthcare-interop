using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FhirIntegration.Api.Models;
using Microsoft.Extensions.Options;

namespace FhirIntegration.Api.Services;

/// <summary>
/// Client for the HL7 FHIR validator wrapper service (POST /validate).
///
/// The wrapper downloads IG packages on first use for a given IG set, which can take minutes,
/// hence the long timeout on the named HttpClient. A sessionId is returned and re-used so later
/// calls hit the warm validation engine.
/// </summary>
public sealed class ValidatorClient
{
    private readonly HttpClient _http;
    private readonly FhirOptions _options;
    private readonly ILogger<ValidatorClient> _logger;

    // The wrapper session is process-wide state: one warm engine per IG set.
    private readonly Dictionary<string, string> _sessionsByIgSet = new();
    private readonly object _lock = new();

    public ValidatorClient(IHttpClientFactory factory, IOptions<FhirOptions> options, ILogger<ValidatorClient> logger)
    {
        _http = factory.CreateClient(HttpClientNames.Validator);
        _options = options.Value;
        _logger = logger;
    }

    public sealed record FileToValidate(string FileName, string FileContent, string FileType = "json");

    public sealed record ValidationIssue(string Level, string? Message, string? Location, string? MessageId);

    public sealed record FileOutcome(string FileName, IReadOnlyList<ValidationIssue> Issues)
    {
        public int Count(string level) => Issues.Count(i => string.Equals(i.Level, level, StringComparison.OrdinalIgnoreCase));
        public int Errors => Issues.Count(i => i.Level.Equals("ERROR", StringComparison.OrdinalIgnoreCase) || i.Level.Equals("FATAL", StringComparison.OrdinalIgnoreCase));
        public int Warnings => Count("WARNING");
        public int Information => Count("INFORMATION");
    }

    public sealed record ValidationResult(string? SessionId, IReadOnlyList<FileOutcome> Outcomes, JsonNode RawResponse);

    /// <summary>Validates a batch of files against FHIR 4.0.1 plus the given IG packages.</summary>
    public async Task<ValidationResult> ValidateAsync(IReadOnlyList<FileToValidate> files, IEnumerable<string> igs, CancellationToken ct)
    {
        var igList = igs.ToList();
        var igKey = string.Join("|", igList);
        string? sessionId;
        lock (_lock) _sessionsByIgSet.TryGetValue(igKey, out sessionId);

        var request = new
        {
            cliContext = new { sv = "4.0.1", igs = igList, locale = "en" },
            filesToValidate = files.Select(f => new { fileName = f.FileName, fileContent = f.FileContent, fileType = f.FileType }).ToList(),
            sessionId
        };

        _logger.LogInformation("Validating {Count} file(s) against {Igs} (session {Session})", files.Count, igKey, sessionId ?? "new");
        var started = DateTime.UtcNow;

        using var response = await _http.PostAsJsonAsync("/validate", request, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        }, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Validator returned {(int)response.StatusCode}: {(body.Length > 500 ? body[..500] : body)}");

        var root = JsonNode.Parse(body) ?? throw new InvalidOperationException("Validator returned an empty body.");
        var newSession = root["sessionId"]?.GetValue<string>();
        if (newSession is not null) lock (_lock) _sessionsByIgSet[igKey] = newSession;

        var outcomes = new List<FileOutcome>();
        foreach (var outcome in root["outcomes"]?.AsArray().OfType<JsonObject>() ?? Enumerable.Empty<JsonObject>())
        {
            var fileName = outcome["fileInfo"]?["fileName"]?.GetValue<string>() ?? "(unknown)";
            var issues = outcome["issues"]?.AsArray().OfType<JsonObject>().Select(i => new ValidationIssue(
                i["level"]?.GetValue<string>() ?? "UNKNOWN",
                i["message"]?.GetValue<string>(),
                i["location"]?.GetValue<string>(),
                i["messageId"]?.GetValue<string>())).ToList() ?? new List<ValidationIssue>();
            outcomes.Add(new FileOutcome(fileName, issues));
        }

        _logger.LogInformation("Validation finished in {Seconds:F1}s: {Errors} errors, {Warnings} warnings",
            (DateTime.UtcNow - started).TotalSeconds, outcomes.Sum(o => o.Errors), outcomes.Sum(o => o.Warnings));

        return new ValidationResult(newSession ?? sessionId, outcomes, root);
    }
}
