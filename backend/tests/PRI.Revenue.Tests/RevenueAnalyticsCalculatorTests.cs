using PRI.Application.Common.DTOs;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Services;
using PRI.Domain.Revenue.Enums;

namespace PRI.Revenue.Tests;

public sealed class RevenueAnalyticsCalculatorTests
{
    [Fact]
    public void SeparatesPaymentOutcomesFromInvoiceExposureAndKeepsCurrencyTotalsSafe()
    {
        var from = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddDays(2);
        var today = new DateOnly(2026, 11, 12);
        var payments = new[]
        {
            Payment(1, 10m, "USD", PaymentStatus.Completed, from),
            Payment(2, 20m, "USD", PaymentStatus.Failed, from.AddDays(1), PaymentFailureCode.CardDeclined),
            Payment(3, 5m, "EUR", PaymentStatus.Failed, from.AddDays(1), PaymentFailureCode.CardDeclined),
            Payment(4, 7m, "USD", PaymentStatus.Pending, from.AddDays(1)),
            Payment(5, 2m, "USD", PaymentStatus.Refunded, from.AddDays(1))
        };
        var result = RevenueAnalyticsCalculator.Calculate(
            payments,
            [],
            CashFlow("100.00", "40.00"),
            from,
            to,
            today);

        Assert.Equal("39.00", result.Leakage.TransactionActivity.Amount);
        Assert.Equal("20.00", result.Leakage.FailedAttemptAmount.Amount);
        Assert.Equal(2, result.FailureReasons.Single().Count);
        Assert.Equal("20.00", result.FailureReasons.Single().AmountUsd.Amount);
        Assert.Equal(1, result.ExcludedPaymentCount);
        Assert.False(result.Leakage.CancellationTrackingAvailable);
        Assert.Equal("100.00", result.Leakage.CurrentOpenReceivables.Amount);
        Assert.Equal("40.00", result.Leakage.CurrentOverdueReceivables.Amount);
        Assert.Equal(2, result.Daily.Count);
    }

    [Fact]
    public void BuildsRetentionCohortsFromFirstSuccessfulPaymentOnRecord()
    {
        var today = new DateOnly(2026, 11, 12);
        var cohortMonth = new DateTime(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc);
        var payments = new[]
        {
            Payment(1, 50m, "USD", PaymentStatus.Completed, cohortMonth),
            Payment(1, 25m, "USD", PaymentStatus.Completed, new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc)),
            Payment(2, 75m, "USD", PaymentStatus.Completed, new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc))
        };
        var firstPayments = new[]
        {
            new RevenueAnalyticsFirstPayment(Customer(1), cohortMonth),
            new RevenueAnalyticsFirstPayment(Customer(2), new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc))
        };

        var result = RevenueAnalyticsCalculator.Calculate(
            payments,
            firstPayments,
            CashFlow("0.00", "0.00"),
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            today);

        var cohort = Assert.Single(result.Cohorts);
        Assert.Equal(new DateOnly(2026, 9, 1), cohort.CohortMonth);
        Assert.Equal(2, cohort.CohortSize);
        Assert.Equal(1d, cohort.Periods[0].RetentionRate);
        Assert.Equal(0.5d, cohort.Periods[1].RetentionRate);
        Assert.Equal("12.50", cohort.Periods[1].RevenuePerCustomer.Amount);
    }

    [Fact]
    public void DetectsLargeFailureRateIncreaseAgainstCompletePriorWindow()
    {
        var today = new DateOnly(2026, 11, 12);
        var currentEnd = today.ToDateTime(TimeOnly.MinValue);
        var baselineStart = today.AddDays(-28).ToDateTime(TimeOnly.MinValue);
        var currentStart = today.AddDays(-14).ToDateTime(TimeOnly.MinValue);
        var payments = Enumerable.Range(0, 20)
            .Select(index => Payment(index + 1, 10m, "USD", PaymentStatus.Completed, baselineStart.AddDays(index / 2)))
            .Concat(Enumerable.Range(0, 12).Select(index =>
                Payment(index + 100, 10m, "USD",
                    index < 4 ? PaymentStatus.Failed : PaymentStatus.Completed,
                    currentStart.AddDays(index))))
            .ToArray();

        var result = RevenueAnalyticsCalculator.Calculate(
            payments,
            [],
            CashFlow("0.00", "0.00"),
            baselineStart,
            currentEnd,
            today);

        Assert.NotNull(result.Anomalies.FailureRateSpike);
        Assert.Equal(4, result.Anomalies.FailureRateSpike.CurrentFailedCount);
        Assert.Equal(0, result.Anomalies.FailureRateSpike.BaselineRate);

        var sparseCurrent = payments.Take(20).Concat(payments.Skip(20).Take(3)).ToArray();
        var sparseResult = RevenueAnalyticsCalculator.Calculate(
            sparseCurrent,
            [],
            CashFlow("0.00", "0.00"),
            baselineStart,
            currentEnd,
            today);
        Assert.Null(sparseResult.Anomalies.FailureRateSpike);
    }

    [Fact]
    public void DetectsCurrencyRevenueDropOnlyAfterMinimumBaselineVolume()
    {
        var today = new DateOnly(2026, 11, 12);
        var baselineStart = today.AddDays(-28).ToDateTime(TimeOnly.MinValue);
        var currentStart = today.AddDays(-14).ToDateTime(TimeOnly.MinValue);
        var currentEnd = today.ToDateTime(TimeOnly.MinValue);
        var baseline = Enumerable.Range(0, 5)
            .Select(index => Payment(index + 1, 100m, "EUR", PaymentStatus.Completed, baselineStart.AddDays(index)))
            .ToArray();

        var result = RevenueAnalyticsCalculator.Calculate(
            baseline,
            [],
            CashFlow("0.00", "0.00"),
            baselineStart,
            currentEnd,
            today);

        var drop = Assert.Single(result.Anomalies.RevenueDrops);
        Assert.Equal("EUR", drop.Currency);
        Assert.Equal("0.00", drop.CurrentRevenue.Amount);
        Assert.Equal(-100d, drop.ChangePercent);

        var insufficientBaseline = RevenueAnalyticsCalculator.Calculate(
            baseline.Take(4).ToArray(),
            [],
            CashFlow("0.00", "0.00"),
            baselineStart,
            currentStart,
            today);
        Assert.Empty(insufficientBaseline.Anomalies.RevenueDrops);
    }

    private static RevenueAnalyticsPaymentFact Payment(
        int customerId,
        decimal amount,
        string currency,
        PaymentStatus status,
        DateTime at,
        PaymentFailureCode? failureCode = null) =>
        new(Customer(customerId), amount, currency, status, failureCode, at);

    private static Guid Customer(int id) =>
        Guid.Parse($"00000000-0000-0000-0000-{id:000000000000}");

    private static CashFlowOutlookDto CashFlow(string open, string overdue) =>
        new(
            new DateOnly(2026, 11, 12),
            MoneyDto.From(decimal.Parse(open, System.Globalization.CultureInfo.InvariantCulture), "USD"),
            1,
            MoneyDto.From(decimal.Parse(overdue, System.Globalization.CultureInfo.InvariantCulture), "USD"),
            1,
            MoneyDto.From(0m, "USD"),
            0,
            MoneyDto.From(0m, "USD"),
            0,
            MoneyDto.From(0m, "USD"),
            0,
            MoneyDto.From(0m, "USD"),
            0,
            [],
            [],
            [],
            []);
}
