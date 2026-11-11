using PRI.Domain.Revenue;
using PRI.Domain.Revenue.Enums;

namespace PRI.Revenue.Tests;

public class IssueRulesTests
{
    private static IssueResolution Evaluate(
        PRI.Domain.Revenue.Entities.RevenueIssue issue,
        PaymentFact? payment = null,
        InvoiceFact? invoice = null,
        params PaymentFact[] completed) =>
        IssueRules.Evaluate(issue, payment, invoice, completed, T.Today, T.Now);

    // ---------- tespit ----------

    [Theory]
    [InlineData(4, true)]
    [InlineData(3, true)]
    [InlineData(2, false)]
    public void Pending_payments_are_stale_after_three_days(int initiatedDaysAgo, bool stale)
    {
        var payment = T.Payment(PaymentStatus.Pending, initiatedDaysAgo: initiatedDaysAgo);

        Assert.Equal(stale, IssueRules.IsStalePending(payment, T.Now));
    }

    [Fact]
    public void Only_pending_payments_can_be_stale()
    {
        Assert.False(IssueRules.IsStalePending(T.Payment(PaymentStatus.Completed, initiatedDaysAgo: 30), T.Now));
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent, 1, true)]
    [InlineData(InvoiceStatus.PartiallyPaid, 1, true)]
    [InlineData(InvoiceStatus.Sent, 0, false)]          // vade günü bugün: henüz gecikmedi
    [InlineData(InvoiceStatus.Sent, -3, false)]         // vadesi gelecekte
    [InlineData(InvoiceStatus.PaymentPending, 5, false)]
    [InlineData(InvoiceStatus.Paid, 5, false)]
    [InlineData(InvoiceStatus.Cancelled, 5, false)]
    public void Overdue_means_sent_or_partially_paid_and_past_due(InvoiceStatus status, int dueDaysAgo, bool overdue)
    {
        Assert.Equal(overdue, IssueRules.IsOverdue(T.Invoice(status, dueDaysAgo), T.Today));
    }

    [Fact]
    public void An_invoice_without_a_due_date_is_never_overdue()
    {
        var invoice = T.Invoice(InvoiceStatus.Sent) with { DueDate = null };

        Assert.False(IssueRules.IsOverdue(invoice, T.Today));
    }

    // ---------- başarısız ödeme ----------

    [Fact]
    public void A_failed_payment_without_any_later_payment_stays_open()
    {
        var failed = T.Payment(PaymentStatus.Failed);

        Assert.Equal(IssueOutcome.StillOpen, Evaluate(T.FailedIssue(failed), failed).Outcome);
    }

    [Fact]
    public void A_failed_issue_without_its_payment_record_is_not_judged()
    {
        var failed = T.Payment(PaymentStatus.Failed);

        Assert.Equal(IssueOutcome.StillOpen, Evaluate(T.FailedIssue(failed), payment: null).Outcome);
    }

    [Fact]
    public void A_failed_payment_that_the_provider_later_completes_is_recovered_by_status_change()
    {
        var failed = T.Payment(PaymentStatus.Failed);
        var issue = T.FailedIssue(failed);
        var nowCompleted = failed with { Status = PaymentStatus.Completed, FailureCode = null };

        var result = Evaluate(issue, nowCompleted);

        Assert.Equal(IssueOutcome.Recovered, result.Outcome);
        Assert.Equal(RecoveryEvidence.StatusChanged, result.Evidence);
    }

    [Fact]
    public void A_later_completed_payment_on_the_same_invoice_recovers_the_failed_one()
    {
        var invoiceId = Guid.NewGuid();
        var failed = T.Payment(PaymentStatus.Failed, invoiceId: invoiceId, initiatedDaysAgo: 5, updatedDaysAgo: 5);
        var later = T.Payment(PaymentStatus.Completed, amount: 75m, invoiceId: invoiceId, initiatedDaysAgo: 2, updatedDaysAgo: 2);

        var result = Evaluate(T.FailedIssue(failed), failed, null, later);

        Assert.Equal(IssueOutcome.Recovered, result.Outcome);
        Assert.Equal(RecoveryEvidence.LinkedInvoicePayment, result.Evidence);
        Assert.Equal(later.UpdatedAtUtc, result.ResolvedAtUtc);
    }

    [Fact]
    public void A_paid_linked_invoice_recovers_the_failed_payment()
    {
        var invoice = T.Invoice(InvoiceStatus.Paid);
        var failed = T.Payment(PaymentStatus.Failed, invoiceId: invoice.Id);

        var result = Evaluate(T.FailedIssue(failed), failed, invoice);

        Assert.Equal(RecoveryEvidence.LinkedInvoicePayment, result.Evidence);
    }

    [Fact]
    public void A_completed_payment_with_the_same_customer_and_amount_soon_after_is_a_matching_retry()
    {
        var customer = Guid.NewGuid();
        var failed = T.Payment(PaymentStatus.Failed, 49.90m, customer, initiatedDaysAgo: 6, updatedDaysAgo: 6);
        var retry = T.Payment(PaymentStatus.Completed, 49.90m, customer, initiatedDaysAgo: 3, updatedDaysAgo: 3);

        var result = Evaluate(T.FailedIssue(failed), failed, null, retry);

        Assert.Equal(IssueOutcome.Recovered, result.Outcome);
        Assert.Equal(RecoveryEvidence.MatchingRetryPayment, result.Evidence);
    }

    [Theory]
    [InlineData("other-amount")]
    [InlineData("other-customer")]
    [InlineData("other-currency")]
    [InlineData("before-failure")]
    [InlineData("too-late")]
    public void Look_alike_payments_that_do_not_match_the_rule_do_not_recover(string reason)
    {
        var customer = Guid.NewGuid();
        var failed = T.Payment(PaymentStatus.Failed, 49.90m, customer, initiatedDaysAgo: 40, updatedDaysAgo: 40);

        var candidate = reason switch
        {
            "other-amount" => T.Payment(PaymentStatus.Completed, 50m, customer, initiatedDaysAgo: 38),
            "other-customer" => T.Payment(PaymentStatus.Completed, 49.90m, Guid.NewGuid(), initiatedDaysAgo: 38),
            "other-currency" => T.Payment(PaymentStatus.Completed, 49.90m, customer, initiatedDaysAgo: 38, currency: "EUR"),
            "before-failure" => T.Payment(PaymentStatus.Completed, 49.90m, customer, initiatedDaysAgo: 41),
            _ => T.Payment(PaymentStatus.Completed, 49.90m, customer, initiatedDaysAgo: 10)   // 14 günlük pencerenin dışı
        };

        Assert.Equal(IssueOutcome.StillOpen, Evaluate(T.FailedIssue(failed), failed, null, candidate).Outcome);
    }

    [Fact]
    public void A_refunded_failed_payment_is_void()
    {
        var failed = T.Payment(PaymentStatus.Failed);
        var refunded = failed with { Status = PaymentStatus.Refunded, FailureCode = null };

        Assert.Equal(IssueOutcome.Void, Evaluate(T.FailedIssue(failed), refunded).Outcome);
    }

    // ---------- bekleyen ödeme ----------

    [Theory]
    [InlineData(PaymentStatus.Completed, IssueOutcome.Recovered)]
    [InlineData(PaymentStatus.Failed, IssueOutcome.Void)]
    [InlineData(PaymentStatus.Refunded, IssueOutcome.Void)]
    [InlineData(PaymentStatus.Pending, IssueOutcome.StillOpen)]
    public void A_pending_issue_follows_the_payments_current_status(PaymentStatus now, IssueOutcome expected)
    {
        var pending = T.Payment(PaymentStatus.Pending, initiatedDaysAgo: 5);
        var current = pending with { Status = now, FailureCode = now == PaymentStatus.Failed ? PaymentFailureCode.Other : null };

        Assert.Equal(expected, Evaluate(T.PendingIssue(pending), current).Outcome);
    }

    // ---------- vadesi geçmiş fatura ----------

    [Theory]
    [InlineData(InvoiceStatus.Paid, 5, IssueOutcome.Recovered)]
    [InlineData(InvoiceStatus.Cancelled, 5, IssueOutcome.Void)]
    [InlineData(InvoiceStatus.Refunded, 5, IssueOutcome.Void)]
    [InlineData(InvoiceStatus.Sent, 5, IssueOutcome.StillOpen)]
    [InlineData(InvoiceStatus.PartiallyPaid, 5, IssueOutcome.StillOpen)]
    [InlineData(InvoiceStatus.PaymentPending, 5, IssueOutcome.StillOpen)]
    [InlineData(InvoiceStatus.Sent, -10, IssueOutcome.Void)]   // vade tarihi ileri alındı
    public void An_overdue_invoice_issue_follows_the_invoices_current_state(InvoiceStatus status, int dueDaysAgo, IssueOutcome expected)
    {
        var original = T.Invoice(InvoiceStatus.Sent, dueDaysAgo: 5);
        var issue = T.OverdueIssue(original);
        var current = original with { Status = status, DueDate = T.Today.AddDays(-dueDaysAgo) };

        Assert.Equal(expected, Evaluate(issue, invoice: current).Outcome);
    }

    [Fact]
    public void An_overdue_issue_without_its_invoice_record_is_not_judged()
    {
        var issue = T.OverdueIssue(T.Invoice(InvoiceStatus.Sent));

        Assert.Equal(IssueOutcome.StillOpen, Evaluate(issue, invoice: null).Outcome);
    }
}

