using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Services;
using PRI.Application.AiGateway.Validators;
using PRI.Domain.AiGateway.Entities;
using PRI.Shared.Exceptions;

namespace PRI.AiGateway.Tests;

public class RoutingRuleServiceTests
{
    private sealed class Harness
    {
        public Harness()
        {
            Luna = TestData.Luna();
            Terra = TestData.Terra();
            Sol = TestData.Sol();
            Disabled = TestData.Model("gpt-old", PRI.Domain.AiGateway.Enums.ModelTier.Economy, 0.5m, null, 1m, enabled: false);

            Models = new FakeProviderModelRepository([Luna, Terra, Sol, Disabled]);
            Rules = new FakeRoutingRuleRepository([RoutingRule.Create("default", Luna.Id, [Terra.Id], TestData.Now)]);
            UnitOfWork = new FakeUnitOfWork();
            Service = new RoutingRuleService(Rules, Models, UnitOfWork, TimeProvider.System);
        }

        public ProviderModel Luna { get; }

        public ProviderModel Terra { get; }

        public ProviderModel Sol { get; }

        public ProviderModel Disabled { get; }

        public FakeProviderModelRepository Models { get; }

        public FakeRoutingRuleRepository Rules { get; }

        public FakeUnitOfWork UnitOfWork { get; }

        public RoutingRuleService Service { get; }
    }

    [Fact]
    public async Task List_shows_catalog_model_ids_with_the_default_rule_first()
    {
        var h = new Harness();
        await h.Service.SetAsync(new SetRoutingRuleRequest("analysis", "gpt-6.1-sol", ["gpt-6.1-terra"]), CancellationToken.None);

        var list = await h.Service.ListAsync(CancellationToken.None);

        Assert.Equal(new[] { "default", "analysis" }, list.Select(r => r.TaskType).ToArray());
        Assert.Equal("gpt-6-luna", list[0].PrimaryModelId);
        Assert.Equal(new[] { "gpt-6.1-terra" }, list[0].FallbackModelIds.ToArray());
    }

    [Fact]
    public async Task Setting_a_new_task_type_creates_a_rule_and_saves()
    {
        var h = new Harness();

        var created = await h.Service.SetAsync(
            new SetRoutingRuleRequest("  Analysis ", "gpt-6.1-sol", ["gpt-6.1-terra", "gpt-6-luna"]), CancellationToken.None);

        Assert.Equal("analysis", created.TaskType);
        Assert.Equal("gpt-6.1-sol", created.PrimaryModelId);
        Assert.Equal(new[] { "gpt-6.1-terra", "gpt-6-luna" }, created.FallbackModelIds.ToArray());
        Assert.Equal(1, h.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Setting_an_existing_task_type_updates_it_instead_of_adding_a_second_rule()
    {
        var h = new Harness();

        var updated = await h.Service.SetAsync(new SetRoutingRuleRequest("default", "gpt-6.1-terra", null), CancellationToken.None);

        Assert.Equal("gpt-6.1-terra", updated.PrimaryModelId);
        Assert.Empty(updated.FallbackModelIds);
        Assert.Single(await h.Service.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task The_primary_model_is_removed_from_its_own_fallbacks()
    {
        var h = new Harness();

        var rule = await h.Service.SetAsync(
            new SetRoutingRuleRequest("default", "gpt-6-luna", ["gpt-6-luna", "gpt-6.1-terra"]), CancellationToken.None);

        Assert.Equal(new[] { "gpt-6.1-terra" }, rule.FallbackModelIds.ToArray());
    }

    [Theory]
    [InlineData("gpt-does-not-exist")]
    [InlineData("gpt-old")]
    public async Task Unknown_or_disabled_models_are_rejected_for_primary_and_fallbacks(string modelId)
    {
        var h = new Harness();

        await Assert.ThrowsAsync<RequestValidationException>(
            () => h.Service.SetAsync(new SetRoutingRuleRequest("default", modelId, null), CancellationToken.None));
        await Assert.ThrowsAsync<RequestValidationException>(
            () => h.Service.SetAsync(new SetRoutingRuleRequest("default", "gpt-6-luna", [modelId]), CancellationToken.None));

        Assert.Equal(0, h.UnitOfWork.SaveCount);
    }

    [Theory]
    [InlineData("analysis", true)]
    [InlineData("my-task_2", true)]
    [InlineData("Analysis", false)]
    [InlineData("has space", false)]
    [InlineData("", false)]
    public void Task_type_format_is_validated(string taskType, bool valid)
    {
        var result = new SetRoutingRuleRequestValidator().Validate(new SetRoutingRuleRequest(taskType, "gpt-6-luna", null));

        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public void More_than_five_fallbacks_are_rejected()
    {
        var request = new SetRoutingRuleRequest("default", "gpt-6-luna", Enumerable.Repeat("m", 6).ToList());

        Assert.False(new SetRoutingRuleRequestValidator().Validate(request).IsValid);
    }
}
