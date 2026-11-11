using Microsoft.EntityFrameworkCore;
using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.Common.DTOs;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Enums;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Queries;

/// <summary>
/// Gateway kullanım sorguları. Tüm tutarlar kayıtlı gerçek kullanımdan (token × istek anındaki resmi fiyat) toplanır;
/// hiçbir değer tahmin edilmez. Yalnızca başarılı (sağlayıcıdan yanıt alınan) denemeler sayılır.
/// </summary>
public sealed class GatewayQueries(AppDbContext db, TimeProvider timeProvider) : IGatewayQueries, IGatewaySpendReader
{
    private const string Usd = "USD";

    public async Task<UsageSummaryDto> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var rows = SuccessfulIn(fromUtc, toUtc);

        var totalCost = await rows.SumAsync(u => u.CostUsd, cancellationToken);
        var requests = await rows.CountAsync(cancellationToken);
        var inputTokens = await rows.SumAsync(u => (long)u.InputTokens, cancellationToken);
        var outputTokens = await rows.SumAsync(u => (long)u.OutputTokens, cancellationToken);

        // Tasarruf yalnızca otomatik yönlendirilen ve baz maliyeti kaydedilen isteklerden gelir; negatif olabilir.
        var savings = await rows
            .Where(u => u.BaselineCostUsd != null)
            .SumAsync(u => u.BaselineCostUsd!.Value - u.CostUsd, cancellationToken);

        // Prompt optimizasyonu: kaldırılan karakter ölçülmüştür; token ve maliyet karşılığı TAHMİNDİR (karakter / 4).
        var optimized = rows.Where(u => u.PromptCharsSaved > 0);
        var optimizedRequests = await optimized.CountAsync(cancellationToken);
        var charsSaved = await optimized.SumAsync(u => (long)u.PromptCharsSaved, cancellationToken);
        var estTokensSaved = await optimized.SumAsync(u => (long)(u.PromptCharsSaved / 4), cancellationToken);
        var estOptimizationSavings = await optimized.SumAsync(
            u => (decimal)(u.PromptCharsSaved / 4) * u.InputPricePerMillionUsd / 1_000_000m, cancellationToken);

        var lookups = db.CacheLookups.AsNoTracking().Where(l => l.RequestedAtUtc >= fromUtc && l.RequestedAtUtc < toUtc);
        var misses = await lookups.CountAsync(l => l.Outcome == CacheOutcome.Miss, cancellationToken);
        var hits = await lookups.CountAsync(l => l.Outcome != CacheOutcome.Miss, cancellationToken);
        var cacheSaved = await lookups.SumAsync(l => l.SavedCostUsd, cancellationToken);
        var embeddingCost = await lookups.SumAsync(l => l.EmbeddingCostUsd, cancellationToken);
        var hitRate = hits + misses == 0 ? 0d : Math.Round((double)hits / (hits + misses), 4);

        BudgetStatusDto? budget = null;
        var configured = await db.AiBudgets.AsNoTracking().OrderBy(b => b.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (configured is not null)
        {
            var (monthStart, monthEnd) = AiBudget.PeriodContaining(timeProvider.GetUtcNow().UtcDateTime);
            budget = new BudgetStatusDto(
                Money(configured.MonthlyLimitUsd), Money(await GetSpentUsdAsync(monthStart, monthEnd, cancellationToken)), "monthly");
        }

        return new UsageSummaryDto(
            new DateRangeDto(fromUtc, toUtc),
            Money(totalCost),
            requests,
            inputTokens,
            outputTokens,
            hitRate,
            Money(savings),
            budget,
            new CacheSummaryDto(hits, misses, hitRate, Money(cacheSaved), Money(embeddingCost)),
            new PromptOptimizationSummaryDto(optimizedRequests, charsSaved, estTokensSaved, Money(estOptimizationSavings)));
    }

    public async Task<UsageSeriesDto> GetSeriesAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var rows = await SuccessfulIn(fromUtc, toUtc)
            .Select(u => new { u.RequestedAtUtc, u.CostUsd })
            .ToListAsync(cancellationToken);

        // Gün bucket'ları UTC gününe göredir; boş günler sıfırla doldurulur.
        var byDay = rows
            .GroupBy(r => DateOnly.FromDateTime(r.RequestedAtUtc))
            .ToDictionary(g => g.Key, g => (Cost: g.Sum(r => r.CostUsd), Count: g.Count()));

