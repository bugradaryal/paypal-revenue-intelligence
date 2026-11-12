using PRI.Application.Common.DTOs;
using PRI.Application.Revenue.DTOs;
using PRI.Domain.Revenue.Enums;

namespace PRI.Application.Revenue.Services;

public static class RevenueAnalyticsCalculator
{
    private const string SummaryCurrency = "USD";
    private const int AnomalyWindowDays = 14;
    private const int MinimumPaymentsPerFailureWindow = 10;
    private const int MinimumBaselineRevenuePayments = 5;
    private const double FailureRateIncreaseThreshold = 0.10;
    private const double RevenueDropThreshold = 0.40;

    public static RevenueAnalyticsDto Calculate(
        IReadOnlyCollection<RevenueAnalyticsPaymentFact> payments,
        IReadOnlyCollection<RevenueAnalyticsFirstPayment> firstPayments,
        CashFlowOutlookDto cashFlow,
        DateTime fromUtc,
        DateTime toUtc,
        DateOnly todayUtc)
    {
        var inRange = payments
            .Where(payment => payment.InitiatedAtUtc >= fromUtc && payment.InitiatedAtUtc < toUtc)
            .ToList();
        var usdPayments = inRange.Where(payment => IsCurrency(payment.Currency, SummaryCurrency)).ToList();

        var completed = usdPayments.Where(payment => payment.Status == PaymentStatus.Completed).ToList();
        var failed = usdPayments.Where(payment => payment.Status == PaymentStatus.Failed).ToList();
        var pending = usdPayments.Where(payment => payment.Status == PaymentStatus.Pending).ToList();
        var refunded = usdPayments.Where(payment => payment.Status == PaymentStatus.Refunded).ToList();

        var daily = BuildDaily(inRange, fromUtc, toUtc);
        var failureReasons = inRange
            .Where(payment => payment.Status == PaymentStatus.Failed)
            .GroupBy(payment => payment.FailureCode ?? PaymentFailureCode.Other)
            .Select(group => new FailureReasonAnalysisDto(
                ToSnakeCase(group.Key),
                group.Count(),
                Usd(group.Where(payment => IsCurrency(payment.Currency, SummaryCurrency)).Sum(payment => payment.Amount))))
            .OrderByDescending(reason => reason.Count)
            .ThenBy(reason => reason.Code, StringComparer.Ordinal)
            .ToList();

        var failureTrend = BuildFailureTrend(inRange, fromUtc, toUtc);
        var cohorts = BuildCohorts(payments, firstPayments, todayUtc);
        var anomalies = BuildAnomalies(payments, todayUtc);

        return new RevenueAnalyticsDto(
            new DateRangeDto(fromUtc, toUtc),
            todayUtc,
            new RevenueLeakageDto(
                Usd(completed.Sum(payment => payment.Amount)
                    + failed.Sum(payment => payment.Amount)
                    + pending.Sum(payment => payment.Amount)
                    + refunded.Sum(payment => payment.Amount)),
                Usd(completed.Sum(payment => payment.Amount)),
                completed.Count,
                Usd(failed.Sum(payment => payment.Amount)),
                failed.Count,
                Usd(pending.Sum(payment => payment.Amount)),
                pending.Count,
                Usd(refunded.Sum(payment => payment.Amount)),
                refunded.Count,
                cashFlow.OpenReceivables,
                cashFlow.OpenInvoiceCount,
                cashFlow.OverdueReceivables,
                cashFlow.OverdueInvoiceCount,
                CancellationTrackingAvailable: false),
            daily,
            failureReasons,
            failureTrend,
            cohorts,
            anomalies,
            inRange.Count(payment => !IsCurrency(payment.Currency, SummaryCurrency)));
    }

