using System.Diagnostics;
using FhirIntegration.Api.Models;
using FhirIntegration.Api.Services;
using Microsoft.Extensions.Options;

namespace FhirIntegration.Api.Endpoints;

/// <summary>
/// GET /health probes each upstream dependency. HAPI and the validator are required; eCRNow is
/// optional (it may not be running), so it is reported but does not affect the overall status.
/// </summary>
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (IHttpClientFactory factory, IOptions<FhirOptions> options, CancellationToken ct) =>
            {
                var o = options.Value;
                var checks = await Task.WhenAll(
                    Probe(factory, "hapi-fhir", $"{o.BaseUrl.TrimEnd('/')}/metadata?_summary=true", FhirSerialization.FhirJsonMediaType, ct),
                    Probe(factory, "fhir-validator", $"{o.ValidatorUrl.TrimEnd('/')}/", null, ct),
                    Probe(factory, "ecrnow", $"{o.EcrNowUrl.TrimEnd('/')}/api/healthcareSettings/", "application/json", ct));

                var required = checks.Where(c => c.Name != "ecrnow").ToList();
                var status = required.All(c => c.Healthy) ? "healthy" : required.Any(c => c.Healthy) ? "degraded" : "unhealthy";
                var report = new HealthReport(status, DateTimeOffset.UtcNow, checks);
                return Results.Json(report, FhirSerialization.ApiJsonOptions, statusCode: status == "unhealthy" ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status200OK);
            })
            .WithTags("Health")
            .WithName("Health")
            .WithSummary("Reachability of HAPI, the validator service and eCRNow.")
            .Produces<HealthReport>();

        return app;
    }

    private static async Task<DependencyStatus> Probe(IHttpClientFactory factory, string name, string url, string? accept, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var http = factory.CreateClient(HttpClientNames.Self);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (accept is not null) request.Headers.TryAddWithoutValidation("Accept", accept);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            return new DependencyStatus(name, url, response.IsSuccessStatusCode, (int)response.StatusCode, sw.ElapsedMilliseconds, response.IsSuccessStatusCode ? null : response.ReasonPhrase);
        }
        catch (Exception ex)
        {
            return new DependencyStatus(name, url, false, null, sw.ElapsedMilliseconds, ex.GetType().Name + ": " + ex.Message);
        }
    }
}
