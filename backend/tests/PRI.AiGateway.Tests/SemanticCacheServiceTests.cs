using Microsoft.Extensions.Logging.Abstractions;
using PRI.Application.AiGateway;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.AiGateway.Services;
using PRI.Domain.AiGateway.Enums;

namespace PRI.AiGateway.Tests;

public class SemanticCacheServiceTests
{
    private sealed class Harness
    {
        public Harness(SemanticCacheSettings? settings = null)
        {
            Service = new SemanticCacheService(
                Entries, Lookups, Embeddings, Queries, settings ?? new SemanticCacheSettings(), Time,
                NullLogger<SemanticCacheService>.Instance);
        }

        public Guid ClientId { get; } = Guid.NewGuid();

        public FakeSemanticCacheRepository Entries { get; } = new();

        public FakeCacheLookupRepository Lookups { get; } = new();

        public FakeEmbeddingProvider Embeddings { get; } = new();

        public StubGatewayQueries Queries { get; } = new();

        public ManualTimeProvider Time { get; } = new(TestData.Now);

        public SemanticCacheService Service { get; }

        public CacheLookupInput Input(string query, Guid? clientId = null, string system = "Sen bir asistansın.") =>
            new(clientId ?? ClientId, "auto", null, null, null, null,
                [new ProviderMessage("system", system), new ProviderMessage("user", query)]);

        /// <summary>Bir yanıtı gerçek akışla (miss → store) cache'e yazar.</summary>
        public async Task SeedAsync(string query, float[] vector, Guid? clientId = null, string system = "Sen bir asistansın.")
        {
            Embeddings.Vectors[query] = vector;
            var lookup = await Service.LookupAsync(Input(query, clientId, system), CancellationToken.None);
            Service.StageStore(
                lookup!, TestData.Luna(), new ProviderCompletionResult("Cevap", 100, 0, 50, "id", "stop"), 0.0004m);
        }
    }

    [Fact]
    public async Task Identical_query_is_an_exact_hit_and_calls_no_embedding()
    {
        var h = new Harness();
        await h.SeedAsync("İade süresi kaç gün?", [1f, 0f, 0f]);
        var callsAfterSeed = h.Embeddings.Calls;

        var lookup = await h.Service.LookupAsync(h.Input("  iade SÜRESİ kaç   gün? "), CancellationToken.None);

        Assert.Equal(CacheOutcome.ExactHit, lookup!.Hit!.Outcome);
        Assert.Equal(callsAfterSeed, h.Embeddings.Calls);   // tam eşleşme ücretsiz
        Assert.Equal(0m, lookup.EmbeddingCostUsd);
    }

    [Fact]
    public async Task A_similar_query_above_the_threshold_is_a_semantic_hit()
    {
        var h = new Harness();
        await h.SeedAsync("İade süresi kaç gün?", [1f, 0f, 0f]);
        h.Embeddings.Vectors["İade için kaç günüm var?"] = [0.99f, 0.14f, 0f];   // kosinüs ≈ 0.99

        var lookup = await h.Service.LookupAsync(h.Input("İade için kaç günüm var?"), CancellationToken.None);

        Assert.Equal(CacheOutcome.SemanticHit, lookup!.Hit!.Outcome);
        Assert.InRange(lookup.Hit.Similarity, 0.98, 1.0);
        Assert.Equal(FakeEmbeddingProvider.CostPerCall, lookup.EmbeddingCostUsd);
    }

    [Fact]
    public async Task A_query_below_the_threshold_is_a_miss()
    {
        var h = new Harness();
        await h.SeedAsync("İade süresi kaç gün?", [1f, 0f, 0f]);
        h.Embeddings.Vectors["Kargo ücreti ne kadar?"] = [0.6f, 0.8f, 0f];   // kosinüs 0.6

        var lookup = await h.Service.LookupAsync(h.Input("Kargo ücreti ne kadar?"), CancellationToken.None);

        Assert.Null(lookup!.Hit);
    }

    [Fact]
    public async Task Different_numbers_never_share_an_answer_even_if_embeddings_are_identical()
    {
        var h = new Harness();
        await h.SeedAsync("Sipariş 123 nerede?", [1f, 0f, 0f]);
        h.Embeddings.Vectors["Sipariş 456 nerede?"] = [1f, 0f, 0f];

        var lookup = await h.Service.LookupAsync(h.Input("Sipariş 456 nerede?"), CancellationToken.None);

        Assert.Null(lookup!.Hit);
    }

