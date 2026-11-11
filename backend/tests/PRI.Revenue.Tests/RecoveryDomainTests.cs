using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;

namespace PRI.Revenue.Tests;

public class RevenueIssueTests
{
    [Fact]
    public void An_issue_must_reference_a_payment_or_an_invoice()
    {
        Assert.Throws<ArgumentException>(() => RevenueIssue.Create(
            IssueType.FailedPayment, null, null, null, 10m, "USD", null, DataSource.Demo, T.Now, T.Now));
    }

    [Fact]
    public void The_amount_must_be_positive_and_the_currency_is_normalized()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RevenueIssue.Create(
            IssueType.FailedPayment, null, Guid.NewGuid(), null, 0m, "USD", null, DataSource.Demo, T.Now, T.Now));

        var issue = RevenueIssue.Create(
            IssueType.FailedPayment, null, Guid.NewGuid(), null, 10m, " usd ", null, DataSource.Demo, T.Now, T.Now);

        Assert.Equal("USD", issue.Currency);
        Assert.Equal(IssueStatus.Open, issue.Status);
        Assert.Equal(0m, issue.RecoveredAmount);
    }

    [Fact]
    public void Recovering_sets_the_recovered_amount_and_evidence_once()
    {
        var issue = T.FailedIssue(T.Payment(PaymentStatus.Failed, 80m));

        issue.MarkRecovered(RecoveryEvidence.MatchingRetryPayment, T.Now.AddHours(-2), T.Now);
        issue.MarkRecovered(RecoveryEvidence.StatusChanged, T.Now, T.Now);   // ikinci çağrı yok sayılır
        issue.MarkVoid(T.Now);                                              // kurtarılmış kayıp void olmaz

        Assert.Equal(IssueStatus.Recovered, issue.Status);
        Assert.Equal(80m, issue.RecoveredAmount);
        Assert.Equal(RecoveryEvidence.MatchingRetryPayment, issue.Evidence);
        Assert.Equal(T.Now.AddHours(-2), issue.ResolvedAtUtc);
    }

    [Fact]
    public void A_void_issue_recovers_nothing()
    {
        var issue = T.PendingIssue(T.Payment(PaymentStatus.Pending, initiatedDaysAgo: 5));

        issue.MarkVoid(T.Now);
        issue.MarkRecovered(RecoveryEvidence.StatusChanged, T.Now, T.Now);

        Assert.Equal(IssueStatus.Void, issue.Status);
        Assert.Equal(0m, issue.RecoveredAmount);
    }

    [Fact]
    public void Days_outstanding_counts_whole_days_since_the_problem_began_and_never_goes_negative()
    {
        var issue = T.PendingIssue(T.Payment(PaymentStatus.Pending, initiatedDaysAgo: 5));

        Assert.Equal(5, issue.DaysOutstanding(T.Now));
        Assert.Equal(0, issue.DaysOutstanding(T.Now.AddDays(-10)));
    }
}

public class RecoveryActionTests
{
    private static RecoveryAction Proposed(bool executable = true)
    {
        var invoice = T.Invoice(InvoiceStatus.Sent, amount: 250m);

        return RecoveryAction.Propose(
            T.OverdueIssue(invoice), RecoveryActionType.SendReminder, executable, "  Because.  ", "tr",
            ContentSource.Ai, Guid.NewGuid(), T.Now);
    }

    [Fact]
    public void A_new_suggestion_is_proposed_and_carries_the_issue_amount_as_the_ceiling()
    {
        var action = Proposed();

        Assert.Equal(RecoveryActionStatus.Proposed, action.Status);
        Assert.False(action.IsDecided);
        Assert.Equal(250m, action.ExpectedRecovery);
        Assert.Equal("USD", action.Currency);
        Assert.Equal("Because.", action.Rationale);
        Assert.Equal("tr", action.RationaleLanguage);
    }

    [Fact]
    public void Approving_records_who_and_when()
    {
        var action = Proposed();
        var user = Guid.NewGuid();

        action.Approve(user, T.Now);

        Assert.Equal(RecoveryActionStatus.Approved, action.Status);
        Assert.Equal(user, action.DecidedByUserId);
        Assert.Equal(T.Now, action.DecidedAtUtc);
        Assert.True(action.IsDecided);
    }

    [Fact]
    public void A_decided_suggestion_cannot_be_decided_again()
    {
        var approved = Proposed();
        approved.Approve(Guid.NewGuid(), T.Now);
        var rejected = Proposed();
        rejected.Reject(Guid.NewGuid(), T.Now);

        Assert.Throws<InvalidOperationException>(() => approved.Approve(Guid.NewGuid(), T.Now));
        Assert.Throws<InvalidOperationException>(() => approved.Reject(Guid.NewGuid(), T.Now));
        Assert.Throws<InvalidOperationException>(() => rejected.Approve(Guid.NewGuid(), T.Now));
    }

    [Fact]
    public void Nothing_can_be_executed_without_approval()
    {
        var action = Proposed();

        Assert.Throws<InvalidOperationException>(() => action.MarkExecuted(T.Now));
        Assert.Throws<InvalidOperationException>(() => action.MarkFailed("x", T.Now));
    }

    [Fact]
    public void A_non_executable_suggestion_is_never_executed_even_when_approved()
    {
        var action = Proposed(executable: false);
        action.Approve(Guid.NewGuid(), T.Now);

        Assert.Throws<InvalidOperationException>(() => action.MarkExecuted(T.Now));
    }

    [Fact]
    public void An_approved_executable_suggestion_ends_as_executed_or_failed()
    {
        var executed = Proposed();
        executed.Approve(Guid.NewGuid(), T.Now);
        executed.MarkExecuted(T.Now.AddSeconds(3));

        var failed = Proposed();
        failed.Approve(Guid.NewGuid(), T.Now);
        failed.MarkFailed(new string('e', 300), T.Now);

        Assert.Equal(RecoveryActionStatus.Executed, executed.Status);
        Assert.Equal(T.Now.AddSeconds(3), executed.ExecutedAtUtc);
        Assert.Null(executed.ExecutionError);
        Assert.Equal(RecoveryActionStatus.Failed, failed.Status);
        Assert.Equal(100, failed.ExecutionError!.Length);
    }

    [Fact]
    public void An_executed_suggestion_cannot_be_executed_again()
    {
        var action = Proposed();
        action.Approve(Guid.NewGuid(), T.Now);
        action.MarkExecuted(T.Now);

        Assert.Throws<InvalidOperationException>(() => action.MarkExecuted(T.Now));
    }
}
