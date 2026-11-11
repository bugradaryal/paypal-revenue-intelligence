using PRI.Domain.AiGateway.Enums;
using PRI.Domain.Common;

namespace PRI.Domain.AiGateway.Entities;

/// <summary>
/// Cache aramasının değiştirilemez kaydı (hit veya miss). Hit oranı, kaçınılan maliyet ve embedding maliyeti bu kayıtlardan hesaplanır.
/// Embedding maliyeti gerçek token sayısı × yapılandırılmış resmi fiyattır; cache kapalıyken veya atlanırken kayıt yazılmaz.
/// </summary>
public sealed class CacheLookup : BaseEntity
{
    // EF Core için.
    private CacheLookup()
    {
    }

    public Guid ClientId { get; private set; }

    public CacheOutcome Outcome { get; private set; }

    /// <summary>Anlamsal hit'te benzerlik (0..1); diğerlerinde null.</summary>
    public double? Similarity { get; private set; }

    /// <summary>Hit'te kullanılan cache kaydı (düz Guid; kayıt silinse de geçmiş kalır).</summary>
    public Guid? CacheEntryId { get; private set; }

    public int EmbeddingTokens { get; private set; }

    public decimal EmbeddingCostUsd { get; private set; }

    /// <summary>Hit'te yanıtın ilk üretiminde ödenen maliyet (bu istekte ödenmeyen tutar); miss'te 0.</summary>
    public decimal SavedCostUsd { get; private set; }

    public DateTime RequestedAtUtc { get; private set; }

    public bool IsHit => Outcome != CacheOutcome.Miss;

    public static CacheLookup Miss(Guid clientId, int embeddingTokens, decimal embeddingCostUsd, DateTime nowUtc) =>
        new()
        {
            ClientId = clientId,
            Outcome = CacheOutcome.Miss,
            EmbeddingTokens = embeddingTokens,
            EmbeddingCostUsd = embeddingCostUsd,
            RequestedAtUtc = nowUtc,
            CreatedAtUtc = nowUtc
        };

    public static CacheLookup Hit(
        Guid clientId, SemanticCacheEntry entry, bool isExact, double similarity,
        int embeddingTokens, decimal embeddingCostUsd, DateTime nowUtc) =>
        new()
        {
            ClientId = clientId,
            Outcome = isExact ? CacheOutcome.ExactHit : CacheOutcome.SemanticHit,
            Similarity = isExact ? null : similarity,
            CacheEntryId = entry.Id,
            EmbeddingTokens = embeddingTokens,
            EmbeddingCostUsd = embeddingCostUsd,
            SavedCostUsd = entry.OriginalCostUsd,
            RequestedAtUtc = nowUtc,
            CreatedAtUtc = nowUtc
        };
}
