using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PRI.Domain.AiGateway;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Shared.Constants;

namespace PRI.Api.Authentication;

public static class ApiKeyDefaults
{
    public const string Scheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    /// <summary>Gateway'in hem panel kullanıcısını (JWT) hem üçüncü taraf istemciyi (API anahtarı) kabul eden uçlar için.</summary>
    public const string BearerOrApiKey = "Bearer," + Scheme;
}

/// <summary>
/// "X-Api-Key" başlığındaki anahtarı özetleyip etkin bir Gateway istemcisiyle eşleştirir. Header yoksa bu şema sessiz kalır
/// (JWT şeması devreye girebilir), anahtar varsa ama geçersizse kimlik doğrulama başarısız olur.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IGatewayClientRepository clients) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyDefaults.HeaderName, out var values))
        {
            return AuthenticateResult.NoResult();
        }

        var key = values.ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            return AuthenticateResult.NoResult();
        }

        var client = await clients.GetActiveByApiKeyHashAsync(ApiKey.Hash(key), Context.RequestAborted);
        if (client is null)
        {
            return AuthenticateResult.Fail("Invalid or inactive API key.");
        }

        var identity = new ClaimsIdentity(
            [new Claim(AuthClaims.ClientId, client.Id.ToString()), new Claim(ClaimTypes.Name, client.Name)],
            Scheme.Name);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
