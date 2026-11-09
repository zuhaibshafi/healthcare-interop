using System.Text;
using FhirIntegration.Api.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FhirIntegration.Api.Endpoints;

/// <summary>Shared plumbing for endpoints that exchange raw FHIR JSON.</summary>
public static class EndpointHelpers
{
    /// <summary>Reads the request body as a string (FHIR payloads are passed through untyped at the HTTP boundary).</summary>
    public static async Task<string> ReadBodyAsync(HttpRequest request, CancellationToken ct)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync(ct);
    }

    /// <summary>Returns a Firely resource as application/fhir+json.</summary>
    public static IResult FhirJson(FhirSerialization fhir, Base resource, int statusCode = StatusCodes.Status200OK) =>
        Results.Content(fhir.ToJson(resource), FhirSerialization.FhirJsonMediaType, Encoding.UTF8, statusCode);

    /// <summary>Returns an OperationOutcome with a single issue.</summary>
    public static IResult Outcome(FhirSerialization fhir, int statusCode, OperationOutcome.IssueType type, string message)
    {
        var outcome = new OperationOutcome
        {
            Issue = { new OperationOutcome.IssueComponent { Severity = OperationOutcome.IssueSeverity.Error, Code = type, Diagnostics = message } }
        };
        return FhirJson(fhir, outcome, statusCode);
    }

    /// <summary>
    /// Maps exceptions thrown while talking to HAPI onto HTTP responses: HAPI's own OperationOutcome
    /// (with its status code) when available, otherwise a generic outcome. Used by the global handler.
    /// </summary>
    public static IResult ToErrorResult(Exception ex, FhirSerialization fhir) => ex switch
    {
        FhirOperationException foe when foe.Outcome is not null => FhirJson(fhir, foe.Outcome, (int)foe.Status),
        FhirOperationException foe => Outcome(fhir, (int)foe.Status, OperationOutcome.IssueType.Exception, foe.Message),
        FormatException fe => Outcome(fhir, StatusCodes.Status400BadRequest, OperationOutcome.IssueType.Structure, "Invalid FHIR payload: " + fe.Message),
        ArgumentException ae => Outcome(fhir, StatusCodes.Status400BadRequest, OperationOutcome.IssueType.Invalid, ae.Message),
        TimeoutException te => Outcome(fhir, StatusCodes.Status504GatewayTimeout, OperationOutcome.IssueType.Timeout, te.Message),
        HttpRequestException he => Outcome(fhir, StatusCodes.Status502BadGateway, OperationOutcome.IssueType.Transient, "Upstream call failed: " + he.Message),
        _ => Outcome(fhir, StatusCodes.Status500InternalServerError, OperationOutcome.IssueType.Exception, ex.Message)
    };
}
