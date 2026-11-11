using PRI.Application.Common.DTOs;
using PRI.Domain.AiGateway.Enums;

namespace PRI.Application.AiGateway.DTOs;

/// <param name="Status">"hit" | "miss" | "bypass" (cache kapalı, istekte kapatıldı veya istek önbelleğe uygun değil).</param>
/// <param name="MatchType">Hit'te "exact" veya "semantic".</param>
/// <param name="Similarity">Yalnızca "semantic" hit'te, 0..1.</param>
/// <param name="SavedCost">Hit'te yanıtın ilk üretiminde ödenen, bu istekte ödenmeyen tutar (kaçınılan maliyet varsayımıdır: aynı istek yeniden gönderilseydi benzer maliyet doğardı).</param>
/// <param name="EmbeddingCost">Bu isteğin cache araması için ödenen gerçek embedding maliyeti (tam eşleşmede ve bypass'ta 0).</param>
public sealed record CompletionCacheDto(
    string Status,
    string? MatchType,
    double? Similarity,
    MoneyDto? SavedCost,
    Guid? EntryId,
    MoneyDto EmbeddingCost);

/// <param name="Applied">Optimizasyon çalıştırıldı mı (istekte kapatılabilir).</param>
/// <param name="EstimatedTokensSaved">Kaldırılan karakter / 4. Bu bir TAHMİNDİR; gerçek token sayıları yanıttaki usage alanındadır.</param>
public sealed record PromptOptimizationDto(
    bool Applied,
    int CharactersBefore,
    int CharactersAfter,
    int CharactersSaved,
    int EstimatedTokensSaved);

public sealed record OptimizePromptRequest(IReadOnlyList<CompletionMessageDto> Messages);

public sealed record OptimizePromptResponse(
    IReadOnlyList<CompletionMessageDto> Messages,
    PromptOptimizationDto Optimization);

/// <param name="Limit">Aylık limit, USD. Tutar string'dir (ör. "50.00").</param>
/// <param name="WarnAtPercent">1..100, boşsa 80.</param>
/// <param name="EnforceLimit">Boşsa true: limit aşılınca yeni sağlayıcı çağrıları reddedilir.</param>
public sealed record SetBudgetRequest(MoneyDto Limit, int? WarnAtPercent, bool? EnforceLimit);

/// <param name="IsConfigured">Bütçe tanımlı değilse false; limit ve ilişkili alanlar null'dır.</param>
/// <param name="Spent">Dönemdeki (UTC takvim ayı) gerçek harcama: tamamlama + embedding maliyeti.</param>
/// <param name="UsedPercent">Harcama / limit × 100 (100'ü aşabilir).</param>
public sealed record BudgetDto(
    bool IsConfigured,
    string Period,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    MoneyDto? Limit,
    MoneyDto Spent,
    MoneyDto? Remaining,
    double? UsedPercent,
    int? WarnAtPercent,
    bool? EnforceLimit,
    BudgetLevel? Level,
    DateTime? UpdatedAt);

/// <param name="Enabled">Cache yapılandırmada açık mı.</param>
/// <param name="Hits">Tüm zamanlar; tam + anlamsal.</param>
/// <param name="SavedCost">Tüm zamanlar, hit'lerde kaçınılan maliyet (varsayım, bkz. CompletionCache).</param>
/// <param name="EmbeddingCost">Tüm zamanlar, cache aramaları için ödenen gerçek embedding maliyeti.</param>
public sealed record CacheStatsDto(
    bool Enabled,
    double SimilarityThreshold,
    int TtlHours,
    int Entries,
    int ActiveEntries,
    int Hits,
    int Misses,
    double HitRate,
    MoneyDto SavedCost,
    MoneyDto EmbeddingCost);

public sealed record ClearCacheResultDto(int DeletedCount);
