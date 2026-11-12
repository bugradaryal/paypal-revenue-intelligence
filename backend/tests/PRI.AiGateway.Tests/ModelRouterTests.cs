using PRI.Application.AiGateway.Services;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Enums;
using PRI.Shared.Exceptions;

namespace PRI.AiGateway.Tests;

public class ModelRouterTests
{
    private readonly ProviderModelFixture _fixture = new();

    private sealed class ProviderModelFixture
    {
        public ProviderModel Luna { get; } = TestData.Luna();
        public ProviderModel Terra { get; } = TestData.Terra();
        public ProviderModel Sol { get; } = TestData.Sol();
    }

    private ModelRouter CreateRouter(IEnumerable<ProviderModel>? models = null, IEnumerable<RoutingRule>? rules = null)
    {
        var all = models ?? [_fixture.Luna, _fixture.Terra, _fixture.Sol];

        var defaultRules = rules ??
        [
            RoutingRule.Create("default", _fixture.Luna.Id, [_fixture.Terra.Id], TestData.Now),
            RoutingRule.Create("analysis", _fixture.Terra.Id, [_fixture.Sol.Id], TestData.Now)
        ];

        return new ModelRouter(new FakeProviderModelRepository(all), new FakeRoutingRuleRepository(defaultRules));
    }

    [Fact]
    public async Task A_pinned_model_is_used_alone_without_fallback()
    {
        var plan = await CreateRouter().BuildPlanAsync("gpt-6.1-sol", "analysis", CancellationToken.None);

        Assert.Equal("explicit", plan.Mode);
        Assert.Equal(new[] { "gpt-6.1-sol" }, plan.Models.Select(m => m.ModelId));
        Assert.Null(plan.Rule);
    }

    [Fact]
    public async Task An_unknown_or_disabled_pinned_model_is_a_validation_error()
    {
        var router = CreateRouter(models: [TestData.Luna(enabled: false), _fixture.Terra]);

        await Assert.ThrowsAsync<RequestValidationException>(() => router.BuildPlanAsync("gpt-6-luna", null, CancellationToken.None));
        await Assert.ThrowsAsync<RequestValidationException>(() => router.BuildPlanAsync("nope", null, CancellationToken.None));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("auto")]
    [InlineData("AUTO")]
    public async Task Empty_or_auto_model_uses_the_default_rule(string? model)
    {
        var plan = await CreateRouter().BuildPlanAsync(model, null, CancellationToken.None);

        Assert.Equal("auto", plan.Mode);
        Assert.Equal("default", plan.Rule);
        Assert.Equal(new[] { "gpt-6-luna", "gpt-6.1-terra" }, plan.Models.Select(m => m.ModelId));
    }

    [Fact]
    public async Task Provider_choice_keeps_automatic_model_selection_within_that_provider()
    {
        var gemini = ProviderModel.Create(
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
        var router = CreateRouter(models: [_fixture.Luna, _fixture.Terra, gemini]);

        var plan = await router.BuildPlanAsync("auto", "default", CancellationToken.None, AiProvider.Gemini);

        Assert.Equal("auto", plan.Mode);
        Assert.Single(plan.Models);
        Assert.Equal(AiProvider.Gemini, plan.Models[0].Provider);
        Assert.Equal("gemini-3.5-flash-lite", plan.Models[0].ModelId);
    }

    [Fact]
    public async Task Automatic_rule_adds_a_competitive_model_from_other_enabled_providers()
    {
        var gemini = ProviderModel.Create(
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
        var router = CreateRouter(models: [_fixture.Luna, _fixture.Terra, gemini]);

        var plan = await router.BuildPlanAsync("auto", "classification", CancellationToken.None);

        Assert.Equal(
            new[] { "gpt-6-luna", "gemini-3.5-flash-lite", "gpt-6.1-terra" },
            plan.Models.Select(model => model.ModelId));
        Assert.Equal(AiProvider.Openai, plan.Models[0].Provider);
        Assert.Contains(plan.Models, model => model.Provider == AiProvider.Gemini);
    }

    [Fact]
    public async Task Auto_selects_the_best_value_provider_for_analysis_instead_of_pinning_the_rule_primary()
    {
        var gemini = ProviderModel.Create(
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
        var router = CreateRouter(models: [_fixture.Luna, _fixture.Terra, _fixture.Sol, gemini]);

        var plan = await router.BuildPlanAsync("auto", "analysis", CancellationToken.None);

        Assert.Equal("gemini-3.5-flash-lite", plan.Models[0].ModelId);
        Assert.Equal(AiProvider.Gemini, plan.Models[0].Provider);
        Assert.Contains(plan.Models, model => model.ModelId == "gpt-6.1-terra");
    }

    [Fact]
    public async Task A_matching_task_type_selects_its_own_rule()
    {
        var plan = await CreateRouter().BuildPlanAsync("auto", "Analysis", CancellationToken.None);

        Assert.Equal("analysis", plan.Rule);
        Assert.Equal("analysis", plan.TaskType);
        Assert.Equal(new[] { "gpt-6.1-terra", "gpt-6.1-sol" }, plan.Models.Select(m => m.ModelId));
    }

    [Fact]
    public async Task An_unknown_task_type_falls_back_to_the_default_rule()
    {
        var plan = await CreateRouter().BuildPlanAsync("auto", "poetry", CancellationToken.None);

        Assert.Equal("default", plan.Rule);
        Assert.Equal("poetry", plan.TaskType);
    }

    [Fact]
    public async Task Disabled_models_are_skipped_in_the_plan()
    {
        var router = CreateRouter(models: [TestData.Luna(enabled: false), _fixture.Terra, _fixture.Sol]);

        var plan = await router.BuildPlanAsync("auto", null, CancellationToken.None);

        Assert.Equal(new[] { "gpt-6.1-terra" }, plan.Models.Select(m => m.ModelId));
    }

    [Fact]
    public async Task Without_any_rule_the_router_uses_the_request_profile_instead_of_just_the_cheapest_model()
    {
        var simple = await CreateRouter(rules: []).BuildPlanAsync("auto", "summarization", CancellationToken.None);
        var complex = await CreateRouter(rules: []).BuildPlanAsync("auto", "analysis", CancellationToken.None);

        Assert.Equal(new[] { "gpt-6-luna", "gpt-6.1-terra" }, simple.Models.Select(m => m.ModelId));
        Assert.Equal(new[] { "gpt-6.1-terra", "gpt-6.1-sol" }, complex.Models.Select(m => m.ModelId));
        Assert.Null(simple.Rule);
        Assert.Null(complex.Rule);
    }

    [Fact]
    public async Task Fails_clearly_when_no_model_is_enabled()
    {
        var router = CreateRouter(models: [TestData.Luna(enabled: false)], rules: []);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => router.BuildPlanAsync("auto", null, CancellationToken.None));

        Assert.Equal("no_enabled_models", ex.Code);
    }
}
