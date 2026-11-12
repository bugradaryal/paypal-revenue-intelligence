using PRI.Application.AiGateway.DTOs;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Enums;

namespace PRI.Application.AiGateway.Interfaces;

public sealed record OptimizedPrompt(IReadOnlyList<ProviderMessage> Messages, int CharactersBefore, int CharactersAfter, bool Applied)
{
    public int CharactersSaved => Math.Max(CharactersBefore - CharactersAfter, 0);

    /// <summary>Tahmin: ~4 karakter = 1 token.</summary>
    public int EstimatedTokensSaved => CharactersSaved / 4;

    public static OptimizedPrompt Unchanged(IReadOnlyList<ProviderMessage> messages)
    {
        var chars = messages.Sum(m => m.Content.Length);

        return new OptimizedPrompt(messages, chars, chars, false);
    }
}

public interface IPromptOptimizer
{
    /// <summary>Anlamı değiştirmeyen, deterministik temizlik uygular. Kod blokları (```) olduğu gibi bırakılır.</summary>
    OptimizedPrompt Optimize(IReadOnlyList<ProviderMessage> messages);
}

public sealed record CacheLookupInput(
    Guid ClientId,
    string? RequestedModel,
    string? TaskType,
    int? MaxOutputTokens,
    double? Temperature,
    string? ReasoningEffort,
    IReadOnlyList<ProviderMessage> Messages,
    AiProvider? Provider = null);

public sealed record CacheHit(SemanticCacheEntry Entry, CacheOutcome Outcome, double Similarity);

/// <param name="Hit">Null ise miss.</param>
public sealed record CacheLookupResult(
    CacheLookupInput Input,
    string ContextHash,
    string QueryHash,
    string NumbersFingerprint,
    float[]? Embedding,
    int EmbeddingTokens,
    decimal EmbeddingCostUsd,
    CacheHit? Hit);

/// <summary>
/// Semantic cache. Servis yalnızca değişiklikleri "hazırlar" (Stage*); kaydetme (SaveChanges) çağıran tarafın işidir,
/// böylece kullanım kaydı ve cache kaydı aynı işlemde yazılabilir.
/// </summary>
public interface ISemanticCacheService
{
    /// <summary>Cache bu istek için uygun değilse (kapalı, son mesaj kullanıcıdan değil) null döner.</summary>
    Task<CacheLookupResult?> LookupAsync(CacheLookupInput input, CancellationToken cancellationToken);

    /// <summary>Hit'i ve arama kaydını hazırlar; hit sayacını artırır.</summary>
    CacheLookup StageHit(CacheLookupResult lookup);

    void StageMiss(CacheLookupResult lookup);

    /// <summary>Yeni yanıtı cache'e ekler (kesilmiş veya boş yanıtlar eklenmez).</summary>
    void StageStore(CacheLookupResult lookup, ProviderModel model, ProviderCompletionResult result, decimal costUsd);

    Task<CacheStatsDto> GetStatsAsync(CancellationToken cancellationToken);

    Task<ClearCacheResultDto> ClearAsync(CancellationToken cancellationToken);
}

public interface IBudgetService
{
    Task<BudgetDto> GetAsync(CancellationToken cancellationToken);

    Task<BudgetDto> SetAsync(SetBudgetRequest request, CancellationToken cancellationToken);

    /// <summary>Limit aşıldıysa ve zorlama açıksa BusinessRuleException("budget_exceeded") fırlatır.</summary>
    Task EnsureWithinBudgetAsync(CancellationToken cancellationToken);
}

/// <summary>Belirtilen aralıktaki gerçek AI harcaması (tamamlama + embedding). Bütçe denetimi için.</summary>
public interface IGatewaySpendReader
{
    Task<decimal> GetSpentUsdAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
}

public sealed record CacheTotals(
    int Entries, int ActiveEntries, int Hits, int Misses, decimal SavedCostUsd, decimal EmbeddingCostUsd);
