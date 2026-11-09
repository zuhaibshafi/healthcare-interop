using FhirIntegration.Api.Models;
using FhirIntegration.Api.Services;
using Hl7.Fhir.Model;

namespace FhirIntegration.Api.Endpoints;

/// <summary>
/// FHIR gateway endpoints: a simplified, opinionated REST surface in front of HAPI.
///
/// Two styles coexist on purpose:
///   * DTO endpoints (/api/patients list + create, /api/encounters) for consumers that want a
///     narrow contract and do not care about FHIR's shape.
///   * Raw FHIR pass-through endpoints (GET patient, PUT patient, transaction, $validate,
///     patient-scoped searches) for FHIR-aware consumers; these return application/fhir+json.
/// </summary>
public static class FhirGatewayEndpoints
{
    public static IEndpointRouteBuilder MapFhirGatewayEndpoints(this IEndpointRouteBuilder app)
    {
        var fhirGroup = app.MapGroup("/api/fhir").WithTags("FHIR gateway");

        fhirGroup.MapGet("/metadata", async (FhirGatewayService gateway, CancellationToken ct) =>
                Results.Ok(await gateway.GetCapabilitySummaryAsync(ct)))
            .WithName("GetMetadataSummary")
            .WithSummary("Summary of HAPI's CapabilityStatement (FHIR version, software, supported resource types).")
            .Produces<CapabilitySummary>();

        fhirGroup.MapPost("/transaction", async (HttpRequest request, FhirGatewayService gateway, FhirSerialization fhir, CancellationToken ct) =>
            {
                var body = await EndpointHelpers.ReadBodyAsync(request, ct);
                var response = await gateway.TransactionAsync(body, fhir, ct);
                var statuses = response.Entry.Select((e, i) => new
                {
                    index = i,
                    status = e.Response?.Status,
                    location = e.Response?.Location,
                    etag = e.Response?.Etag
                }).ToList();
                return Results.Json(new { bundleType = response.Type?.ToString(), entryCount = response.Entry.Count, entries = statuses }, FhirSerialization.ApiJsonOptions);
            })
            .WithName("PostTransaction")
            .WithSummary("Forwards a raw transaction/batch Bundle to HAPI and returns the per-entry response statuses.")
            .Accepts<string>(FhirSerialization.FhirJsonMediaType, "application/json");

        fhirGroup.MapPost("/validate", async (HttpRequest request, string? profile, FhirGatewayService gateway, FhirSerialization fhir, CancellationToken ct) =>
            {
                var body = await EndpointHelpers.ReadBodyAsync(request, ct);
                var outcome = await gateway.ValidateAsync(body, profile, fhir, ct);
                return EndpointHelpers.FhirJson(fhir, outcome);
            })
            .WithName("ValidateResource")
            .WithSummary("Sends a resource to HAPI's $validate (optionally against a profile) and returns the OperationOutcome.")
            .Accepts<string>(FhirSerialization.FhirJsonMediaType, "application/json");

        var patients = app.MapGroup("/api/patients").WithTags("Patients");

        patients.MapGet("/", async (string? family, string? given, string? identifier, int? count, FhirGatewayService gateway, CancellationToken ct) =>
                Results.Ok(await gateway.SearchPatientsAsync(family, given, identifier, Math.Clamp(count ?? 20, 1, 200), ct)))
            .WithName("SearchPatients")
            .WithSummary("Search Patients by family/given/identifier; returns a simplified DTO list.")
            .Produces<List<PatientSummaryDto>>();

        patients.MapGet("/{id}", async (string id, FhirGatewayService gateway, FhirSerialization fhir, CancellationToken ct) =>
            {
                var patient = await gateway.GetPatientAsync(id, ct);
                return patient is null
                    ? EndpointHelpers.Outcome(fhir, StatusCodes.Status404NotFound, OperationOutcome.IssueType.NotFound, $"Patient/{id} not found")
                    : EndpointHelpers.FhirJson(fhir, patient);
            })
            .WithName("GetPatient")
            .WithSummary("Raw FHIR Patient resource.");

        patients.MapPost("/", async (CreatePatientRequest request, FhirGatewayService gateway, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(request.Family) || string.IsNullOrWhiteSpace(request.Given))
                    return Results.BadRequest(new { error = "family and given are required" });
                var result = await gateway.CreatePatientAsync(request, ct);
                return Results.Created(result.Location, result);
            })
            .WithName("CreatePatient")
            .WithSummary("Creates a US Core-profiled Patient from a simplified DTO {family, given, gender, birthDate, mrn}.")
            .Produces<ResourceWriteResult>(StatusCodes.Status201Created);