        var items = new List<UsageSeriesPointDto>();
        var lastDay = DateOnly.FromDateTime(toUtc.AddTicks(-1));

        for (var day = DateOnly.FromDateTime(fromUtc); day <= lastDay; day = day.AddDays(1))
        {
            byDay.TryGetValue(day, out var bucket);
            items.Add(new UsageSeriesPointDto(
                day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Money(bucket.Cost), bucket.Count));
        }

        return new UsageSeriesDto(new DateRangeDto(fromUtc, toUtc), items);
    }

    public async Task<UsageBreakdownDto> GetBreakdownAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var rows = SuccessfulIn(fromUtc, toUtc);

        var byModel = await rows
            .GroupBy(u => u.ModelId)
            .Select(g => new
            {
                Key = g.Key,
                Cost = g.Sum(u => u.CostUsd),
                Requests = g.Count(),
                Input = g.Sum(u => (long)u.InputTokens),
                Output = g.Sum(u => (long)u.OutputTokens)
            })
            .ToListAsync(cancellationToken);

        var byProvider = await rows
            .GroupBy(u => u.Provider)
            .Select(g => new
            {
                Key = g.Key,
                Cost = g.Sum(u => u.CostUsd),
                Requests = g.Count(),
                Input = g.Sum(u => (long)u.InputTokens),
                Output = g.Sum(u => (long)u.OutputTokens)
            })
            .ToListAsync(cancellationToken);

        var displayNames = await db.ProviderModels.AsNoTracking()
            .ToDictionaryAsync(m => m.ModelId, m => m.DisplayName, cancellationToken);

        return new UsageBreakdownDto(
            new DateRangeDto(fromUtc, toUtc),
            byModel
                .OrderByDescending(x => x.Cost)
                .Select(x => new UsageBreakdownItemDto(
                    x.Key,
                    displayNames.GetValueOrDefault(x.Key, x.Key),
                    Money(x.Cost), x.Requests, x.Input, x.Output))
                .ToList(),
            byProvider
                .OrderByDescending(x => x.Cost)
                .Select(x => new UsageBreakdownItemDto(
                    ProviderKey(x.Key),
                    ProviderLabel(x.Key),
                    Money(x.Cost), x.Requests, x.Input, x.Output))
                .ToList());
    }

    /// <summary>Aralıktaki gerçek harcama: başarılı tamamlamalar + cache araması embedding maliyeti.</summary>
    public async Task<decimal> GetSpentUsdAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var completions = await SuccessfulIn(fromUtc, toUtc).SumAsync(u => u.CostUsd, cancellationToken);
        var embeddings = await db.CacheLookups.AsNoTracking()
            .Where(l => l.RequestedAtUtc >= fromUtc && l.RequestedAtUtc < toUtc)
            .SumAsync(l => l.EmbeddingCostUsd, cancellationToken);

        return completions + embeddings;
    }

    public async Task<CacheTotals> GetCacheTotalsAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var entries = await db.SemanticCacheEntries.AsNoTracking().CountAsync(cancellationToken);
        var active = await db.SemanticCacheEntries.AsNoTracking().CountAsync(e => e.ExpiresAtUtc > nowUtc, cancellationToken);

        var lookups = db.CacheLookups.AsNoTracking();
        var misses = await lookups.CountAsync(l => l.Outcome == CacheOutcome.Miss, cancellationToken);
        var hits = await lookups.CountAsync(l => l.Outcome != CacheOutcome.Miss, cancellationToken);
        var saved = await lookups.SumAsync(l => l.SavedCostUsd, cancellationToken);
        var embeddingCost = await lookups.SumAsync(l => l.EmbeddingCostUsd, cancellationToken);

        return new CacheTotals(entries, active, hits, misses, saved, embeddingCost);
    }

    private IQueryable<UsageRecord> SuccessfulIn(DateTime fromUtc, DateTime toUtc) =>
        db.UsageRecords.AsNoTracking()
            .Where(u => u.Status == UsageStatus.Success && u.RequestedAtUtc >= fromUtc && u.RequestedAtUtc < toUtc);

    private static MoneyDto Money(decimal amount) => MoneyDto.FromPrecise(amount, Usd);

    private static string ProviderKey(AiProvider provider) => provider switch
    {
        AiProvider.Openai => "openai",
        _ => provider.ToString().ToLowerInvariant()
    };

    private static string ProviderLabel(AiProvider provider) => provider switch
    {
        AiProvider.Openai => "OpenAI",
        _ => provider.ToString()
    };
}
