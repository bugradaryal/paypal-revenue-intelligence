using Microsoft.Extensions.Logging.Abstractions;
using PRI.Application.AiGateway;
using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.AiGateway.Services;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Enums;
using PRI.Shared.Exceptions;

namespace PRI.AiGateway.Tests;

public class AiCompletionServiceTests
{
    private static readonly CompletionRequest SimpleRequest = new(
        "auto", null, [new CompletionMessageDto("user", "Merhaba")], null, null, null);

    private sealed class Harness
    {
        public Harness(
            ScriptedProvider provider,
            IEnumerable<ProviderModel>? additionalModels = null,
            IEnumerable<IAiProvider>? additionalProviders = null,
            IEnumerable<RoutingRule>? routingRules = null)
        {
            Provider = provider;

            var luna = TestData.Luna();
            var terra = TestData.Terra();
            var sol = TestData.Sol();
            var models = new FakeProviderModelRepository(new[] { luna, terra, sol }.Concat(additionalModels ?? []));
            var rules = new FakeRoutingRuleRepository(routingRules ??
            [
                RoutingRule.Create("default", luna.Id, [terra.Id], TestData.Now)
            ]);
            var providers = new List<IAiProvider> { provider };
            providers.AddRange(additionalProviders ?? []);

            Clients.Clients.Add(GatewayClient.CreateFirstParty("PRI Dashboard", TestData.Now));

            // Cache kapalı: bu sınıftaki testler yönlendirme/yedekleme/maliyet davranışını sınar (cache için CompletionCacheBudgetTests).
            var cache = new SemanticCacheService(
                new FakeSemanticCacheRepository(), new FakeCacheLookupRepository(), new FakeEmbeddingProvider(),
                new StubGatewayQueries(), new SemanticCacheSettings { Enabled = false }, TimeProvider.System,
                NullLogger<SemanticCacheService>.Instance);
            var budget = new BudgetService(new FakeAiBudgetRepository(), new FakeSpendReader(), UnitOfWork, TimeProvider.System);

            Service = new AiCompletionService(
                new ModelRouter(models, rules), models, Clients, Usage,
                new AiProviderResolver(providers), new PromptOptimizer(), cache, budget,
                UnitOfWork, TimeProvider.System, NullLogger<AiCompletionService>.Instance);
        }

        public ScriptedProvider Provider { get; }

        public FakeGatewayClientRepository Clients { get; } = new();

        public FakeUsageRecordRepository Usage { get; } = new();

        public FakeUnitOfWork UnitOfWork { get; } = new();

        public AiCompletionService Service { get; }
    }

    private static ProviderCompletionResult Ok(int input = 1000, int cached = 0, int output = 500) =>
        new("Merhaba!", input, cached, output, "chatcmpl-1", "stop");

    [Fact]
    public async Task Auto_routed_request_uses_the_primary_model_and_records_cost_and_baseline()
    {
        var harness = new Harness(new ScriptedProvider(_ => Ok()));

        var response = await harness.Service.CompleteAsync(SimpleRequest, null, CancellationToken.None);

        Assert.Equal("gpt-6-luna", response.Model);
        Assert.Equal("auto", response.Routing.Mode);
        Assert.False(response.Routing.FallbackUsed);
        Assert.Equal("0.000350", response.Cost.Amount);   // (1000 × 0.10 + 500 × 0.50) / 1M

        var record = Assert.Single(harness.Usage.Records);
        Assert.Equal(UsageStatus.Success, record.Status);
        Assert.True(record.IsAutoRouted);
        Assert.Equal("gpt-6.1-terra", record.BaselineModelId);   // baz model: aynı token'lar Terra'da
        Assert.Equal(0.0035m, record.BaselineCostUsd);            // (1000 × 1 + 500 × 5) / 1M
    }

    [Fact]
    public async Task A_pinned_model_claims_no_savings()
    {
        var harness = new Harness(new ScriptedProvider(_ => Ok()));
        var request = SimpleRequest with { Model = "gpt-6.1-sol" };

        var response = await harness.Service.CompleteAsync(request, null, CancellationToken.None);

        Assert.Equal("explicit", response.Routing.Mode);
        var record = Assert.Single(harness.Usage.Records);
        Assert.False(record.IsAutoRouted);
        Assert.Null(record.BaselineCostUsd);
    }

    [Fact]
    public async Task Transient_provider_failure_falls_back_to_the_next_model_and_records_both_attempts()
    {
        var harness = new Harness(new ScriptedProvider(request =>
            request.ModelId == "gpt-6-luna"
                ? throw new ExternalServiceException("provider_unavailable", "down")
                : Ok()));

        var response = await harness.Service.CompleteAsync(SimpleRequest, null, CancellationToken.None);

        Assert.Equal("gpt-6.1-terra", response.Model);
        Assert.True(response.Routing.FallbackUsed);
        Assert.Equal(new[] { "gpt-6-luna", "gpt-6.1-terra" }, response.Routing.AttemptedModels);

        Assert.Equal(2, harness.Usage.Records.Count);
        var failed = harness.Usage.Records.Single(r => r.Status == UsageStatus.Failed);
        Assert.Equal("gpt-6-luna", failed.ModelId);
        Assert.Equal("provider_unavailable", failed.ErrorCode);
        Assert.Equal(0m, failed.CostUsd);
        Assert.Equal(2, harness.Usage.Records.Single(r => r.Status == UsageStatus.Success).Attempt);
    }

