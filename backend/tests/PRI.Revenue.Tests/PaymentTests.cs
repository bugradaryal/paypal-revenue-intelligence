using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;

namespace PRI.Revenue.Tests;

public class PaymentTests
{
    private static readonly DateTime Now = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static Payment CreatePayment(PaymentStatus status, PaymentFailureCode? failureCode = null, decimal amount = 100m) =>
        Payment.Create(
            Guid.NewGuid(), DataSource.Demo, "TX-1", amount, "usd", status, failureCode,
            Now.AddDays(-1), Now.AddDays(-1), null, Now);

    [Fact]
    public void Create_records_the_initial_status_in_history()
    {
        var payment = CreatePayment(PaymentStatus.Pending);

        var only = Assert.Single(payment.Events);
        Assert.Equal(PaymentStatus.Pending, only.Status);
        Assert.Equal("USD", payment.Currency);
    }

    [Fact]
    public void Create_clears_failure_code_when_payment_is_not_failed()
    {
        var payment = CreatePayment(PaymentStatus.Completed, PaymentFailureCode.CardDeclined);

        Assert.Null(payment.FailureCode);
    }

    [Fact]
    public void Create_uses_other_when_failed_without_a_reason()
    {
        var payment = CreatePayment(PaymentStatus.Failed);

        Assert.Equal(PaymentFailureCode.Other, payment.FailureCode);
    }

    [Fact]
    public void Create_rejects_non_positive_amounts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePayment(PaymentStatus.Completed, amount: 0m));
    }

    [Fact]
    public void ApplyProviderUpdate_adds_a_history_entry_when_status_changes()
    {
        var payment = CreatePayment(PaymentStatus.Pending);

        var changed = payment.ApplyProviderUpdate(PaymentStatus.Completed, null, Now, Now);

        Assert.True(changed);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(2, payment.Events.Count);
    }

    [Fact]
    public void A_refunded_payment_does_not_become_completed_again()
    {
        var payment = CreatePayment(PaymentStatus.Completed);
        payment.ApplyProviderUpdate(PaymentStatus.Refunded, null, Now, Now);

        var changed = payment.ApplyProviderUpdate(PaymentStatus.Completed, null, Now.AddHours(1), Now.AddHours(1));

        Assert.False(changed);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(2, payment.Events.Count);
    }

    [Fact]
    public void ApplyProviderUpdate_returns_false_when_nothing_changed()
    {
        var payment = CreatePayment(PaymentStatus.Completed);

        var changed = payment.ApplyProviderUpdate(PaymentStatus.Completed, null, Now, Now);

        Assert.False(changed);
        Assert.Single(payment.Events);
    }
}
