using Microsoft.Extensions.Logging.Abstractions;
using PRI.Application.Revenue.Services;
using PRI.Domain.Revenue;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;
using PRI.Shared.Exceptions;

namespace PRI.Revenue.Tests;

public class SuggestionServiceTests
{
    private sealed class Harness
    {
        public Harness()
        {
            Service = new SuggestionService(
                Sync, Issues, Actions, Facts, Writer, Queue, Queries, UnitOfWork, new ManualTimeProvider(T.Now));
        }

        public FakeIssueSync Sync { get; } = new();

        public FakeIssueRepository Issues { get; } = new();

        public FakeActionRepository Actions { get; } = new();

        public FakeFactsReader Facts { get; } = new();

        public FakeRationaleWriter Writer { get; } = new();

        public FakeQueue Queue { get; } = new();

        public FakeRevenueQueries Queries { get; } = new();

        public FakeUnitOfWork UnitOfWork { get; } = new();

        public SuggestionService Service { get; }

        public RevenueIssue AddOverdue(decimal amount = 200m, DataSource source = DataSource.Paypal)
        {
            var invoice = T.Invoice(InvoiceStatus.Sent, amount: amount, source: source);
            Facts.Invoices.Add(invoice);
            var issue = T.OverdueIssue(invoice);
            Issues.Issues.Add(issue);

            return issue;
        }

        public RecoveryAction AddProposed(bool executable)
        {
            var issue = AddOverdue();
            var action = RecoveryAction.Propose(
                issue, RecoveryActionType.SendReminder, executable, "why", "en", ContentSource.Rule, null, T.Now);
            Actions.Add(action);

            return action;
        }
    }

    // ---------- üretim ----------

