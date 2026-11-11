using PRI.Application.Revenue.Services;
using PRI.Domain.Revenue.Enums;

namespace PRI.Revenue.Tests;

public class RevenueIssueSyncTests
{
    private sealed class Harness
    {
        public Harness() =>
            Sync = new RevenueIssueSync(Issues, Facts, UnitOfWork, new ManualTimeProvider(T.Now));

        public FakeIssueRepository Issues { get; } = new();

        public FakeFactsReader Facts { get; } = new();

        public FakeUnitOfWork UnitOfWork { get; } = new();

        public RevenueIssueSync Sync { get; }
    }

    [Fact]
    public async Task A_failed_payment_becomes_one_open_issue_and_rerunning_does_not_duplicate_it()
    {
        var h = new Harness();
        var failed = T.Payment(PaymentStatus.Failed, 80m, failure: PaymentFailureCode.ExpiredCard);
        h.Facts.Payments.Add(failed);

        var first = await h.Sync.RefreshAsync(CancellationToken.None);
        var second = await h.Sync.RefreshAsync(CancellationToken.None);

        var issue = Assert.Single(h.Issues.Issues);
        Assert.Equal(1, first.Created);
        Assert.Equal(0, second.Created);
        Assert.Equal(IssueType.FailedPayment, issue.Type);
        Assert.Equal(IssueStatus.Open, issue.Status);
        Assert.Equal(80m, issue.Amount);
        Assert.Equal(PaymentFailureCode.ExpiredCard, issue.FailureCode);
        Assert.Equal(failed.UpdatedAtUtc, issue.ProblemSinceUtc);
    }

    [Fact]
    public async Task A_failure_that_was_already_retried_successfully_is_recorded_as_recovered_right_away()
    {
        var h = new Harness();
        var customer = Guid.NewGuid();
        h.Facts.Payments.Add(T.Payment(PaymentStatus.Failed, 49.90m, customer, initiatedDaysAgo: 6, updatedDaysAgo: 6));
        h.Facts.Payments.Add(T.Payment(PaymentStatus.Completed, 49.90m, customer, initiatedDaysAgo: 3, updatedDaysAgo: 3));

        var result = await h.Sync.RefreshAsync(CancellationToken.None);

        var issue = Assert.Single(h.Issues.Issues);
        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.Recovered);
        Assert.Equal(IssueStatus.Recovered, issue.Status);
        Assert.Equal(49.90m, issue.RecoveredAmount);
        Assert.Equal(RecoveryEvidence.MatchingRetryPayment, issue.Evidence);
    }

    [Fact]
    public async Task Only_pending_payments_older_than_the_threshold_become_issues()
    {
        var h = new Harness();
        h.Facts.Payments.Add(T.Payment(PaymentStatus.Pending, initiatedDaysAgo: 5));
        h.Facts.Payments.Add(T.Payment(PaymentStatus.Pending, initiatedDaysAgo: 1));
        h.Facts.Payments.Add(T.Payment(PaymentStatus.Completed, initiatedDaysAgo: 5));

        await h.Sync.RefreshAsync(CancellationToken.None);

        var issue = Assert.Single(h.Issues.Issues);
        Assert.Equal(IssueType.PendingPayment, issue.Type);
    }

    [Fact]
    public async Task Overdue_invoices_become_issues_even_without_a_customer()
    {
        var h = new Harness();
        var overdue = T.Invoice(InvoiceStatus.Sent, dueDaysAgo: 4, amount: 300m);
        h.Facts.Invoices.Add(overdue with { CustomerId = null });
        h.Facts.Invoices.Add(T.Invoice(InvoiceStatus.Sent, dueDaysAgo: -5));   // vadesi gelmemiş
        h.Facts.Invoices.Add(T.Invoice(InvoiceStatus.Paid, dueDaysAgo: 9));

        await h.Sync.RefreshAsync(CancellationToken.None);

        var issue = Assert.Single(h.Issues.Issues);
        Assert.Equal(IssueType.OverdueInvoice, issue.Type);
        Assert.Null(issue.CustomerId);
        Assert.Equal(300m, issue.Amount);
        Assert.Equal(overdue.Id, issue.InvoiceId);
        Assert.Equal(T.Today.AddDays(-3).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), issue.ProblemSinceUtc);
    }

    [Fact]
    public async Task Payments_older_than_the_lookback_window_are_not_scanned_for_new_issues()
    {
        var h = new Harness();
        h.Facts.Payments.Add(T.Payment(PaymentStatus.Failed, initiatedDaysAgo: 120, updatedDaysAgo: 120));

        var result = await h.Sync.RefreshAsync(CancellationToken.None);

        Assert.Equal(0, result.Created);
    }

    [Fact]
    public async Task An_open_issue_is_recovered_when_the_source_later_shows_the_invoice_as_paid()
    {
        var h = new Harness();
        var invoice = T.Invoice(InvoiceStatus.Sent, dueDaysAgo: 4);
        h.Facts.Invoices.Add(invoice);
        await h.Sync.RefreshAsync(CancellationToken.None);

        h.Facts.Invoices[0] = invoice with { Status = InvoiceStatus.Paid };
        var result = await h.Sync.RefreshAsync(CancellationToken.None);

        var issue = Assert.Single(h.Issues.Issues);
        Assert.Equal(1, result.Recovered);
        Assert.Equal(IssueStatus.Recovered, issue.Status);
        Assert.Equal(issue.Amount, issue.RecoveredAmount);
        Assert.Equal(RecoveryEvidence.StatusChanged, issue.Evidence);
    }

    [Fact]
    public async Task A_pending_issue_whose_payment_then_fails_is_voided_and_a_failed_issue_is_created()
    {
        var h = new Harness();
        var pending = T.Payment(PaymentStatus.Pending, initiatedDaysAgo: 5);
        h.Facts.Payments.Add(pending);
        await h.Sync.RefreshAsync(CancellationToken.None);

        h.Facts.Payments[0] = pending with { Status = PaymentStatus.Failed, FailureCode = PaymentFailureCode.Other, UpdatedAtUtc = T.Now };
        var result = await h.Sync.RefreshAsync(CancellationToken.None);

        Assert.Equal(1, result.Voided);
        Assert.Equal(1, result.Created);
        Assert.Contains(h.Issues.Issues, i => i.Type == IssueType.PendingPayment && i.Status == IssueStatus.Void);
        Assert.Contains(h.Issues.Issues, i => i.Type == IssueType.FailedPayment && i.Status == IssueStatus.Open);
    }

    [Fact]
    public async Task A_voided_or_recovered_issue_is_never_recreated()
    {
        var h = new Harness();
        var invoice = T.Invoice(InvoiceStatus.Sent, dueDaysAgo: 4);
        h.Facts.Invoices.Add(invoice);
        await h.Sync.RefreshAsync(CancellationToken.None);

        h.Facts.Invoices[0] = invoice with { Status = InvoiceStatus.Cancelled };
        await h.Sync.RefreshAsync(CancellationToken.None);
        h.Facts.Invoices[0] = invoice;   // yeniden "gönderilmiş" görünse bile aynı kayıp yeniden açılmaz
        var again = await h.Sync.RefreshAsync(CancellationToken.None);

        Assert.Equal(0, again.Created);
        Assert.Equal(IssueStatus.Void, Assert.Single(h.Issues.Issues).Status);
    }

    [Fact]
    public async Task Each_refresh_saves_exactly_once()
    {
        var h = new Harness();

        await h.Sync.RefreshAsync(CancellationToken.None);

        Assert.Equal(1, h.UnitOfWork.SaveCount);
    }
}
