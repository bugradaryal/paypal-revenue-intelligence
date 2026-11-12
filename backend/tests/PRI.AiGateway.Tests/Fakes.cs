using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Enums;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Domain.Common.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.AiGateway.Tests;

internal static class TestData
{
    public static readonly DateTime Now = new(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);

    public static ProviderModel Model(
        string modelId, ModelTier tier, decimal input, decimal? cached, decimal output,
        bool baseline = false, bool enabled = true)
    {
        var model = ProviderModel.Create(
            AiProvider.Openai, modelId, modelId, tier, input, cached, output,
            "https://developers.openai.com/api/docs/pricing", new DateOnly(2026, 11, 7), Now);

        model.SetBaseline(baseline);
        model.SetEnabled(enabled);

        return model;
    }

    // Katalogtaki gerçek fiyatlarla aynı: luna 0.10/0.01/0.50, terra 1/0.05/5, sol 2/0.10/10
    public static ProviderModel Luna(bool enabled = true) => Model("gpt-6-luna", ModelTier.Economy, 0.10m, 0.01m, 0.50m, enabled: enabled);

    public static ProviderModel Terra(bool baseline = true) => Model("gpt-6.1-terra", ModelTier.Standard, 1.00m, 0.05m, 5.00m, baseline);

    public static ProviderModel Sol() => Model("gpt-6.1-sol", ModelTier.Premium, 2.00m, 0.10m, 10.00m);
}

internal sealed class FakeProviderModelRepository(IEnumerable<ProviderModel> models) : IProviderModelRepository
{
    private readonly List<ProviderModel> _models = models.ToList();

    public Task<IReadOnlyList<ProviderModel>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProviderModel>>(_models);

    public Task<ProviderModel?> GetCheapestEnabledAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_models.Where(m => m.IsEnabled).OrderBy(m => m.OutputPricePerMillionUsd).FirstOrDefault());

    public Task<ProviderModel?> GetBaselineAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_models.FirstOrDefault(m => m.IsBaseline));
}

internal sealed class FakeRoutingRuleRepository(IEnumerable<RoutingRule> rules) : IRoutingRuleRepository
{
    private readonly List<RoutingRule> _rules = rules.ToList();

    public Task<RoutingRule?> GetByTaskTypeAsync(string taskType, CancellationToken cancellationToken) =>
        Task.FromResult(_rules.FirstOrDefault(r => r.TaskType == taskType));

    public Task<IReadOnlyList<RoutingRule>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RoutingRule>>(_rules.ToList());

    public Task<RoutingRule?> GetTrackedByTaskTypeAsync(string taskType, CancellationToken cancellationToken) =>
        Task.FromResult(_rules.FirstOrDefault(r => r.TaskType == taskType));

    public void Add(RoutingRule rule) => _rules.Add(rule);
}

internal sealed class FakeGatewayClientRepository : IGatewayClientRepository
{
    public List<GatewayClient> Clients { get; } = [];

    public Task<GatewayClient?> GetFirstPartyAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Clients.FirstOrDefault(c => c.IsFirstParty && c.IsActive));

    public Task<GatewayClient?> GetActiveByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken) =>
        Task.FromResult(Clients.FirstOrDefault(c => c.IsActive && c.ApiKeyHash == apiKeyHash));

    public Task<GatewayClient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Clients.FirstOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<GatewayClient>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<GatewayClient>>(Clients.ToList());

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult(Clients.Any(c => c.Name == name));

    public void Add(GatewayClient client) => Clients.Add(client);
}

internal sealed class FakeUsageRecordRepository : IUsageRecordRepository
{
    public List<UsageRecord> Records { get; } = [];

    public void Add(UsageRecord record) => Records.Add(record);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.FromResult(0);
    }
}

/// <summary>Her çağrıda verilen davranışı çalıştıran sahte sağlayıcı; hangi modellerin çağrıldığını kaydeder.</summary>
internal sealed class ScriptedProvider(
    Func<ProviderCompletionRequest, ProviderCompletionResult> behavior,
    AiProvider provider = AiProvider.Openai) : IAiProvider
{
    public AiProvider Provider => provider;

    public List<string> CalledModels { get; } = [];

    public Task<ProviderCompletionResult> CompleteAsync(ProviderCompletionRequest request, CancellationToken cancellationToken)
    {
        CalledModels.Add(request.ModelId);
        return Task.FromResult(behavior(request));
    }
}