    [Fact]
    public async Task Entries_are_never_shared_between_clients()
    {
        var h = new Harness();
        await h.SeedAsync("İade süresi kaç gün?", [1f, 0f, 0f]);

        var lookup = await h.Service.LookupAsync(h.Input("İade süresi kaç gün?", clientId: Guid.NewGuid()), CancellationToken.None);

        Assert.Null(lookup!.Hit);
    }

    [Fact]
    public async Task A_different_system_prompt_is_a_different_context()
    {
        var h = new Harness();
        await h.SeedAsync("İade süresi kaç gün?", [1f, 0f, 0f]);

        var lookup = await h.Service.LookupAsync(
            h.Input("İade süresi kaç gün?", system: "Sen bir hukuk asistanısın."), CancellationToken.None);

        Assert.Null(lookup!.Hit);
    }

    [Fact]
    public async Task Expired_entries_are_not_served()
    {
        var h = new Harness(new SemanticCacheSettings { TtlHours = 1 });
        await h.SeedAsync("İade süresi kaç gün?", [1f, 0f, 0f]);

        h.Time.Advance(TimeSpan.FromHours(2));
        var lookup = await h.Service.LookupAsync(h.Input("İade süresi kaç gün?"), CancellationToken.None);

        Assert.Null(lookup!.Hit);
    }

    [Fact]
    public async Task When_embedding_fails_the_lookup_degrades_to_exact_only_without_throwing()
    {
        var h = new Harness();
        await h.SeedAsync("İade süresi kaç gün?", [1f, 0f, 0f]);
        h.Embeddings.Fail = true;

        var exact = await h.Service.LookupAsync(h.Input("İade süresi kaç gün?"), CancellationToken.None);
        var other = await h.Service.LookupAsync(h.Input("Başka bir soru"), CancellationToken.None);

        Assert.Equal(CacheOutcome.ExactHit, exact!.Hit!.Outcome);
        Assert.NotNull(other);
        Assert.Null(other.Hit);
        Assert.Null(other.Embedding);
    }

    [Fact]
    public async Task Cache_is_skipped_when_disabled_or_the_last_message_is_not_from_the_user()
    {
        var disabled = new Harness(new SemanticCacheSettings { Enabled = false });
        Assert.Null(await disabled.Service.LookupAsync(disabled.Input("x"), CancellationToken.None));

        var h = new Harness();
        var assistantLast = new CacheLookupInput(
            h.ClientId, "auto", null, null, null, null, [new ProviderMessage("assistant", "Merhaba")]);
        Assert.Null(await h.Service.LookupAsync(assistantLast, CancellationToken.None));
    }

    [Fact]
    public async Task Very_long_queries_use_exact_matching_only()
    {
        var h = new Harness(new SemanticCacheSettings { MaxQueryCharacters = 10 });

        var lookup = await h.Service.LookupAsync(h.Input(new string('a', 50)), CancellationToken.None);

        Assert.Equal(0, h.Embeddings.Calls);
        Assert.Null(lookup!.Hit);
    }

    [Theory]
    [InlineData("length")]
    [InlineData("content_filter")]
    public async Task Truncated_or_filtered_responses_are_not_stored(string finishReason)
    {
        var h = new Harness();
        var lookup = await h.Service.LookupAsync(h.Input("Uzun bir soru"), CancellationToken.None);

        h.Service.StageStore(lookup!, TestData.Luna(), new ProviderCompletionResult("yarım...", 10, 0, 10, "id", finishReason), 0.1m);

        Assert.Empty(h.Entries.Entries);
    }

    [Fact]
    public async Task Staging_a_hit_counts_it_and_records_the_saved_cost()
    {
        var h = new Harness();
        await h.SeedAsync("İade süresi kaç gün?", [1f, 0f, 0f]);
        var lookup = await h.Service.LookupAsync(h.Input("İade süresi kaç gün?"), CancellationToken.None);

        var record = h.Service.StageHit(lookup!);

        Assert.Equal(1, lookup!.Hit!.Entry.HitCount);
        Assert.Equal(0.0004m, record.SavedCostUsd);
        Assert.Equal(CacheOutcome.ExactHit, record.Outcome);
        Assert.Same(record, Assert.Single(h.Lookups.Lookups));
    }

    [Fact]
    public async Task Stats_combine_totals_and_settings()
    {
        var h = new Harness();
        h.Queries.Totals = new PRI.Application.AiGateway.Interfaces.CacheTotals(10, 8, 3, 1, 0.0123m, 0.000001m);

        var stats = await h.Service.GetStatsAsync(CancellationToken.None);

        Assert.Equal(0.75, stats.HitRate);
        Assert.Equal("0.012300", stats.SavedCost.Amount);
        Assert.Equal(8, stats.ActiveEntries);
        Assert.True(stats.Enabled);
    }
}