    [Fact]
    public async Task Generate_refreshes_issues_first_and_proposes_one_suggestion_per_open_issue()
    {
        var h = new Harness();
        h.AddOverdue(200m);
        h.Issues.Add(T.FailedIssue(T.Payment(PaymentStatus.Failed, 75m, failure: PaymentFailureCode.CardDeclined)));

        var result = await h.Service.GenerateAsync("en", CancellationToken.None);

        Assert.Equal(1, h.Sync.Calls);
        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(2, h.Actions.Actions.Count);
        Assert.All(h.Actions.Actions, a => Assert.Equal(RecoveryActionStatus.Proposed, a.Status));
        Assert.Equal(1, h.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task The_type_and_executability_come_from_the_rules_not_the_writer()
    {
        var h = new Harness();
        var overdue = h.AddOverdue(200m);
        var failed = T.FailedIssue(T.Payment(PaymentStatus.Failed, 75m, failure: PaymentFailureCode.ExpiredCard));
        h.Issues.Add(failed);

        await h.Service.GenerateAsync("en", CancellationToken.None);

        var reminder = h.Actions.Actions.Single(a => a.IssueId == overdue.Id);
        var alternative = h.Actions.Actions.Single(a => a.IssueId == failed.Id);
        Assert.Equal(RecoveryActionType.SendReminder, reminder.Type);
        Assert.True(reminder.Executable);
        Assert.Equal(200m, reminder.ExpectedRecovery);
        Assert.Equal(RecoveryActionType.OfferAlternativeMethod, alternative.Type);
        Assert.False(alternative.Executable);
    }

    [Fact]
    public async Task A_demo_invoice_reminder_is_proposed_but_not_executable()
    {
        var h = new Harness();
        h.AddOverdue(source: DataSource.Demo);

        await h.Service.GenerateAsync("en", CancellationToken.None);

        Assert.False(h.Actions.Actions.Single().Executable);
    }

    [Fact]
    public async Task The_requested_language_is_passed_to_the_writer_and_stored_with_the_suggestion()
    {
        var h = new Harness { };
        h.Writer.Source = ContentSource.Ai;
        h.AddOverdue();

        await h.Service.GenerateAsync("tr", CancellationToken.None);

        var (facts, language) = Assert.Single(h.Writer.Calls);
        var action = Assert.Single(h.Actions.Actions);
        Assert.Equal("tr", language);
        Assert.Equal("tr", action.RationaleLanguage);
        Assert.Equal(ContentSource.Ai, action.RationaleSource);
        Assert.NotNull(action.GatewayRequestId);
        Assert.Equal(RecoveryActionType.SendReminder, facts.ActionType);
        Assert.Equal(200m, facts.Amount);
        Assert.Equal(4, facts.DaysOutstanding);   // vade günü sonundan (4,5 gün önce) bu yana tam gün
    }

    [Fact]
    public async Task At_most_ten_suggestions_are_created_per_run_highest_amounts_first()
    {
        var h = new Harness();
        for (var amount = 1; amount <= 12; amount++)
        {
            h.AddOverdue(amount);
        }

        var result = await h.Service.GenerateAsync("en", CancellationToken.None);

        Assert.Equal(SuggestionService.MaxPerRun, result.CreatedCount);
        Assert.DoesNotContain(h.Actions.Actions, a => a.ExpectedRecovery <= 2m);
        Assert.Contains(h.Actions.Actions, a => a.ExpectedRecovery == 12m);
    }

    [Fact]
    public async Task A_second_run_adds_the_remaining_issues_and_never_duplicates()
    {
        var h = new Harness();
        for (var amount = 1; amount <= 12; amount++)
        {
            h.AddOverdue(amount);
        }

        await h.Service.GenerateAsync("en", CancellationToken.None);
        var second = await h.Service.GenerateAsync("en", CancellationToken.None);
        var third = await h.Service.GenerateAsync("en", CancellationToken.None);

        Assert.Equal(2, second.CreatedCount);
        Assert.Equal(0, third.CreatedCount);
        Assert.Equal(12, h.Actions.Actions.Select(a => a.IssueId).Distinct().Count());
    }

    [Theory]
    [InlineData(RecoveryActionStatus.Proposed, false)]
    [InlineData(RecoveryActionStatus.Approved, false)]
    [InlineData(RecoveryActionStatus.Rejected, false)]
    [InlineData(RecoveryActionStatus.Executed, false)]
    [InlineData(RecoveryActionStatus.Failed, true)]
    public async Task An_issue_is_proposed_again_only_when_its_previous_suggestion_failed(RecoveryActionStatus existing, bool reproposed)
    {
        var h = new Harness();
        var action = h.AddProposed(executable: true);
        switch (existing)
        {
            case RecoveryActionStatus.Approved:
                action.Approve(Guid.NewGuid(), T.Now);
                break;
            case RecoveryActionStatus.Rejected:
                action.Reject(Guid.NewGuid(), T.Now);
                break;
            case RecoveryActionStatus.Executed:
                action.Approve(Guid.NewGuid(), T.Now);
                action.MarkExecuted(T.Now);
                break;
            case RecoveryActionStatus.Failed:
                action.Approve(Guid.NewGuid(), T.Now);
                action.MarkFailed("paypal_unavailable", T.Now);
                break;
        }

        var result = await h.Service.GenerateAsync("en", CancellationToken.None);

        Assert.Equal(reproposed ? 1 : 0, result.CreatedCount);
    }

    [Fact]
    public async Task Recovered_issues_get_no_suggestion()
    {
        var h = new Harness();
        var issue = h.AddOverdue();
        issue.MarkRecovered(RecoveryEvidence.StatusChanged, T.Now, T.Now);

        var result = await h.Service.GenerateAsync("en", CancellationToken.None);

        Assert.Equal(0, result.CreatedCount);
        Assert.Empty(h.Writer.Calls);
    }

    // ---------- karar ----------

    [Fact]
    public async Task Approving_an_executable_suggestion_queues_it_for_background_execution()
    {
        var h = new Harness();
        var action = h.AddProposed(executable: true);
        var user = Guid.NewGuid();

        var dto = await h.Service.DecideAsync(action.Id, "approve", user, CancellationToken.None);

        Assert.Equal(RecoveryActionStatus.Approved, action.Status);
        Assert.Equal(user, action.DecidedByUserId);
        Assert.Equal(action.Id, Assert.Single(h.Queue.Enqueued));
        Assert.Equal(action.Id, dto.Id);
        Assert.Equal(1, h.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Approving_an_advice_only_suggestion_does_not_queue_anything()
    {
        var h = new Harness();
        var action = h.AddProposed(executable: false);

        await h.Service.DecideAsync(action.Id, "APPROVE", Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(RecoveryActionStatus.Approved, action.Status);
        Assert.Empty(h.Queue.Enqueued);
    }

    [Fact]
    public async Task Rejecting_never_queues_and_never_executes()
    {
        var h = new Harness();
        var action = h.AddProposed(executable: true);

        await h.Service.DecideAsync(action.Id, "reject", Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(RecoveryActionStatus.Rejected, action.Status);
        Assert.Empty(h.Queue.Enqueued);
    }

    [Fact]
    public async Task A_second_decision_is_a_conflict_and_does_not_queue_twice()
    {
        var h = new Harness();
        var action = h.AddProposed(executable: true);
        await h.Service.DecideAsync(action.Id, "approve", Guid.NewGuid(), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => h.Service.DecideAsync(action.Id, "approve", Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("suggestion_already_decided", ex.Code);
        Assert.Single(h.Queue.Enqueued);
    }

    [Fact]
    public async Task Deciding_an_unknown_suggestion_is_not_found()
    {
        var h = new Harness();

        await Assert.ThrowsAsync<NotFoundException>(
            () => h.Service.DecideAsync(Guid.NewGuid(), "approve", Guid.NewGuid(), CancellationToken.None));
    }
}

public class RecoveryActionExecutorTests
{
    private sealed class Harness
    {
        public Harness()
        {
            Executor = new RecoveryActionExecutor(
                Actions, Facts, PayPal, UnitOfWork, new ManualTimeProvider(T.Now), NullLogger<RecoveryActionExecutor>.Instance);
        }

        public FakeActionRepository Actions { get; } = new();

        public FakeFactsReader Facts { get; } = new();

        public FakePayPalClient PayPal { get; } = new();

        public FakeUnitOfWork UnitOfWork { get; } = new();

        public RecoveryActionExecutor Executor { get; }

        public (RecoveryAction Action, InvoiceFact Invoice) Approved(
            InvoiceStatus status = InvoiceStatus.Sent, DataSource source = DataSource.Paypal, bool executable = true, bool approve = true)
        {
            var invoice = T.Invoice(status, source: source);
            Facts.Invoices.Add(invoice);
            var action = RecoveryAction.Propose(
                T.OverdueIssue(invoice), RecoveryActionType.SendReminder, executable, "why", "en", ContentSource.Rule, null, T.Now);
            if (approve)
            {
                action.Approve(Guid.NewGuid(), T.Now);
            }

            Actions.Add(action);

            return (action, invoice);
        }
    }

    [Fact]
    public async Task An_approved_reminder_is_sent_through_paypal_and_recorded_as_executed()
    {
        var h = new Harness();
        var (action, invoice) = h.Approved();

        await h.Executor.ExecuteAsync(action.Id, CancellationToken.None);

        Assert.Equal(invoice.ExternalId, Assert.Single(h.PayPal.Reminders));
        Assert.Equal(RecoveryActionStatus.Executed, action.Status);
        Assert.NotNull(action.ExecutedAtUtc);
        Assert.Equal(1, h.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task A_paypal_error_is_recorded_as_failed_with_its_error_code()
    {
        var h = new Harness();
        h.PayPal.ReminderError = new ExternalServiceException("paypal_unavailable", "down");
        var (action, _) = h.Approved();

        await h.Executor.ExecuteAsync(action.Id, CancellationToken.None);

        Assert.Equal(RecoveryActionStatus.Failed, action.Status);
        Assert.Equal("paypal_unavailable", action.ExecutionError);
    }

    [Fact]
    public async Task An_unexpected_error_is_recorded_as_internal_error_instead_of_crashing_the_worker()
    {
        var h = new Harness();
        h.PayPal.ReminderError = new InvalidOperationException("boom");
        var (action, _) = h.Approved();

        await h.Executor.ExecuteAsync(action.Id, CancellationToken.None);

        Assert.Equal(RecoveryActionStatus.Failed, action.Status);
        Assert.Equal("internal_error", action.ExecutionError);
    }

    [Fact]
    public async Task A_suggestion_that_was_never_approved_is_never_executed()
    {
        var h = new Harness();
        var (action, _) = h.Approved(approve: false);

        await h.Executor.ExecuteAsync(action.Id, CancellationToken.None);

        Assert.Empty(h.PayPal.Reminders);
        Assert.Equal(RecoveryActionStatus.Proposed, action.Status);
        Assert.Equal(0, h.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task An_approved_advice_only_suggestion_is_never_executed()
    {
        var h = new Harness();
        var (action, _) = h.Approved(executable: false);

        await h.Executor.ExecuteAsync(action.Id, CancellationToken.None);

        Assert.Empty(h.PayPal.Reminders);
        Assert.Equal(RecoveryActionStatus.Approved, action.Status);
    }

    [Fact]
    public async Task Executing_twice_sends_only_one_reminder()
    {
        var h = new Harness();
        var (action, _) = h.Approved();

        await h.Executor.ExecuteAsync(action.Id, CancellationToken.None);
        await h.Executor.ExecuteAsync(action.Id, CancellationToken.None);

        Assert.Single(h.PayPal.Reminders);
    }

    [Theory]
    [InlineData(InvoiceStatus.Paid, DataSource.Paypal, "invoice_not_remindable")]
    [InlineData(InvoiceStatus.Cancelled, DataSource.Paypal, "invoice_not_remindable")]
    [InlineData(InvoiceStatus.Sent, DataSource.Demo, "invoice_not_found")]
    public async Task A_reminder_for_an_invoice_that_changed_since_approval_fails_without_calling_paypal(
        InvoiceStatus status, DataSource source, string expectedError)
    {
        var h = new Harness();
        var (action, _) = h.Approved(InvoiceStatus.Sent);
        // Onaydan sonra faturanın durumu/kaynağı değişti.
        h.Facts.Invoices[0] = h.Facts.Invoices[0] with { Status = status, DataSource = source };

        await h.Executor.ExecuteAsync(action.Id, CancellationToken.None);

        Assert.Empty(h.PayPal.Reminders);
        Assert.Equal(RecoveryActionStatus.Failed, action.Status);
        Assert.Equal(expectedError, action.ExecutionError);
    }

    [Fact]
    public async Task An_unknown_suggestion_is_ignored()
    {
        var h = new Harness();

        await h.Executor.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(0, h.UnitOfWork.SaveCount);
    }
}
