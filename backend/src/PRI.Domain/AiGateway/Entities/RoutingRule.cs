using PRI.Domain.Common;

namespace PRI.Domain.AiGateway.Entities;

/// <summary>
/// Görev tipine göre model seçim kuralı: ana model ve sırayla denenecek yedek modeller. Kural tabanlıdır ve
/// yönlendirme için ayrıca bir LLM çağrısı yapılmaz (maliyet ve gecikme eklememek için).
/// </summary>
public sealed class RoutingRule : BaseEntity
{
    public const string DefaultTaskType = "default";

    // EF Core için.
    private RoutingRule()
    {
    }

    /// <summary>Küçük harfli görev tipi (ör. "analysis"). "default" kuralı, eşleşme olmayan istekler içindir.</summary>
    public string TaskType { get; private set; } = string.Empty;

    public Guid PrimaryModelId { get; private set; }

    /// <summary>Ana model başarısız olursa sırayla denenecek model kimlikleri (uuid[]).</summary>
    public Guid[] FallbackModelIds { get; private set; } = [];

    public static RoutingRule Create(string taskType, Guid primaryModelId, IEnumerable<Guid> fallbackModelIds, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskType);

        return new RoutingRule
        {
            TaskType = taskType.Trim().ToLowerInvariant(),
            PrimaryModelId = primaryModelId,
            FallbackModelIds = fallbackModelIds.Where(id => id != primaryModelId).Distinct().ToArray(),
            CreatedAtUtc = nowUtc
        };
    }

    public void Update(Guid primaryModelId, IEnumerable<Guid> fallbackModelIds)
    {
        PrimaryModelId = primaryModelId;
        FallbackModelIds = fallbackModelIds.Where(id => id != primaryModelId).Distinct().ToArray();
    }
}
