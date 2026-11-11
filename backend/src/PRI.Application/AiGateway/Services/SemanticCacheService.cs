using Microsoft.Extensions.Logging;
using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.Common.DTOs;
using PRI.Domain.AiGateway;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Enums;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Application.AiGateway.Services;

/// <summary>
/// Semantic cache: önce tam eşleşme (ücretsiz), yoksa embedding benzerliği aranır. Güvenlik kuralları:
/// (1) istemciler arasında paylaşım yok, (2) bağlam (model, görev tipi, parametreler, önceki mesajlar) aynı olmalı,
/// (3) sorgudaki sayılar aynı olmalı, (4) benzerlik eşiği geçilmeli, (5) kesilmiş/boş yanıtlar saklanmaz.
/// Embedding alınamazsa (sağlayıcı hatası) cache sessizce yalnızca tam eşleşmeye düşer; istek asla bu yüzden başarısız olmaz.
/// </summary>
public sealed class SemanticCacheService(
    ISemanticCacheRepository entries,
    ICacheLookupRepository lookups,
    IEmbeddingProvider embeddings,
    IGatewayQueries queries,
    SemanticCacheSettings settings,
    TimeProvider timeProvider,
    ILogger<SemanticCacheService> logger) : ISemanticCacheService
{
    private const string Usd = "USD";

    public async Task<CacheLookupResult?> LookupAsync(CacheLookupInput input, CancellationToken cancellationToken)
    {
        if (!settings.Enabled || input.Messages.Count == 0 || input.Messages[^1].Role != "user")
        {
            return null;
        }

        var query = input.Messages[^1].Content;
        var normalized = SemanticCacheKey.NormalizeQuery(query);
        var queryHash = SemanticCacheKey.ComputeQueryHash(normalized);
        var fingerprint = SemanticCacheKey.ComputeNumbersFingerprint(query);
        var contextHash = SemanticCacheKey.ComputeContextHash(
            input.RequestedModel, input.TaskType, input.MaxOutputTokens, input.Temperature, input.ReasoningEffort,
            input.Messages.Take(input.Messages.Count - 1).Select(m => (m.Role, m.Content)));

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // 1) Tam eşleşme: embedding çağrısı gerekmez.
        var exact = await entries.FindExactAsync(input.ClientId, contextHash, queryHash, now, cancellationToken);
        if (exact is not null)
        {
            return new CacheLookupResult(
                input, contextHash, queryHash, fingerprint, null, 0, 0m, new CacheHit(exact, CacheOutcome.ExactHit, 1d));
        }

        // 2) Anlamsal eşleşme.
        float[]? vector = null;
        var tokens = 0;
        var cost = 0m;

        if (query.Length <= settings.MaxQueryCharacters)
        {
            try
            {
                var embedding = await embeddings.EmbedAsync(query, cancellationToken);
                vector = EmbeddingMath.Normalize(embedding.Vector);
                tokens = embedding.InputTokens;
                cost = embedding.CostUsd;
            }
            catch (AppException ex)
            {
                logger.LogWarning("Semantic cache embedding failed ({Code}); falling back to exact matching only.", ex.Code);
            }
        }

        CacheHit? hit = null;
        if (vector is not null)
        {
            hit = await FindSemanticAsync(input.ClientId, contextHash, fingerprint, vector, now, cancellationToken);
        }

        return new CacheLookupResult(input, contextHash, queryHash, fingerprint, vector, tokens, cost, hit);
    }

    public CacheLookup StageHit(CacheLookupResult lookup)
    {
        var hit = lookup.Hit ?? throw new InvalidOperationException("The lookup has no hit.");
        var now = timeProvider.GetUtcNow().UtcDateTime;

        hit.Entry.RegisterHit(now);

        var record = CacheLookup.Hit(
            lookup.Input.ClientId, hit.Entry, hit.Outcome == CacheOutcome.ExactHit, hit.Similarity,
            lookup.EmbeddingTokens, lookup.EmbeddingCostUsd, now);
        lookups.Add(record);

        return record;
    }

    public void StageMiss(CacheLookupResult lookup) =>
        lookups.Add(CacheLookup.Miss(
            lookup.Input.ClientId, lookup.EmbeddingTokens, lookup.EmbeddingCostUsd, timeProvider.GetUtcNow().UtcDateTime));

    public void StageStore(CacheLookupResult lookup, ProviderModel model, ProviderCompletionResult result, decimal costUsd)
    {
        // Kesilmiş (length), içerik filtresi vb. ile biten veya boş yanıtlar yeniden kullanılmaz.
        var complete = result.FinishReason is null or "stop";
        if (!complete || string.IsNullOrWhiteSpace(result.Content))
        {
            return;
        }

        entries.Add(SemanticCacheEntry.Create(
            lookup.Input.ClientId, lookup.ContextHash, lookup.QueryHash, lookup.NumbersFingerprint, lookup.Embedding,
            result.Content, model.Provider, model.ModelId, result.FinishReason, costUsd,
            settings.Ttl, timeProvider.GetUtcNow().UtcDateTime));
    }

    public async Task<CacheStatsDto> GetStatsAsync(CancellationToken cancellationToken)
    {
        var totals = await queries.GetCacheTotalsAsync(timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        var lookupCount = totals.Hits + totals.Misses;

        return new CacheStatsDto(
            settings.Enabled,
            settings.EffectiveThreshold,
            Math.Max(settings.TtlHours, 1),
            totals.Entries,
            totals.ActiveEntries,
            totals.Hits,
            totals.Misses,
            lookupCount == 0 ? 0d : Math.Round((double)totals.Hits / lookupCount, 4),
            MoneyDto.FromPrecise(totals.SavedCostUsd, Usd),
            MoneyDto.FromPrecise(totals.EmbeddingCostUsd, Usd));
    }

    public async Task<ClearCacheResultDto> ClearAsync(CancellationToken cancellationToken) =>
        new(await entries.DeleteAllAsync(cancellationToken));

    private async Task<CacheHit?> FindSemanticAsync(
        Guid clientId, string contextHash, string fingerprint, float[] vector, DateTime now, CancellationToken cancellationToken)
    {
        var candidates = await entries.ListCandidatesAsync(
            clientId, contextHash, now, Math.Max(settings.MaxCandidates, 1), cancellationToken);

        CacheCandidate? best = null;
        var bestScore = double.MinValue;

        foreach (var candidate in candidates)
        {
            if (candidate.NumbersFingerprint != fingerprint)
            {
                continue;
            }

            var score = EmbeddingMath.Similarity(vector, candidate.Embedding);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best is null || bestScore < settings.EffectiveThreshold)
        {
            return null;
        }

        var entry = await entries.GetByIdAsync(best.Id, cancellationToken);

        return entry is null || entry.IsExpired(now)
            ? null
            : new CacheHit(entry, CacheOutcome.SemanticHit, Math.Round(bestScore, 4));
    }
}
