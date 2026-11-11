using PRI.Domain.AiGateway.Enums;
using PRI.Domain.Common;

namespace PRI.Domain.AiGateway.Entities;

/// <summary>
/// Başarılı bir sağlayıcı yanıtının önbellek kaydı. İstek metni saklanmaz (yalnızca özeti ve embedding'i); yanıt metni,
/// yeniden kullanılabilmesi için saklanır. Kayıtlar istemciler arasında asla paylaşılmaz.
/// </summary>
public sealed class SemanticCacheEntry : BaseEntity
{
    // EF Core için.
    private SemanticCacheEntry()
    {
    }

    public Guid ClientId { get; private set; }

    public string ContextHash { get; private set; } = string.Empty;

    public string QueryHash { get; private set; } = string.Empty;

    public string NumbersFingerprint { get; private set; } = string.Empty;

    /// <summary>Birim uzunluğa normalize edilmiş sorgu embedding'i; embedding alınamadıysa null (yalnızca tam eşleşmeyle bulunur).</summary>
    public float[]? Embedding { get; private set; }

    public string ResponseContent { get; private set; } = string.Empty;

    public AiProvider Provider { get; private set; }

    public string ModelId { get; private set; } = string.Empty;

    public string? FinishReason { get; private set; }

    /// <summary>Yanıt ilk üretildiğinde ödenen gerçek maliyet; her hit'te "kaçınılan maliyet" olarak sayılır.</summary>
    public decimal OriginalCostUsd { get; private set; }

    public int HitCount { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? LastHitAtUtc { get; private set; }

    public static SemanticCacheEntry Create(
        Guid clientId,
        string contextHash,
        string queryHash,
        string numbersFingerprint,
        float[]? embedding,
        string responseContent,
        AiProvider provider,
        string modelId,
        string? finishReason,
        decimal originalCostUsd,
        TimeSpan ttl,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contextHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(responseContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        ArgumentOutOfRangeException.ThrowIfNegative(originalCostUsd);

        return new SemanticCacheEntry
        {
            ClientId = clientId,
            ContextHash = contextHash,
            QueryHash = queryHash,
            NumbersFingerprint = numbersFingerprint,
            Embedding = embedding,
            ResponseContent = responseContent,
            Provider = provider,
            ModelId = modelId,
            FinishReason = finishReason,
            OriginalCostUsd = originalCostUsd,
            ExpiresAtUtc = nowUtc.Add(ttl),
            CreatedAtUtc = nowUtc
        };
    }

    public bool IsExpired(DateTime nowUtc) => ExpiresAtUtc <= nowUtc;

    public void RegisterHit(DateTime nowUtc)
    {
        HitCount++;
        LastHitAtUtc = nowUtc;
    }
}
