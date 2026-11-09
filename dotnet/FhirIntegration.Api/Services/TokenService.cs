using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FhirIntegration.Api.Models;
using Microsoft.Extensions.Options;

namespace FhirIntegration.Api.Services;

/// <summary>
/// Minimal OAuth2 client-credentials token issuer (the "token facade").
///
/// Purpose: eCRNow's healthcare-setting can be configured with auth type "System" (SMART Backend
/// Services / client credentials). The local HAPI server is open and has no token endpoint, so this
/// service stands in as the authorization server: it validates the client against appsettings
/// "Auth:Clients" and issues a short-lived HS256 JWT. Nothing downstream verifies the token in this
/// PoC (HAPI is open); in production the gateway would validate it on every FHIR call instead.
///
/// The JWT is hand-rolled (header.payload.signature, base64url) to avoid an extra dependency; the
/// format is standard so any JWT library can decode it.
/// </summary>
public sealed class TokenService
{
    private readonly AuthOptions _options;

    public TokenService(IOptions<AuthOptions> options) => _options = options.Value;

    public sealed record TokenResponse(string access_token, string token_type, int expires_in, string scope);

    public OAuthClient? Authenticate(string? clientId, string? clientSecret)
    {
        if (string.IsNullOrEmpty(clientId) || clientSecret is null) return null;
        var client = _options.Clients.FirstOrDefault(c => string.Equals(c.ClientId, clientId, StringComparison.Ordinal));
        if (client is null) return null;
        return FixedTimeEquals(client.ClientSecret, clientSecret) ? client : null;
    }

    public TokenResponse Issue(OAuthClient client, string? requestedScope)
    {
        var grantedScope = string.IsNullOrWhiteSpace(requestedScope) ? client.Scopes : IntersectScopes(client.Scopes, requestedScope);
        var now = DateTimeOffset.UtcNow;
        var header = new { alg = "HS256", typ = "JWT" };
        var payload = new
        {
            iss = _options.Issuer,
            sub = client.ClientId,
            aud = _options.Issuer,
            client_id = client.ClientId,
            scope = grantedScope,
            iat = now.ToUnixTimeSeconds(),
            exp = now.AddSeconds(_options.TokenLifetimeSeconds).ToUnixTimeSeconds(),
            jti = Guid.NewGuid().ToString("N")
        };

        var signingInput = Base64Url(JsonSerializer.SerializeToUtf8Bytes(header)) + "." + Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload));
        var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(_options.SigningKey), Encoding.UTF8.GetBytes(signingInput));
        var jwt = signingInput + "." + Base64Url(signature);

        return new TokenResponse(jwt, "Bearer", _options.TokenLifetimeSeconds, grantedScope);
    }

    private static string IntersectScopes(string granted, string requested)
    {
        var allowed = granted.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var result = requested.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(s => allowed.Contains(s) || allowed.Contains("system/*.read") && s.EndsWith(".read") || allowed.Contains("system/*.write") && s.EndsWith(".write"))
            .ToList();
        return result.Count > 0 ? string.Join(' ', result) : granted;
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
