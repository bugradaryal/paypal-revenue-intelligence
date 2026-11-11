namespace PRI.Infrastructure.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    /// <summary>HMAC-SHA256 imza anahtarı, en az 32 karakter. Production'da environment variable ile verilir.</summary>
    public string Key { get; init; } = string.Empty;

    public int ExpiresInMinutes { get; init; } = 60;
}
