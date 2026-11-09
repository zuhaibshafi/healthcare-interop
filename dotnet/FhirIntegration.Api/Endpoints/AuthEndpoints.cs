using System.Text;
using FhirIntegration.Api.Models;
using FhirIntegration.Api.Services;
using Microsoft.Extensions.Options;

namespace FhirIntegration.Api.Endpoints;

/// <summary>
/// OAuth2 token facade.
///
/// POST /oauth/token implements the client_credentials grant for machine clients such as eCRNow
/// (auth type "System"). Client authentication is accepted either as HTTP Basic
/// (Authorization: Basic base64(client_id:client_secret)) or as client_id / client_secret form fields,
/// matching what SMART Backend Services clients and eCRNow send.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/oauth").WithTags("OAuth2 facade");

        group.MapPost("/token", async (HttpRequest request, TokenService tokens) =>
            {
                if (!request.HasFormContentType)
                    return OAuthError("invalid_request", "Content-Type must be application/x-www-form-urlencoded", StatusCodes.Status400BadRequest);

                var form = await request.ReadFormAsync();
                var grantType = form["grant_type"].ToString();
                if (!string.Equals(grantType, "client_credentials", StringComparison.Ordinal))
                    return OAuthError("unsupported_grant_type", "Only grant_type=client_credentials is supported", StatusCodes.Status400BadRequest);

                // Client credentials: HTTP Basic takes precedence, then form fields.
                string? clientId = null, clientSecret = null;
                var authHeader = request.Headers.Authorization.ToString();
                if (authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader["Basic ".Length..].Trim()));
                        var separator = decoded.IndexOf(':');
                        if (separator > 0)
                        {
                            clientId = Uri.UnescapeDataString(decoded[..separator]);
                            clientSecret = Uri.UnescapeDataString(decoded[(separator + 1)..]);
                        }
                    }
                    catch (FormatException) { /* fall through to form fields */ }
                }
                clientId ??= form["client_id"].ToString();
                clientSecret ??= form["client_secret"].ToString();

                var client = tokens.Authenticate(clientId, clientSecret);
                if (client is null)
                    return OAuthError("invalid_client", "Unknown client_id or bad client_secret", StatusCodes.Status401Unauthorized);

                var token = tokens.Issue(client, form["scope"].ToString());
                return Results.Json(token, contentType: "application/json");
            })
            .WithName("Token")
            .WithSummary("OAuth2 client_credentials token endpoint (form-encoded). Returns {access_token, token_type, expires_in, scope}.")
            .Accepts<IFormCollection>("application/x-www-form-urlencoded")
            .DisableAntiforgery()
            .Produces<TokenService.TokenResponse>();

        // Discovery document so SMART-style clients can find the token endpoint automatically.
        app.MapGet("/.well-known/smart-configuration", (IOptions<AuthOptions> auth, IOptions<FhirOptions> fhir) => Results.Json(new
            {
                issuer = auth.Value.Issuer,
                token_endpoint = $"{fhir.Value.PublicBaseUrl.TrimEnd('/')}/oauth/token",
                grant_types_supported = new[] { "client_credentials" },
                token_endpoint_auth_methods_supported = new[] { "client_secret_basic", "client_secret_post" },
                scopes_supported = auth.Value.Clients.SelectMany(c => c.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Distinct().ToArray(),
                capabilities = new[] { "client-confidential-symmetric" }
            }))
            .WithTags("OAuth2 facade")
            .WithName("SmartConfiguration")
            .WithSummary("SMART discovery document pointing at this token facade.");

        return app;
    }

    private static IResult OAuthError(string error, string description, int status) =>
        Results.Json(new { error, error_description = description }, statusCode: status);
}
