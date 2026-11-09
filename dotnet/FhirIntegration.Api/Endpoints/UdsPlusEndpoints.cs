using FhirIntegration.Api.Models;
using FhirIntegration.Api.Services;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task; // Hl7.Fhir.Model also defines a Task resource

namespace FhirIntegration.Api.Endpoints;

/// <summary>
/// UDS+ endpoints. Two actors live here:
///   * Data Submitter  — POST /api/udsplus/submit runs export -> de-identify -> manifest -> validate -> $import.
///   * Data Receiver   — POST /udsplus/$import (+ $import-status) is the mock HRSA receiver.
/// The de-identified NDJSON files are served read-only from /udsplus/files/{runId}/{fileName}.
/// </summary>
public static class UdsPlusEndpoints
{
    public static IEndpointRouteBuilder MapUdsPlusEndpoints(this IEndpointRouteBuilder app)
    {
        // ---------------------------------------------------------------- Data Submitter
        app.MapPost("/api/udsplus/submit", async (string? groupId, bool? validate, bool? submit, UdsPlusSubmitterService submitter, CancellationToken ct) =>
            {
                var result = await submitter.SubmitAsync(groupId ?? "uds-plus-reporting-group", validate ?? true, submit ?? true, ct);
                return Results.Json(result, FhirSerialization.ApiJsonOptions);
            })
            .WithTags("UDS+ Data Submitter")
            .WithName("UdsPlusSubmit")
            .WithSummary("Runs the UDS+ pipeline: Group $export -> de-identify -> NDJSON + manifest -> validate (optional) -> $import to the mock receiver.")
            .Produces<UdsPlusSubmitResult>();

        app.MapGet("/api/udsplus/runs", (OutputPaths paths, IOptions<FhirOptions> options) =>
            {
                var runs = new DirectoryInfo(paths.UdsPlusRuns).EnumerateDirectories()
                    .OrderByDescending(d => d.Name)
                    .Select(d => new
                    {
                        runId = d.Name,
                        createdAt = d.CreationTimeUtc,
                        files = d.EnumerateFiles().Select(f => $"{options.Value.PublicBaseUrl.TrimEnd('/')}/udsplus/files/{d.Name}/{f.Name}").ToList()
                    });
                return Results.Json(runs, FhirSerialization.ApiJsonOptions);
            })
            .WithTags("UDS+ Data Submitter")
            .WithName("UdsPlusRuns")
            .WithSummary("Lists previous submitter runs and their artefacts.");

        app.MapGet("/udsplus/files/{runId}/{fileName}", (string runId, string fileName, OutputPaths paths) =>
            {
                string path;
                try { path = Path.Combine(paths.RunDirectory(runId), OutputPaths.SafeSegment(fileName)); }
                catch (ArgumentException) { return Results.BadRequest(); }
                if (!File.Exists(path)) return Results.NotFound();

                var contentType = fileName.EndsWith(".ndjson", StringComparison.OrdinalIgnoreCase)
                    ? FhirSerialization.FhirNdjsonMediaType
                    : fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? FhirSerialization.FhirJsonMediaType : "application/octet-stream";
                return Results.File(path, contentType);
            })
            .WithTags("UDS+ Data Submitter")
            .WithName("UdsPlusFile")
            .WithSummary("Read-only download of a run artefact (NDJSON as application/fhir+ndjson).");

        // ---------------------------------------------------------------- Data Receiver (mock)
        app.MapPost("/udsplus/$import", async (HttpRequest request, UdsPlusReceiverService receiver, FhirSerialization fhir, IOptions<FhirOptions> options, CancellationToken ct) =>
            {
                var body = await EndpointHelpers.ReadBodyAsync(request, ct);
                Parameters manifest;
                try { manifest = fhir.Parse<Parameters>(body); }
                catch (Exception ex)
                {
                    return EndpointHelpers.Outcome(fhir, StatusCodes.Status400BadRequest, OperationOutcome.IssueType.Structure, "Body must be a FHIR Parameters import manifest: " + ex.Message);
                }

                var submission = await receiver.ProcessAsync(manifest, body, ct);
                var statusUrl = $"{options.Value.PublicBaseUrl.TrimEnd('/')}/udsplus/$import-status/{submission.Id}";

                var outcome = new OperationOutcome
                {
                    Issue =
                    {
                        new OperationOutcome.IssueComponent
                        {
                            Severity = OperationOutcome.IssueSeverity.Information,
                            Code = OperationOutcome.IssueType.Informational,
                            Diagnostics = $"Submission {submission.Id} accepted; poll {statusUrl}"
                        }
                    }
                };
                var result = EndpointHelpers.FhirJson(fhir, outcome, StatusCodes.Status202Accepted);
                return new WithHeaders(result, ("Content-Location", statusUrl));
            })
            .WithTags("UDS+ Data Receiver (mock)")
            .WithName("UdsPlusImport")
            .WithSummary("Mock HRSA $import: fetches every udsData.url in the manifest, counts resources, returns 202 + Content-Location.")
            .Accepts<string>(FhirSerialization.FhirJsonMediaType, "application/json");

        app.MapGet("/udsplus/$import-status/{id}", (string id, UdsPlusReceiverService receiver) =>
            {
                var submission = receiver.Get(id);
                return submission is null
                    ? Results.NotFound(new { error = $"submission {id} not found" })
                    : Results.Json(new { id = submission.Id, status = submission.Status, submissionId = submission.SubmissionId, receivedAt = submission.ReceivedAt, completedAt = submission.CompletedAt, counts = submission.Counts, outcomes = submission.Outcomes }, FhirSerialization.ApiJsonOptions);
            })
            .WithTags("UDS+ Data Receiver (mock)")
            .WithName("UdsPlusImportStatus")
            .WithSummary("Status of a received submission: {status, counts, outcomes}.");

        app.MapGet("/udsplus/received", (UdsPlusReceiverService receiver) => Results.Json(receiver.All(), FhirSerialization.ApiJsonOptions))
            .WithTags("UDS+ Data Receiver (mock)")
            .WithName("UdsPlusReceivedList")
            .WithSummary("All submissions received by the mock receiver since start-up.");

        return app;
    }

    /// <summary>Wraps an IResult and adds response headers (minimal API has no built-in way to do this on Results.Content).</summary>
    private sealed class WithHeaders : IResult
    {
        private readonly IResult _inner;
        private readonly (string Name, string Value)[] _headers;

        public WithHeaders(IResult inner, params (string, string)[] headers)
        {
            _inner = inner;
            _headers = headers;
        }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            foreach (var (name, value) in _headers) httpContext.Response.Headers[name] = value;
            return _inner.ExecuteAsync(httpContext);
        }
    }
}
