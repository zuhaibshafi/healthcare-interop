using FhirIntegration.Api.Models;
using FhirIntegration.Api.Services;

namespace FhirIntegration.Api.Endpoints;

/// <summary>
/// Mock Public Health Agency receiver. eCRNow is configured to post eICR message bundles to
/// {this}/pha/r4/fhir/$process-message (the standard FHIR messaging operation); /Bundle is offered as
/// an alias because some eCRNow configurations post to the FHIR base + resource type.
/// </summary>
public static class PhaEndpoints
{
    public static IEndpointRouteBuilder MapPhaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/pha").WithTags("PHA receiver mock");

        async Task<IResult> Receive(HttpRequest request, PhaReceiverService pha, FhirSerialization fhir, CancellationToken ct)
        {
            var body = await EndpointHelpers.ReadBodyAsync(request, ct);
            if (string.IsNullOrWhiteSpace(body))
                return EndpointHelpers.Outcome(fhir, StatusCodes.Status400BadRequest, Hl7.Fhir.Model.OperationOutcome.IssueType.Required, "Request body is empty; expected a FHIR message Bundle.");

            var result = await pha.ReceiveAsync(body, ct);
            return EndpointHelpers.FhirJson(fhir, result.Acknowledgement);
        }

        // eCRNow instantiates a HAPI FHIR client with base URL {this}/pha/r4/fhir and calls $process-message on it;
        // standard FHIR clients probe /metadata first, so a minimal CapabilityStatement is published here.
        group.MapGet("/r4/fhir/metadata", (PhaReceiverService pha, FhirSerialization fhir) =>
                EndpointHelpers.FhirJson(fhir, pha.BuildCapabilityStatement()))
            .WithName("PhaMetadata")
            .WithSummary("Minimal R4 CapabilityStatement for the PHA receiver (advertises the $process-message operation).");

        group.MapPost("/r4/fhir/$process-message", Receive)
            .WithName("ProcessMessage")
            .WithSummary("Receives an eICR message Bundle; persists it and returns a Reportability-Response-style acknowledgement Bundle.")
            .Accepts<string>(FhirSerialization.FhirJsonMediaType, "application/json");

        group.MapPost("/r4/fhir/Bundle", Receive)
            .WithName("ReceiveBundle")
            .WithSummary("Alias of $process-message for clients that POST to the Bundle endpoint.")
            .Accepts<string>(FhirSerialization.FhirJsonMediaType, "application/json");

        group.MapGet("/received", (PhaReceiverService pha) => Results.Ok(pha.ListReceived()))
            .WithName("ListReceivedEicrs")
            .WithSummary("Lists eICR bundles received so far (file name, size, timestamp).")
            .Produces<List<ReceivedEicr>>();

        group.MapGet("/received/{fileName}", (string fileName, PhaReceiverService pha) =>
            {
                var content = pha.ReadReceived(fileName);
                return content is null ? Results.NotFound() : Results.Content(content, FhirSerialization.FhirJsonMediaType);
            })
            .WithName("GetReceivedEicr")
            .WithSummary("Returns a previously received eICR bundle.");

        return app;
    }
}
