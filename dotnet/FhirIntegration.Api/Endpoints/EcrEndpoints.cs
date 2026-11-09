using FhirIntegration.Api.Models;
using FhirIntegration.Api.Services;

namespace FhirIntegration.Api.Endpoints;

/// <summary>Thin proxy endpoints in front of the eCRNow application.</summary>
public static class EcrEndpoints
{
    public static IEndpointRouteBuilder MapEcrEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ecr").WithTags("eCRNow client");

        group.MapPost("/launch", async (EcrLaunchRequest request, EcrNowClient ecr, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(request.PatientId) || string.IsNullOrWhiteSpace(request.EncounterId))
                    return Results.BadRequest(new { error = "patientId and encounterId are required" });
                try
                {
                    var response = await ecr.LaunchPatientAsync(request, ct);
                    return Results.Json(response, FhirSerialization.ApiJsonOptions, statusCode: response.StatusCode >= 500 ? StatusCodes.Status502BadGateway : StatusCodes.Status200OK);
                }
                catch (HttpRequestException ex)
                {
                    return Results.Json(new { error = "eCRNow unreachable", detail = ex.Message }, statusCode: StatusCodes.Status502BadGateway);
                }
            })
            .WithName("EcrLaunch")
            .WithSummary("POSTs {fhirServerURL, patientId, encounterId} to eCRNow /api/launchPatient and relays the response.")
            .Produces<UpstreamResponse>();

        group.MapGet("/healthcare-settings", async (EcrNowClient ecr, CancellationToken ct) =>
            {
                try
                {
                    var response = await ecr.GetHealthcareSettingsAsync(ct);
                    return Results.Json(response, FhirSerialization.ApiJsonOptions);
                }
                catch (HttpRequestException ex)
                {
                    return Results.Json(new { error = "eCRNow unreachable", detail = ex.Message }, statusCode: StatusCodes.Status502BadGateway);
                }
            })
            .WithName("EcrHealthcareSettings")
            .WithSummary("Proxies GET {EcrNowUrl}/api/healthcareSettings/.")
            .Produces<UpstreamResponse>();

        return app;
    }
}
