using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Services;
using PRI.Domain.Revenue.Enums;

namespace PRI.Revenue.Tests;

public sealed class CashFlowOutlookCalculatorTests
{
    [Fact]
    public void CalculatesInvoiceBackedBalancesAndSeparatesCurrencies()
    {
        var today = new DateOnly(2026, 3, 10);
        var outlook = CashFlowOutlookCalculator.Calculate(
        [
            Invoice(1, 100m, 30m, "USD", InvoiceStatus.PartiallyPaid, today.AddDays(-9)),
            Invoice(2, 200m, 0m, "USD", InvoiceStatus.PaymentPending, today),
            Invoice(3, 75m, 0m, "USD", InvoiceStatus.Sent, today.AddDays(7)),
            Invoice(4, 40m, 0m, "USD", InvoiceStatus.Sent, null),
            Invoice(5, 25m, 0m, "EUR", InvoiceStatus.Sent, today),
            Invoice(6, 500m, 0m, "USD", InvoiceStatus.Paid, today)
        ],
        today);

        Assert.Equal("385.00", outlook.OpenReceivables.Amount);
        Assert.Equal(4, outlook.OpenInvoiceCount);
        Assert.Equal("70.00", outlook.OverdueReceivables.Amount);
        Assert.Equal(1, outlook.OverdueInvoiceCount);
        Assert.Equal("200.00", outlook.DueWithin7Days.Amount);
        Assert.Equal("275.00", outlook.DueWithin30Days.Amount);
        Assert.Equal("40.00", outlook.UnscheduledReceivables.Amount);
        Assert.Equal("70.00", outlook.OverdueAging.Single(bucket => bucket.Key == "8_30_days").Amount.Amount);
        Assert.Equal("25.00", outlook.ForeignCurrencyExposure.Single().OpenAmount.Amount);
        Assert.Equal("200.00", outlook.WeeklySchedule[0].Amount.Amount);
        Assert.Equal("75.00", outlook.WeeklySchedule[1].Amount.Amount);
    }

    [Fact]
    public void IgnoresZeroBalancesAndClosedInvoices()
    {
        var today = new DateOnly(2026, 3, 10);
        var outlook = CashFlowOutlookCalculator.Calculate(
        [
            Invoice(1, 100m, 100m, "USD", InvoiceStatus.PartiallyPaid, today),
            Invoice(2, 100m, 0m, "USD", InvoiceStatus.Cancelled, today),
            Invoice(3, 100m, 120m, "USD", InvoiceStatus.Sent, today)
        ],
        today);

        Assert.Equal("0.00", outlook.OpenReceivables.Amount);
        Assert.Empty(outlook.PriorityInvoices);
    }

    private static CashFlowInvoiceFact Invoice(
        int id,
        decimal amount,
        decimal paid,
        string currency,
        InvoiceStatus status,
        DateOnly? dueDate) =>
        new(Guid.Parse($"00000000-0000-0000-0000-{id:000000000000}"), $"INV-{id}", "Customer", amount, paid,
            currency, status, dueDate, DataSource.Demo);
}
