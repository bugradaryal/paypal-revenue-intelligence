using System.Security.Claims;
using PRI.Shared.Constants;
using PRI.Shared.Exceptions;

namespace PRI.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>API anahtarıyla doğrulanan Gateway istemcisinin kimliği; panel kullanıcısı (JWT) için null.</summary>
    public static Guid? GetClientId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirst(AuthClaims.ClientId)?.Value, out var id) ? id : null;

    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(AuthClaims.UserId)?.Value;

        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedException("The access token does not contain a valid user id.");
    }
}