internal sealed class ManualTimeProvider(DateTime startUtc) : TimeProvider
{
    private DateTime _now = startUtc;

    public override DateTimeOffset GetUtcNow() => new(_now, TimeSpan.Zero);

    public void Advance(TimeSpan by) => _now += by;
}

internal sealed class FakeSemanticCacheRepository : ISemanticCacheRepository
{
    public List<SemanticCacheEntry> Entries { get; } = [];

    public Task<SemanticCacheEntry?> FindExactAsync(
        Guid clientId, string contextHash, string queryHash, DateTime nowUtc, CancellationToken cancellationToken) =>
        Task.FromResult(Entries
            .Where(e => e.ClientId == clientId && e.ContextHash == contextHash && e.QueryHash == queryHash && !e.IsExpired(nowUtc))
            .OrderByDescending(e => e.CreatedAtUtc)
            .FirstOrDefault());

    public Task<IReadOnlyList<CacheCandidate>> ListCandidatesAsync(
        Guid clientId, string contextHash, DateTime nowUtc, int maxCandidates, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CacheCandidate>>(Entries
            .Where(e => e.ClientId == clientId && e.ContextHash == contextHash && !e.IsExpired(nowUtc) && e.Embedding is not null)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(maxCandidates)
            .Select(e => new CacheCandidate(e.Id, e.Embedding!, e.NumbersFingerprint))
            .ToList());

    public Task<SemanticCacheEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Entries.FirstOrDefault(e => e.Id == id));

    public void Add(SemanticCacheEntry entry) => Entries.Add(entry);

    public Task<int> DeleteAllAsync(CancellationToken cancellationToken)
    {
        var count = Entries.Count;
        Entries.Clear();

        return Task.FromResult(count);
    }
}

internal sealed class FakeCacheLookupRepository : ICacheLookupRepository
{
    public List<CacheLookup> Lookups { get; } = [];

    public void Add(CacheLookup lookup) => Lookups.Add(lookup);
}

internal sealed class FakeAiBudgetRepository : IAiBudgetRepository
{
    public AiBudget? Budget { get; set; }

    public Task<AiBudget?> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Budget);

    public void Add(AiBudget budget) => Budget = budget;
}

internal sealed class FakeSpendReader : IGatewaySpendReader
{
    public decimal Spent { get; set; }

    public Task<decimal> GetSpentUsdAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        Task.FromResult(Spent);
}

/// <summary>SemanticCacheService yalnızca istatistik için GetCacheTotalsAsync kullanır; diğer sorgular testlerde çağrılmaz.</summary>
internal sealed class StubGatewayQueries : IGatewayQueries
{
    public CacheTotals Totals { get; set; } = new(0, 0, 0, 0, 0m, 0m);

    public Task<CacheTotals> GetCacheTotalsAsync(DateTime nowUtc, CancellationToken cancellationToken) => Task.FromResult(Totals);

    public Task<UsageSummaryDto> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<UsageSeriesDto> GetSeriesAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<UsageBreakdownDto> GetBreakdownAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

/// <summary>Metne göre önceden verilen vektörü döndürür; verilmeyen her yeni metne birbirine dik (benzerlik 0) 16 boyutlu bir vektör atar.</summary>
internal sealed class FakeEmbeddingProvider : IEmbeddingProvider
{
    public const decimal CostPerCall = 0.0000002m;

    public Dictionary<string, float[]> Vectors { get; } = new(StringComparer.Ordinal);

    public bool Fail { get; set; }

    public int Calls { get; private set; }

    private int _next;

    public Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        Calls++;

        if (Fail)
        {
            throw new ExternalServiceException("provider_unavailable", "embeddings down");
        }

        if (!Vectors.TryGetValue(text, out var vector))
        {
            vector = new float[16];
            vector[_next++ % 16] = 1f;
            Vectors[text] = vector;
        }

        return Task.FromResult(new EmbeddingResult(vector, 10, CostPerCall, "text-embedding-3-small"));
    }
}
