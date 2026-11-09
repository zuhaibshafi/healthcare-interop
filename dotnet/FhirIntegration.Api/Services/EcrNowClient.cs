using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using FhirIntegration.Api.Models;
using Microsoft.Extensions.Options;

namespace FhirIntegration.Api.Services;

/// <summary>
/// Client for the eCRNow (electronic case reporting) application's REST API.
///
/// eCRNow is launched per patient/encounter; it then reads the FHIR server configured in its
/// healthcare setting, evaluates the eRSD trigger codes, builds an eICR and posts it to the PHA
/// endpoint (which, in this PoC, is our own /pha/r4/fhir/$process-message).
/// </summary>
public sealed class EcrNowClient
{
    private readonly HttpClient _http;
    private readonly FhirOptions _options;

    public EcrNowClient(IHttpClientFactory factory, IOptions<FhirOptions> options)
    {
        _http = factory.CreateClient(HttpClientNames.EcrNow);
        _options = options.Value;
    }

    /// <summary>
    /// POST /api/launchPatient — tells eCRNow to start processing a patient encounter.
    /// eCRNow rejects requests without an X-Request-ID header (422), so one is always generated.
    /// </summary>
    public async Task<UpstreamResponse> LaunchPatientAsync(EcrLaunchRequest request, CancellationToken ct)
    {
        var url = $"{_options.EcrNowUrl.TrimEnd('/')}/api/launchPatient";
        var payload = new
        {
            fhirServerURL = _options.BaseUrl,
            patientId = request.PatientId,
            encounterId = request.EncounterId,
            validationMode = request.ValidationMode
        };
        using var message = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(payload) };
        message.Headers.TryAddWithoutValidation("X-Request-ID", Guid.NewGuid().ToString());
        using var response = await _http.SendAsync(message, ct);
        return await ToUpstreamAsync(response, url, ct);
    }

    /// <summary>GET /api/healthcareSettings/ — lists eCRNow's configured healthcare settings.</summary>
    public async Task<UpstreamResponse> GetHealthcareSettingsAsync(CancellationToken ct)
    {
        var url = $"{_options.EcrNowUrl.TrimEnd('/')}/api/healthcareSettings/";
        using var message = new HttpRequestMessage(HttpMethod.Get, url);
        message.Headers.TryAddWithoutValidation("X-Request-ID", Guid.NewGuid().ToString());
        using var response = await _http.SendAsync(message, ct);
        return await ToUpstreamAsync(response, url, ct);
    }

    private static async Task<UpstreamResponse> ToUpstreamAsync(HttpResponseMessage response, string url, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        JsonNode? node = null;
        try { node = string.IsNullOrWhiteSpace(body) ? null : JsonNode.Parse(body); } catch (JsonException) { /* non-JSON body */ }
        return new UpstreamResponse((int)response.StatusCode, response.Content.Headers.ContentType?.ToString(), node, node is null ? body : null, url);
    }
}
