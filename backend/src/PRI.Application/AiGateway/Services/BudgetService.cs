using System.Globalization;
using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.Common.DTOs;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Domain.Common.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Application.AiGateway.Services;

public sealed class BudgetService(
    IAiBudgetRepository budgets,
    IGatewaySpendReader spendReader,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IBudgetService
{
    private const string Usd = "USD";
    private const string Monthly = "monthly";

    public async Task<BudgetDto> GetAsync(CancellationToken cancellationToken)
    {
        var budget = await budgets.GetAsync(cancellationToken);

        return await BuildAsync(budget, cancellationToken);
    }

    public async Task<BudgetDto> SetAsync(SetBudgetRequest request, CancellationToken cancellationToken)
    {
        var limit = decimal.Parse(request.Limit.Amount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        var warnAt = request.WarnAtPercent ?? AiBudget.DefaultWarnAtPercent;
        var enforce = request.EnforceLimit ?? true;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var budget = await budgets.GetAsync(cancellationToken);
        if (budget is null)
        {
            budget = AiBudget.Create(limit, warnAt, enforce, now);
            budgets.Add(budget);
        }
        else
        {
            budget.Update(limit, warnAt, enforce, now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildAsync(budget, cancellationToken);
    }

    public async Task EnsureWithinBudgetAsync(CancellationToken cancellationToken)
    {
        var budget = await budgets.GetAsync(cancellationToken);
        if (budget is null || !budget.EnforceLimit)
        {
            return;
        }

        var (start, end) = AiBudget.PeriodContaining(timeProvider.GetUtcNow().UtcDateTime);
        var spent = await spendReader.GetSpentUsdAsync(start, end, cancellationToken);

        if (!budget.CanSpend(spent))
        {
            throw new BusinessRuleException(
                "budget_exceeded",
                $"The monthly AI budget of {Format(budget.MonthlyLimitUsd)} USD has been reached ({Format(spent)} USD spent this month). " +
                "Raise it with PUT /api/v1/gateway/budgets. Cached responses are still served.");
        }
    }

    private async Task<BudgetDto> BuildAsync(AiBudget? budget, CancellationToken cancellationToken)
    {
        var (start, end) = AiBudget.PeriodContaining(timeProvider.GetUtcNow().UtcDateTime);
        var spent = await spendReader.GetSpentUsdAsync(start, end, cancellationToken);

        if (budget is null)
        {
            return new BudgetDto(
                false, Monthly, start, end, null, MoneyDto.FromPrecise(spent, Usd), null, null, null, null, null, null);
        }

        return new BudgetDto(
            true,
            Monthly,
            start,
            end,
            MoneyDto.FromPrecise(budget.MonthlyLimitUsd, Usd),
            MoneyDto.FromPrecise(spent, Usd),
            MoneyDto.FromPrecise(Math.Max(budget.MonthlyLimitUsd - spent, 0m), Usd),
            Math.Round((double)(spent / budget.MonthlyLimitUsd * 100m), 2),
            budget.WarnAtPercent,
            budget.EnforceLimit,
            budget.Evaluate(spent),
            budget.UpdatedAtUtc);
    }

    private static string Format(decimal value) => value.ToString("0.000000", CultureInfo.InvariantCulture);
}
