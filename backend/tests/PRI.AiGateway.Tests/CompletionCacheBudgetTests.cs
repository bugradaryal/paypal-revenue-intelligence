using Microsoft.Extensions.Logging.Abstractions;
using PRI.Application.AiGateway;
using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.AiGateway.Services;
using PRI.Application.Common.DTOs;
using PRI.Domain.AiGateway.Entities;
using PRI.Shared.Exceptions;

namespace PRI.AiGateway.Tests;

/// <summary>AiCompletionService'in cache, bütçe ve prompt optimizasyonu ile uçtan uca davranışı (sahte sağlayıcı, gerçek servisler).</summary>
public class CompletionCacheBudgetTests
{
    private sealed class Harness
    {
        public Harness(Func<ProviderCompletionRequest, ProviderCompletionResult>? behavior = null)
        {
            Provider = new ScriptedProvider(behavior ?? (_ => new ProviderCompletionResult("Cevap", 1000, 0, 500, "chatcmpl-1", "stop")));

            var luna = TestData.Luna();
            var terra = TestData.Terra();
            var models = new FakeProviderModelRepository([luna, terra]);
            var rules = new FakeRoutingRuleRepository([RoutingRule.Create("default", luna.Id, [terra.Id], TestData.Now)]);

            Clients.Clients.Add(GatewayClient.CreateFirstParty("PRI Dashboard", TestData.Now));

            var cache = new SemanticCacheService(
                CacheEntries, CacheLookups, Embeddings, new StubGatewayQueries(), new SemanticCacheSettings(), Time,
                NullLogger<SemanticCacheService>.Instance);
            var budget = new BudgetService(Budgets, Spend, UnitOfWork, Time);

            Service = new AiCompletionService(
                new ModelRouter(models, rules), models, Clients, Usage, new AiProviderResolver([Provider]),
                new PromptOptimizer(), cache, budget, UnitOfWork, Time, NullLogger<AiCompletionService>.Instance);
            BudgetService = budget;
        }

        public ScriptedProvider Provider { get; }

        public FakeGatewayClientRepository Clients { get; } = new();

        public FakeUsageRecordRepository Usage { get; } = new();

        public FakeUnitOfWork UnitOfWork { get; } = new();

        public FakeSemanticCacheRepository CacheEntries { get; } = new();

        public FakeCacheLookupRepository CacheLookups { get; } = new();

        public FakeEmbeddingProvider Embeddings { get; } = new();

        public FakeAiBudgetRepository Budgets { get; } = new();

        public FakeSpendReader Spend { get; } = new();

        public ManualTimeProvider Time { get; } = new(TestData.Now);

        public AiCompletionService Service { get; }

        public BudgetService BudgetService { get; }
    }

    private static CompletionRequest Ask(string text, bool? cache = null, bool? optimize = null) =>
        new("auto", null, [new CompletionMessageDto("user", text)], null, null, null, cache, optimize);

    [Fact]
    public async Task Second_identical_request_is_served_from_cache_without_calling_the_provider()
    {
        var h = new Harness();

        var first = await h.Service.CompleteAsync(Ask("İade süresi kaç gün?"), null, CancellationToken.None);
        var second = await h.Service.CompleteAsync(Ask("İade süresi kaç gün?"), null, CancellationToken.None);

        Assert.Equal("miss", first.Cache.Status);
        Assert.Equal("hit", second.Cache.Status);
        Assert.Equal("exact", second.Cache.MatchType);
        Assert.Equal("Cevap", second.Content);
        Assert.Equal("0.000000", second.Cost.Amount);
        Assert.Equal(new CompletionUsageDto(0, 0, 0), second.Usage);
        Assert.Equal(first.Cost.Amount, second.Cache.SavedCost!.Amount);   // kaçınılan = ilk üretim maliyeti
        Assert.Single(h.Provider.CalledModels);                              // sağlayıcı yalnızca bir kez çağrıldı
        Assert.Single(h.Usage.Records);                                      // hit için kullanım kaydı yazılmaz
        Assert.Equal(2, h.CacheLookups.Lookups.Count);
        Assert.Equal(1, h.CacheEntries.Entries.Single().HitCount);
    }

    [Fact]
    public async Task Cache_false_bypasses_lookup_and_storage()
    {
        var h = new Harness();

        var first = await h.Service.CompleteAsync(Ask("Merhaba", cache: false), null, CancellationToken.None);
        var second = await h.Service.CompleteAsync(Ask("Merhaba", cache: false), null, CancellationToken.None);

        Assert.Equal("bypass", first.Cache.Status);
        Assert.Equal("bypass", second.Cache.Status);
        Assert.Equal(2, h.Provider.CalledModels.Count);
        Assert.Empty(h.CacheEntries.Entries);
        Assert.Empty(h.CacheLookups.Lookups);
    }