        patients.MapPut("/{id}", async (string id, HttpRequest request, FhirGatewayService gateway, FhirSerialization fhir, CancellationToken ct) =>
            {
                var body = await EndpointHelpers.ReadBodyAsync(request, ct);
                return Results.Ok(await gateway.UpdatePatientAsync(id, body, fhir, ct));
            })
            .WithName("UpdatePatient")
            .WithSummary("Updates a Patient with a raw FHIR Patient JSON body.")
            .Accepts<string>(FhirSerialization.FhirJsonMediaType, "application/json")
            .Produces<ResourceWriteResult>();

        patients.MapDelete("/{id}", async (string id, FhirGatewayService gateway, CancellationToken ct) =>
            {
                await gateway.DeletePatientAsync(id, ct);
                return Results.NoContent();
            })
            .WithName("DeletePatient")
            .WithSummary("Deletes a Patient (HAPI soft-delete; the resource becomes HTTP 410 Gone).");

        patients.MapGet("/{id}/encounters", async (string id, FhirGatewayService gateway, FhirSerialization fhir, CancellationToken ct) =>
                EndpointHelpers.FhirJson(fhir, await gateway.SearchEncountersAsync(id, ct) ?? new Bundle { Type = Bundle.BundleType.Searchset }))
            .WithName("GetPatientEncounters")
            .WithSummary("Encounter?patient={id} as a FHIR searchset Bundle.");

        patients.MapGet("/{id}/conditions", async (string id, FhirGatewayService gateway, FhirSerialization fhir, CancellationToken ct) =>
                EndpointHelpers.FhirJson(fhir, await gateway.SearchConditionsAsync(id, ct) ?? new Bundle { Type = Bundle.BundleType.Searchset }))
            .WithName("GetPatientConditions")
            .WithSummary("Condition?patient={id} as a FHIR searchset Bundle.");

        patients.MapGet("/{id}/observations", async (string id, string? category, FhirGatewayService gateway, FhirSerialization fhir, CancellationToken ct) =>
                EndpointHelpers.FhirJson(fhir, await gateway.SearchObservationsAsync(id, category, ct) ?? new Bundle { Type = Bundle.BundleType.Searchset }))
            .WithName("GetPatientObservations")
            .WithSummary("Observation?patient={id}[&category=] as a FHIR searchset Bundle (category e.g. vital-signs, laboratory).");

        patients.MapGet("/{id}/everything", async (string id, FhirGatewayService gateway, FhirSerialization fhir, CancellationToken ct) =>
            {
                var bundle = await gateway.GetEverythingAsync(id, maxPages: 20, ct);
                var summary = bundle.Entry
                    .Where(e => e.Resource is not null)
                    .GroupBy(e => e.Resource!.TypeName)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.Count());
                return Results.Json(new { patientId = id, total = bundle.Entry.Count, summary, bundle = fhir.ToJsonNode(bundle) }, FhirSerialization.ApiJsonOptions);
            })
            .WithName("GetPatientEverything")
            .WithSummary("Patient/{id}/$everything: resource-type counts plus the full Bundle.");

        app.MapPost("/api/encounters", async (CreateEncounterRequest request, FhirGatewayService gateway, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(request.PatientId)) return Results.BadRequest(new { error = "patientId is required" });
                var result = await gateway.CreateEncounterAsync(request, ct);
                return Results.Created(result.Location, result);
            })
            .WithTags("Encounters")
            .WithName("CreateEncounter")
            .WithSummary("Creates a US Core Encounter {patientId, start, end, classCode(AMB|IMP|EMER), typeText, status}.")
            .Produces<ResourceWriteResult>(StatusCodes.Status201Created);

        return app;
    }
}
