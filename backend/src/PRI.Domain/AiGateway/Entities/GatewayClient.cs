using PRI.Domain.Common;

namespace PRI.Domain.AiGateway.Entities;

/// <summary>Gateway'i kullanan istemci. İlk uygulamamızın kendisi (first-party, JWT ile) ve API anahtarıyla gelen üçüncü taraflar.</summary>
public sealed class GatewayClient : BaseEntity
{
    // EF Core için.
    private GatewayClient()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public bool IsFirstParty { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>API anahtarının SHA-256 özeti (hex). First-party istemcide yoktur.</summary>
    public string? ApiKeyHash { get; private set; }

    /// <summary>Anahtarın başındaki gösterim öneki (ör. "pri_AbCdEf"); anahtarı listede tanımak için.</summary>
    public string? ApiKeyPrefix { get; private set; }

    public static GatewayClient CreateFirstParty(string name, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new GatewayClient
        {
            Name = name.Trim(),
            IsFirstParty = true,
            IsActive = true,
            CreatedAtUtc = nowUtc
        };
    }

    public static GatewayClient CreateThirdParty(string name, string apiKeyHash, string apiKeyPrefix, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKeyHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKeyPrefix);

        return new GatewayClient
        {
            Name = name.Trim(),
            IsFirstParty = false,
            IsActive = true,
            ApiKeyHash = apiKeyHash,
            ApiKeyPrefix = apiKeyPrefix,
            CreatedAtUtc = nowUtc
        };
    }

    public void Deactivate() => IsActive = false;
}