    [Fact]
    public async Task A_miss_records_the_embedding_cost_and_stores_the_entry()
    {
        var h = new Harness();

        var response = await h.Service.CompleteAsync(Ask("Yeni soru"), null, CancellationToken.None);

        Assert.Equal("miss", response.Cache.Status);
        var lookup = Assert.Single(h.CacheLookups.Lookups);
        Assert.False(lookup.IsHit);
        Assert.Equal(FakeEmbeddingProvider.CostPerCall, lookup.EmbeddingCostUsd);
        Assert.Single(h.CacheEntries.Entries);
    }

    [Fact]
    public async Task A_truncated_answer_is_returned_but_not_cached()
    {
        var h = new Harness(_ => new ProviderCompletionResult("yarım", 100, 0, 50, "id", "length"));

        await h.Service.CompleteAsync(Ask("Uzun soru"), null, CancellationToken.None);
        await h.Service.CompleteAsync(Ask("Uzun soru"), null, CancellationToken.None);

        Assert.Equal(2, h.Provider.CalledModels.Count);
        Assert.Empty(h.CacheEntries.Entries);
    }

    [Fact]
    public async Task An_exceeded_budget_blocks_provider_calls_and_keeps_the_miss_record()
    {
        var h = new Harness();
        await h.BudgetService.SetAsync(new SetBudgetRequest(new MoneyDto("10.00", "USD"), null, null), CancellationToken.None);
        h.Spend.Spent = 10m;

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => h.Service.CompleteAsync(Ask("Pahalı soru"), null, CancellationToken.None));

        Assert.Equal("budget_exceeded", ex.Code);
        Assert.Empty(h.Provider.CalledModels);
        Assert.Empty(h.Usage.Records);
        Assert.Single(h.CacheLookups.Lookups);   // ödenen embedding maliyeti kaybolmaz
    }

    [Fact]
    public async Task Cached_answers_are_still_served_when_the_budget_is_exceeded()
    {
        var h = new Harness();
        await h.Service.CompleteAsync(Ask("İade süresi kaç gün?"), null, CancellationToken.None);

        await h.BudgetService.SetAsync(new SetBudgetRequest(new MoneyDto("1.00", "USD"), null, null), CancellationToken.None);
        h.Spend.Spent = 5m;

        var cached = await h.Service.CompleteAsync(Ask("İade süresi kaç gün?"), null, CancellationToken.None);
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => h.Service.CompleteAsync(Ask("Hiç sorulmamış bir soru"), null, CancellationToken.None));

        Assert.Equal("hit", cached.Cache.Status);
    }

    [Fact]
    public async Task Prompt_optimization_is_applied_by_default_recorded_and_can_be_turned_off()
    {
        var sent = new List<string>();
        var h = new Harness(request =>
        {
            sent.Add(request.Messages[^1].Content);
            return new ProviderCompletionResult("Cevap", 100, 0, 50, "id", "stop");
        });

        var optimized = await h.Service.CompleteAsync(Ask("Merhaba   \n\n\n\n dünya  ", cache: false), null, CancellationToken.None);
        var untouched = await h.Service.CompleteAsync(Ask("Merhaba   \n\n\n\n dünya  ", cache: false, optimize: false), null, CancellationToken.None);

        Assert.Equal("Merhaba\n\n dünya", sent[0]);
        Assert.True(optimized.PromptOptimization.Applied);
        Assert.True(optimized.PromptOptimization.CharactersSaved > 0);
        Assert.Equal(optimized.PromptOptimization.CharactersSaved, h.Usage.Records[0].PromptCharsSaved);

        Assert.Equal("Merhaba   \n\n\n\n dünya  ", sent[1]);
        Assert.False(untouched.PromptOptimization.Applied);
        Assert.Equal(0, h.Usage.Records[1].PromptCharsSaved);
    }

    [Fact]
    public async Task Whitespace_variants_of_a_prompt_hit_the_same_cache_entry()
    {
        var h = new Harness();

        await h.Service.CompleteAsync(Ask("Merhaba dünya"), null, CancellationToken.None);
        var second = await h.Service.CompleteAsync(Ask("Merhaba dünya   \n\n\n"), null, CancellationToken.None);

        Assert.Equal("hit", second.Cache.Status);
    }
}
