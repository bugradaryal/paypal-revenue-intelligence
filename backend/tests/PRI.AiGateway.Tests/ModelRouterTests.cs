using PRI.Application.AiGateway.Services;
using PRI.Domain.AiGateway.Entities;
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
    public async Task Without_any_rule_the_cheapest_enabled_model_is_used()
    {
        var plan = await CreateRouter(rules: []).BuildPlanAsync("auto", null, CancellationToken.None);

        Assert.Equal(new[] { "gpt-6-luna" }, plan.Models.Select(m => m.ModelId));
        Assert.Null(plan.Rule);
    }

    [Fact]
    public async Task Fails_clearly_when_no_model_is_enabled()
    {
        var router = CreateRouter(models: [TestData.Luna(enabled: false)], rules: []);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => router.BuildPlanAsync("auto", null, CancellationToken.None));

        Assert.Equal("no_enabled_models", ex.Code);
    }
}