    private static IReadOnlyList<RevenueAnalyticsDayDto> BuildDaily(
        IReadOnlyCollection<RevenueAnalyticsPaymentFact> payments,
        DateTime fromUtc,
        DateTime toUtc)
    {
        var dailyPayments = payments
            .Where(payment => IsCurrency(payment.Currency, SummaryCurrency))
            .GroupBy(payment => DateOnly.FromDateTime(payment.InitiatedAtUtc))
            .ToDictionary(group => group.Key, group => group.ToList());

        var firstDay = DateOnly.FromDateTime(fromUtc);
        var lastDay = DateOnly.FromDateTime(toUtc.AddTicks(-1));
        var result = new List<RevenueAnalyticsDayDto>();

        for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
        {
            dailyPayments.TryGetValue(day, out var dayPayments);
            dayPayments ??= [];
            result.Add(new RevenueAnalyticsDayDto(
                day,
                Usd(dayPayments.Where(payment => payment.Status == PaymentStatus.Completed).Sum(payment => payment.Amount)),
                Usd(dayPayments.Where(payment => payment.Status == PaymentStatus.Failed).Sum(payment => payment.Amount)),
                Usd(dayPayments.Where(payment => payment.Status == PaymentStatus.Pending).Sum(payment => payment.Amount)),
                Usd(dayPayments.Where(payment => payment.Status == PaymentStatus.Refunded).Sum(payment => payment.Amount))));
        }

        return result;
    }

    private static IReadOnlyList<FailureTrendPointDto> BuildFailureTrend(
        IReadOnlyCollection<RevenueAnalyticsPaymentFact> payments,
        DateTime fromUtc,
        DateTime toUtc)
    {
        var counts = payments
            .Where(payment => payment.Status == PaymentStatus.Failed
                              && payment.InitiatedAtUtc >= fromUtc && payment.InitiatedAtUtc < toUtc)
            .GroupBy(payment => (Date: DateOnly.FromDateTime(payment.InitiatedAtUtc),
                Code: ToSnakeCase(payment.FailureCode ?? PaymentFailureCode.Other)))
            .Select(group => new FailureTrendPointDto(group.Key.Date, group.Key.Code, group.Count()))
            .OrderBy(point => point.Date)
            .ThenBy(point => point.Code, StringComparer.Ordinal)
            .ToList();

        return counts;
    }

