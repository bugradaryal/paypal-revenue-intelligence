namespace PRI.Shared.Constants;

/// <summary>JWT içinde kullanılan claim isimleri (token üretimi ve doğrulaması aynı isimleri kullanır).</summary>
public static class AuthClaims
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Role = "role";

    /// <summary>API anahtarıyla doğrulanan Gateway istemcisinin kimliği.</summary>
    public const string ClientId = "client_id";
}