public class RecoveryPlannerTests
{
    [Fact]
    public void An_overdue_paypal_invoice_gets_an_executable_reminder()
    {
        var invoice = T.Invoice(InvoiceStatus.Sent);

        var (type, executable) = RecoveryPlanner.Plan(T.OverdueIssue(invoice), invoice);

        Assert.Equal(RecoveryActionType.SendReminder, type);
        Assert.True(executable);
    }

    [Fact]
    public void A_demo_invoice_reminder_is_advice_only()
    {
        var invoice = T.Invoice(InvoiceStatus.Sent, source: DataSource.Demo);

        Assert.False(RecoveryPlanner.Plan(T.OverdueIssue(invoice), invoice).Executable);
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent, true)]
    [InlineData(InvoiceStatus.PartiallyPaid, true)]
    [InlineData(InvoiceStatus.PaymentPending, false)]
    [InlineData(InvoiceStatus.Paid, false)]
    public void Executability_depends_on_the_invoice_still_being_remindable(InvoiceStatus status, bool executable)
    {
        var invoice = T.Invoice(status);

        Assert.Equal(executable, RecoveryPlanner.Plan(T.OverdueIssue(invoice), invoice).Executable);
    }

    [Fact]
    public void A_stale_pending_payment_linked_to_a_paypal_invoice_can_be_reminded()
    {
        var invoice = T.Invoice(InvoiceStatus.Sent);
        var pending = T.Payment(PaymentStatus.Pending, invoiceId: invoice.Id, initiatedDaysAgo: 5);

        var (type, executable) = RecoveryPlanner.Plan(T.PendingIssue(pending), invoice);

        Assert.Equal(RecoveryActionType.SendReminder, type);
        Assert.True(executable);
    }

    [Fact]
    public void A_stale_pending_payment_without_an_invoice_is_advice_only()
    {
        var pending = T.Payment(PaymentStatus.Pending, initiatedDaysAgo: 5);

        Assert.False(RecoveryPlanner.Plan(T.PendingIssue(pending), null).Executable);
    }

    [Theory]
    [InlineData(PaymentFailureCode.CardDeclined, RecoveryActionType.RetryPayment)]
    [InlineData(PaymentFailureCode.InsufficientFunds, RecoveryActionType.RetryPayment)]
    [InlineData(PaymentFailureCode.ExpiredCard, RecoveryActionType.OfferAlternativeMethod)]
    [InlineData(PaymentFailureCode.AccountRestricted, RecoveryActionType.OfferAlternativeMethod)]
    [InlineData(PaymentFailureCode.Other, RecoveryActionType.OfferAlternativeMethod)]
    public void Failed_payments_get_a_retry_or_an_alternative_method_by_failure_code_and_are_never_executable(
        PaymentFailureCode code, RecoveryActionType expected)
    {
        var failed = T.Payment(PaymentStatus.Failed, failure: code);

        var (type, executable) = RecoveryPlanner.Plan(T.FailedIssue(failed), null);

        Assert.Equal(expected, type);
        Assert.False(executable);
    }
}