    private static IReadOnlyList<RevenueCohortDto> BuildCohorts(
        IReadOnlyCollection<RevenueAnalyticsPaymentFact> payments,
        IReadOnlyCollection<RevenueAnalyticsFirstPayment> firstPayments,
        DateOnly todayUtc)
    {
        var currentMonth = new DateOnly(todayUtc.Year, todayUtc.Month, 1);
        var firstMonth = currentMonth.AddMonths(-6);
        var candidates = firstPayments
            .Where(payment => payment.FirstPaymentAtUtc >= firstMonth.ToDateTime(TimeOnly.MinValue)
                              && payment.FirstPaymentAtUtc < currentMonth.ToDateTime(TimeOnly.MinValue))
            .ToList();

        var firstByCustomer = candidates.ToDictionary(
            payment => payment.CustomerId,
            payment => new DateOnly(payment.FirstPaymentAtUtc.Year, payment.FirstPaymentAtUtc.Month, 1));

        var completedByCustomerMonth = payments
            .Where(payment => payment.Status == PaymentStatus.Completed
                              && IsCurrency(payment.Currency, SummaryCurrency)
                              && payment.InitiatedAtUtc >= firstMonth.ToDateTime(TimeOnly.MinValue)
                              && payment.InitiatedAtUtc < currentMonth.ToDateTime(TimeOnly.MinValue)
                              && firstByCustomer.ContainsKey(payment.CustomerId))
            .GroupBy(payment => (
                CustomerId: payment.CustomerId,
                Month: new DateOnly(payment.InitiatedAtUtc.Year, payment.InitiatedAtUtc.Month, 1)))
            .ToDictionary(group => group.Key, group => group.Sum(payment => payment.Amount));

        var customersByCohort = firstByCustomer
            .GroupBy(pair => pair.Value)
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Key).ToHashSet());

        var result = new List<RevenueCohortDto>();
        for (var cohortMonth = firstMonth; cohortMonth < currentMonth; cohortMonth = cohortMonth.AddMonths(1))
        {
            if (!customersByCohort.TryGetValue(cohortMonth, out var cohortCustomers) || cohortCustomers.Count == 0)
            {
                continue;
            }

            var periods = new List<RevenueCohortPeriodDto>();
            for (var age = 0; cohortMonth.AddMonths(age) < currentMonth; age++)
            {
                var month = cohortMonth.AddMonths(age);
                var activeCustomers = cohortCustomers.Count(customer => completedByCustomerMonth.ContainsKey((customer, month)));
                var revenue = cohortCustomers.Sum(customer => completedByCustomerMonth.GetValueOrDefault((customer, month)));
                periods.Add(new RevenueCohortPeriodDto(
                    age,
                    activeCustomers,
                    (double)activeCustomers / cohortCustomers.Count,
                    Usd(revenue / cohortCustomers.Count)));
            }

            result.Add(new RevenueCohortDto(cohortMonth, cohortCustomers.Count, periods));
        }

        return result;
    }

    private static RevenueAnomalyReportDto BuildAnomalies(
        IReadOnlyCollection<RevenueAnalyticsPaymentFact> payments,
        DateOnly todayUtc)
    {
        var currentEnd = todayUtc.ToDateTime(TimeOnly.MinValue);
        var currentStart = todayUtc.AddDays(-AnomalyWindowDays).ToDateTime(TimeOnly.MinValue);
        var baselineStart = todayUtc.AddDays(-AnomalyWindowDays * 2).ToDateTime(TimeOnly.MinValue);
        var baseline = payments.Where(payment =>
            payment.InitiatedAtUtc >= baselineStart && payment.InitiatedAtUtc < currentStart).ToList();
        var current = payments.Where(payment =>
            payment.InitiatedAtUtc >= currentStart && payment.InitiatedAtUtc < currentEnd).ToList();

        var baselineFailedCount = baseline.Count(payment => payment.Status == PaymentStatus.Failed);
        var currentFailedCount = current.Count(payment => payment.Status == PaymentStatus.Failed);
        var baselineFailureRate = baseline.Count == 0 ? 0d : (double)baselineFailedCount / baseline.Count;
        var currentFailureRate = current.Count == 0 ? 0d : (double)currentFailedCount / current.Count;

        FailureRateSpikeDto? failureSpike = baseline.Count >= MinimumPaymentsPerFailureWindow
                                            && current.Count >= MinimumPaymentsPerFailureWindow
                                            && currentFailedCount >= 3
                                            && currentFailureRate - baselineFailureRate >= FailureRateIncreaseThreshold
            ? new FailureRateSpikeDto(
                currentFailureRate,
                baselineFailureRate,
                currentFailedCount,
                current.Count,
                baseline.Count)
            : null;

        var baselineRevenue = RevenueByCurrency(baseline);
        var currentRevenue = RevenueByCurrency(current);
        var drops = baselineRevenue
            .Where(pair => pair.Value.Count >= MinimumBaselineRevenuePayments && pair.Value.Amount > 0m)
            .Select(pair =>
            {
                var currentValue = currentRevenue.GetValueOrDefault(pair.Key);
                var changePercent = (double)((currentValue.Amount - pair.Value.Amount) / pair.Value.Amount) * 100d;
                return new RevenueDropAnomalyDto(
                    pair.Key,
                    MoneyDto.From(currentValue.Amount, pair.Key),
                    MoneyDto.From(pair.Value.Amount, pair.Key),
                    changePercent,
                    currentValue.Count,
                    pair.Value.Count);
            })
            .Where(drop => drop.ChangePercent <= -RevenueDropThreshold * 100d)
            .OrderBy(drop => drop.ChangePercent)
            .ToList();

        return new RevenueAnomalyReportDto(AnomalyWindowDays, failureSpike, drops);
    }

    private static Dictionary<string, (decimal Amount, int Count)> RevenueByCurrency(
        IEnumerable<RevenueAnalyticsPaymentFact> payments) =>
        payments
            .Where(payment => payment.Status == PaymentStatus.Completed)
            .GroupBy(payment => payment.Currency.ToUpperInvariant())
            .ToDictionary(
                group => group.Key,
                group => (group.Sum(payment => payment.Amount), group.Count()),
                StringComparer.OrdinalIgnoreCase);

    private static string ToSnakeCase(PaymentFailureCode code) =>
        code switch
        {
            PaymentFailureCode.CardDeclined => "card_declined",
            PaymentFailureCode.InsufficientFunds => "insufficient_funds",
            PaymentFailureCode.ExpiredCard => "expired_card",
            PaymentFailureCode.AccountRestricted => "account_restricted",
            _ => "other"
        };

    private static bool IsCurrency(string currency, string expected) =>
        string.Equals(currency, expected, StringComparison.OrdinalIgnoreCase);

    private static MoneyDto Usd(decimal amount) => MoneyDto.From(amount, SummaryCurrency);
}