    [Fact]
    public async Task Auto_uses_Gemini_for_analysis_when_it_has_the_best_task_fit_and_price()
    {
        var geminiModel = ProviderModel.Create(
            AiProvider.Gemini,
            "gemini-3.5-flash-lite",
            "Gemini 3.5 Flash Lite",
            ModelTier.Standard,
            0.75m,
            null,
            3.75m,
            "https://ai.google.dev/gemini-api/docs/pricing",
            new DateOnly(2026, 11, 7),
            TestData.Now);
        var openAi = new ScriptedProvider(_ => Ok());
        var gemini = new ScriptedProvider(_ => Ok(), AiProvider.Gemini);
        var harness = new Harness(openAi, [geminiModel], [gemini]);
        var request = SimpleRequest with
        {
            Messages = [new CompletionMessageDto("user", "Bu ödeme sorununu analiz et ve nedenlerini karşılaştır.")]
        };

        var response = await harness.Service.CompleteAsync(request, null, CancellationToken.None);

        Assert.Equal(AiProvider.Gemini, response.Provider);
        Assert.Equal("gemini-3.5-flash-lite", response.Model);
        Assert.Empty(openAi.CalledModels);
        Assert.Equal("gemini-3.5-flash-lite", Assert.Single(gemini.CalledModels));
    }

    [Fact]
    public async Task Auto_falls_back_when_the_best_fit_provider_is_not_configured()
    {
        var geminiModel = ProviderModel.Create(
            AiProvider.Gemini,
            "gemini-3.5-flash-lite",
            "Gemini 3.5 Flash Lite",
            ModelTier.Standard,
            0.75m,
            null,
            3.75m,
            "https://ai.google.dev/gemini-api/docs/pricing",
            new DateOnly(2026, 11, 7),
            TestData.Now);
        var openAi = new ScriptedProvider(_ => Ok());
        var gemini = new ScriptedProvider(
            _ => throw new BusinessRuleException("gemini_not_configured", "key missing"),
            AiProvider.Gemini);
        var harness = new Harness(openAi, [geminiModel], [gemini]);
        var request = SimpleRequest with
        {
            Messages = [new CompletionMessageDto("user", "Bu ödeme sorununu analiz et ve nedenlerini karşılaştır.")]
        };

        var response = await harness.Service.CompleteAsync(request, null, CancellationToken.None);

        Assert.Equal(AiProvider.Openai, response.Provider);
        Assert.Equal("gpt-6.1-terra", response.Model);
        Assert.True(response.Routing.FallbackUsed);
        Assert.Equal("gemini-3.5-flash-lite", Assert.Single(gemini.CalledModels));
        Assert.Equal("gpt-6.1-terra", Assert.Single(openAi.CalledModels));
    }

    [Fact]
    public async Task A_rejected_request_does_not_trigger_a_fallback()
    {
        var harness = new Harness(new ScriptedProvider(_ =>
            throw new BusinessRuleException("provider_rejected_request", "bad parameter")));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => harness.Service.CompleteAsync(SimpleRequest, null, CancellationToken.None));

        Assert.Equal("provider_rejected_request", ex.Code);
        Assert.Equal(new[] { "gpt-6-luna" }, harness.Provider.CalledModels);
        Assert.Single(harness.Usage.Records);
    }

    [Fact]
    public async Task When_every_model_fails_the_last_error_is_returned()
    {
        var harness = new Harness(new ScriptedProvider(_ =>
            throw new ExternalServiceException("provider_rate_limited", "slow down")));

        var ex = await Assert.ThrowsAsync<ExternalServiceException>(
            () => harness.Service.CompleteAsync(SimpleRequest, null, CancellationToken.None));

        Assert.Equal("provider_rate_limited", ex.Code);
        Assert.Equal(2, harness.Provider.CalledModels.Count);
        Assert.All(harness.Usage.Records, r => Assert.Equal(UsageStatus.Failed, r.Status));
    }

    [Fact]
    public async Task Requests_with_an_api_key_are_attributed_to_that_client()
    {
        var harness = new Harness(new ScriptedProvider(_ => Ok()));
        var clientId = Guid.NewGuid();

        await harness.Service.CompleteAsync(SimpleRequest, clientId, CancellationToken.None);

        Assert.Equal(clientId, Assert.Single(harness.Usage.Records).ClientId);
    }
}
