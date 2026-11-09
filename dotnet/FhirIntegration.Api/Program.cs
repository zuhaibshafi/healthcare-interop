// FhirIntegration.Api — FHIR integration layer proof-of-concept
//
// Architecture (gateway / facade):
//
//   clients ──> [ this API : http://localhost:5080 ]
//                 ├─ /api/fhir/*, /api/patients/*, /api/encounters   FHIR gateway over HAPI (Firely FhirClient)
//                 ├─ /oauth/token                                    OAuth2 client-credentials facade (for eCRNow)
//                 ├─ /pha/r4/fhir/$process-message                   mock Public Health Agency eICR receiver
//                 ├─ /api/ecr/*                                      eCRNow client (launchPatient, settings)
//                 ├─ /api/udsplus/submit, /udsplus/files/*           UDS+ Data Submitter pipeline
//                 ├─ /udsplus/$import, /udsplus/$import-status/{id}  mock HRSA UDS+ Data Receiver
//                 └─ /health, /swagger
//
//   Upstreams: HAPI FHIR R4 (:8080), HL7 validator wrapper (:8082), eCRNow (:8081)
//
// Layout: Models/ (options + DTOs), Services/ (all integration logic, DI-registered), Endpoints/
// (minimal-API route groups, one file per concern). Program.cs only wires things together.

using FhirIntegration.Api.Endpoints;
using FhirIntegration.Api.Models;
using FhirIntegration.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Listen on a fixed port so eCRNow / HAPI-side configuration can point at it reliably.
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://localhost:5080");

// ---- configuration -------------------------------------------------------------------------
builder.Services.Configure<FhirOptions>(builder.Configuration.GetSection(FhirOptions.SectionName));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<UdsPlusOptions>(builder.Configuration.GetSection(UdsPlusOptions.SectionName));

var fhirOptions = builder.Configuration.GetSection(FhirOptions.SectionName).Get<FhirOptions>() ?? new FhirOptions();

// ---- named HttpClients (one per upstream so timeouts/headers can differ) -------------------
builder.Services.AddHttpClient(HttpClientNames.Hapi, c =>
{
    c.BaseAddress = new Uri(fhirOptions.BaseUrl.TrimEnd('/') + "/");
    c.Timeout = TimeSpan.FromMinutes(2);
});
builder.Services.AddHttpClient(HttpClientNames.Validator, c =>
{
    c.BaseAddress = new Uri(fhirOptions.ValidatorUrl.TrimEnd('/') + "/");
    c.Timeout = TimeSpan.FromMinutes(15); // first validation per IG set downloads packages
});
builder.Services.AddHttpClient(HttpClientNames.EcrNow, c => c.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddHttpClient(HttpClientNames.Self, c => c.Timeout = TimeSpan.FromMinutes(5));

// ---- services ------------------------------------------------------------------------------
builder.Services.AddSingleton<FhirSerialization>();
builder.Services.AddSingleton<OutputPaths>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<DeIdentifier>();
builder.Services.AddSingleton<UdsPlusManifestBuilder>();
builder.Services.AddSingleton<ValidatorClient>();          // holds the validator session cache
builder.Services.AddSingleton<UdsPlusReceiverService>();   // holds received submissions
builder.Services.AddSingleton<PhaReceiverService>();
builder.Services.AddScoped<FhirGatewayService>();
builder.Services.AddScoped<BulkDataExportClient>();
builder.Services.AddScoped<UdsPlusSubmitterService>();
builder.Services.AddScoped<EcrNowClient>();

// ---- API plumbing ---------------------------------------------------------------------------
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.WriteIndented = true;
    o.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xml = Path.Combine(AppContext.BaseDirectory, "FhirIntegration.Api.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);
});

var app = builder.Build();

// Global error handling: FHIR-style OperationOutcome for anything that escapes an endpoint.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    var fhir = context.RequestServices.GetRequiredService<FhirSerialization>();
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Unhandled");
    if (feature?.Error is { } ex)
    {
        logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        await EndpointHelpers.ToErrorResult(ex, fhir).ExecuteAsync(context);
    }
}));

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.DocumentTitle = "FHIR Integration Layer PoC";
});

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapHealthEndpoints();
app.MapFhirGatewayEndpoints();
app.MapAuthEndpoints();
app.MapPhaEndpoints();
app.MapEcrEndpoints();
app.MapUdsPlusEndpoints();

app.Logger.LogInformation("FHIR integration layer starting. HAPI={Hapi} Validator={Validator} eCRNow={Ecr} Output={Output}",
    fhirOptions.BaseUrl, fhirOptions.ValidatorUrl, fhirOptions.EcrNowUrl, fhirOptions.OutputDirectory);

app.Run();
