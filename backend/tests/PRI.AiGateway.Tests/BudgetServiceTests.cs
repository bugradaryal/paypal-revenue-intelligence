using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Services;
using PRI.Application.Common.DTOs;
using PRI.Domain.AiGateway.Enums;
using PRI.Shared.Exceptions;

namespace PRI.AiGateway.Tests;

public class BudgetServiceTests
{
    private sealed class Harness
    {
        public Harness() =>
            Service = new BudgetService(Budgets, Spend, UnitOfWork, new ManualTimeProvider(TestData.Now));

        public FakeAiBudgetRepository Budgets { get; } = new();

        public FakeSpendReader Spend { get; } = new();

        public FakeUnitOfWork UnitOfWork { get; } = new();

        public BudgetService Service { get; }
    }

    private static SetBudgetRequest Set(string amount, int? warn = null, bool? enforce = null) =>
        new(new MoneyDto(amount, "USD"), warn, enforce);

    [Fact]
    public async Task Without_a_budget_nothing_is_blocked_and_the_status_says_not_configured()
    {
        var h = new Harness { };
        h.Spend.Spent = 999m;

        await h.Service.EnsureWithinBudgetAsync(CancellationToken.None);
        var status = await h.Service.GetAsync(CancellationToken.None);

        Assert.False(status.IsConfigured);
        Assert.Null(status.Limit);
        Assert.Equal("999.000000", status.Spent.Amount);
    }

    [Fact]
    public async Task Setting_a_budget_creates_it_then_updates_the_same_record()
    {
        var h = new Harness();

        var created = await h.Service.SetAsync(Set("50.00"), CancellationToken.None);
        var firstId = h.Budgets.Budget!.Id;
        var updated = await h.Service.SetAsync(Set("75.50", warn: 90, enforce: false), CancellationToken.None);

        Assert.True(created.IsConfigured);
        Assert.Equal(80, created.WarnAtPercent);      // varsayılan
        Assert.True(created.EnforceLimit);            // varsayılan
        Assert.Equal(firstId, h.Budgets.Budget.Id);
        Assert.Equal("75.500000", updated.Limit!.Amount);
        Assert.Equal(90, updated.WarnAtPercent);
        Assert.False(updated.EnforceLimit);
        Assert.Equal(2, h.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Status_reports_remaining_percent_and_level()
    {
        var h = new Harness();
        await h.Service.SetAsync(Set("10.00"), CancellationToken.None);
        h.Spend.Spent = 8.5m;

        var status = await h.Service.GetAsync(CancellationToken.None);

        Assert.Equal("1.500000", status.Remaining!.Amount);
        Assert.Equal(85d, status.UsedPercent);
        Assert.Equal(BudgetLevel.Warning, status.Level);
        Assert.Equal(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), status.PeriodStart);
        Assert.Equal(new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), status.PeriodEnd);
    }

    [Fact]
    public async Task An_exceeded_enforced_budget_throws_budget_exceeded()
    {
        var h = new Harness();
        await h.Service.SetAsync(Set("10.00"), CancellationToken.None);
        h.Spend.Spent = 10m;

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => h.Service.EnsureWithinBudgetAsync(CancellationToken.None));

        Assert.Equal("budget_exceeded", ex.Code);
    }

    [Fact]
    public async Task An_exceeded_budget_without_enforcement_only_changes_the_level()
    {
        var h = new Harness();
        await h.Service.SetAsync(Set("10.00", enforce: false), CancellationToken.None);
        h.Spend.Spent = 12m;

        await h.Service.EnsureWithinBudgetAsync(CancellationToken.None);
        var status = await h.Service.GetAsync(CancellationToken.None);

        Assert.Equal(BudgetLevel.Exceeded, status.Level);
        Assert.Equal("0.000000", status.Remaining!.Amount);
    }
}
