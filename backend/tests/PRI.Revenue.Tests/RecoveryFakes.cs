using PRI.Application.Common.DTOs;
using PRI.Application.Common.Interfaces;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Domain.Common.Interfaces;
using PRI.Domain.Revenue;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;
using PRI.Domain.Revenue.Interfaces;
using PRI.Shared.Models;

namespace PRI.Revenue.Tests;

internal static class T
{
    public static readonly DateTime Now = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    public static readonly DateOnly Today = DateOnly.FromDateTime(Now);

    public static PaymentFact Payment(
        PaymentStatus status,
        decimal amount = 100m,
        Guid? customerId = null,
        Guid? invoiceId = null,
        PaymentFailureCode? failure = null,
        int initiatedDaysAgo = 1,
        int updatedDaysAgo = 1,
        Guid? id = null,
        string currency = "USD",
        DataSource source = DataSource.Paypal) =>
        new(id ?? Guid.NewGuid(), customerId ?? Guid.NewGuid(), invoiceId, source, amount, currency, status,
            status == PaymentStatus.Failed ? failure ?? PaymentFailureCode.CardDeclined : null,
            Now.AddDays(-initiatedDaysAgo), Now.AddDays(-updatedDaysAgo));

    public static InvoiceFact Invoice(
        InvoiceStatus status,
        int dueDaysAgo = 5,
        decimal amount = 200m,
        Guid? customerId = null,
        Guid? id = null,
        DataSource source = DataSource.Paypal) =>
        new(id ?? Guid.NewGuid(), customerId ?? Guid.NewGuid(), source, "INV2-" + Guid.NewGuid().ToString("N")[..8],
            amount, "USD", status, Today.AddDays(-dueDaysAgo), Now.AddHours(-1));

    public static RevenueIssue FailedIssue(PaymentFact payment, decimal? amount = null) =>
        RevenueIssue.Create(
            IssueType.FailedPayment, payment.CustomerId, payment.Id, payment.InvoiceId, amount ?? payment.Amount,
            payment.Currency, payment.FailureCode, payment.DataSource, payment.UpdatedAtUtc, Now);

    public static RevenueIssue PendingIssue(PaymentFact payment) =>
        RevenueIssue.Create(
            IssueType.PendingPayment, payment.CustomerId, payment.Id, payment.InvoiceId, payment.Amount,
            payment.Currency, null, payment.DataSource, payment.InitiatedAtUtc, Now);

    public static RevenueIssue OverdueIssue(InvoiceFact invoice) =>
        RevenueIssue.Create(
            IssueType.OverdueInvoice, invoice.CustomerId, null, invoice.Id, invoice.Amount, invoice.Currency,
            null, invoice.DataSource, IssueRules.OverdueSince(invoice.DueDate!.Value), Now);
}

internal sealed class ManualTimeProvider(DateTime startUtc) : TimeProvider
{
    private DateTime _now = startUtc;

    public override DateTimeOffset GetUtcNow() => new(_now, TimeSpan.Zero);

    public void Advance(TimeSpan by) => _now += by;
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;

        return Task.FromResult(0);
    }
}

internal sealed class FakeIssueRepository : IRevenueIssueRepository
{
    public List<RevenueIssue> Issues { get; } = [];

    public Task<IReadOnlyList<RevenueIssue>> ListOpenAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RevenueIssue>>(Issues.Where(i => i.Status == IssueStatus.Open).ToList());

    public Task<HashSet<IssueKey>> ListKeysAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Issues
            .Select(i => new IssueKey(i.Type, i.PaymentId, i.PaymentId is null ? i.InvoiceId : null))
            .ToHashSet());

    public void Add(RevenueIssue issue) => Issues.Add(issue);
}

internal sealed class FakeActionRepository : IRecoveryActionRepository
{
    public List<RecoveryAction> Actions { get; } = [];

    public Task<RecoveryAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Actions.FirstOrDefault(a => a.Id == id));

    public Task<HashSet<Guid>> ListIssueIdsWithBlockingActionsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Actions.Where(a => a.Status != RecoveryActionStatus.Failed).Select(a => a.IssueId).ToHashSet());

    public void Add(RecoveryAction action) => Actions.Add(action);
}

internal sealed class FakeInsightRepository : IAiInsightRepository
{
    public List<AiInsight> Insights { get; } = [];

    public void Add(AiInsight insight) => Insights.Add(insight);

    public Task<(IReadOnlyList<AiInsight> Items, int Total)> ListLatestAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult<(IReadOnlyList<AiInsight>, int)>((
            Insights.OrderByDescending(i => i.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Insights.Count));
}

internal sealed class FakeFactsReader : IRevenueFactsReader
{
    public List<PaymentFact> Payments { get; } = [];

    public List<InvoiceFact> Invoices { get; } = [];

    public Task<IReadOnlyList<PaymentFact>> ListPaymentsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PaymentFact>>(Payments.Where(p => p.InitiatedAtUtc >= sinceUtc).ToList());

    public Task<IReadOnlyList<PaymentFact>> ListPaymentsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PaymentFact>>(Payments.Where(p => ids.Contains(p.Id)).ToList());

    public Task<IReadOnlyList<InvoiceFact>> ListUnsettledInvoicesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<InvoiceFact>>(Invoices
            .Where(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.PartiallyPaid or InvoiceStatus.PaymentPending)
            .ToList());

    public Task<IReadOnlyList<InvoiceFact>> ListInvoicesByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<InvoiceFact>>(Invoices.Where(i => ids.Contains(i.Id)).ToList());
}

internal sealed class FakeAiGateway : IAiGateway
{
    public Func<AiTextRequest, AiTextResult> Behavior { get; set; } =
        _ => throw new InvalidOperationException("No behavior configured.");

    public List<AiTextRequest> Requests { get; } = [];

    public Task<AiTextResult> GenerateAsync(AiTextRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        return Task.FromResult(Behavior(request));
    }
}

internal sealed class FakeQueue : IRecoveryActionQueue
{
    public List<Guid> Enqueued { get; } = [];

    public ValueTask EnqueueAsync(Guid actionId)
    {
        Enqueued.Add(actionId);

        return ValueTask.CompletedTask;
    }
}

internal sealed class FakePayPalClient : IPayPalClient
{
    public List<string> Reminders { get; } = [];

    public bool IsConfigured => true;

    public string Environment => "sandbox";

    public Exception? ReminderError { get; set; }

    public Task TestConnectionAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task SendInvoiceReminderAsync(string invoiceId, CancellationToken cancellationToken)
    {
        if (ReminderError is not null)
        {
            throw ReminderError;
        }

        Reminders.Add(invoiceId);

        return Task.CompletedTask;
    }

    public Task<PayPalTransactionPage> SearchTransactionsAsync(DateTime fromUtc, DateTime toUtc, int page, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<PayPalInvoicePage> ListInvoicesAsync(int page, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

internal sealed class FakeIssueSync : IRevenueIssueSync
{
    public int Calls { get; private set; }

    public Task<IssueSyncResult> RefreshAsync(CancellationToken cancellationToken)
    {
        Calls++;

        return Task.FromResult(new IssueSyncResult(0, 0, 0));
    }
}

internal sealed class FakeRationaleWriter : IRationaleWriter
{
    public List<(RationaleFacts Facts, string Language)> Calls { get; } = [];

    public ContentSource Source { get; set; } = ContentSource.Rule;

    public Task<WrittenText> WriteRationaleAsync(RationaleFacts facts, string language, CancellationToken cancellationToken)
    {
        Calls.Add((facts, language));

        return Task.FromResult(new WrittenText($"why-{Calls.Count}", Source, Source == ContentSource.Ai ? Guid.NewGuid() : null));
    }

    public Task<WrittenText> WriteInsightCommentaryAsync(InsightFacts facts, string language, CancellationToken cancellationToken) =>
        Task.FromResult(new WrittenText("insight-commentary", Source, null));
}

internal sealed class FakeRevenueQueries : IRevenueQueries
{
    /// <summary>GetInsightFactsAsync sırayla bu listeden döner (önce güncel dönem, sonra önceki dönem).</summary>
    public Queue<InsightFacts> InsightFactsQueue { get; } = new();

    public Task<InsightFacts> GetInsightFactsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        Task.FromResult(InsightFactsQueue.Dequeue());

    public Task<SuggestionDto> GetSuggestionAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(new SuggestionDto(
            id, Guid.NewGuid(), IssueType.OverdueInvoice, null, null, null, RecoveryActionType.SendReminder, "r", "en",
            ContentSource.Rule, new MoneyDto("1.00", "USD"), RecoveryActionStatus.Approved, true, null, DataSource.Demo, T.Now, T.Now));

    public Task<PagedResult<PaymentDto>> ListPaymentsAsync(PaymentListQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<PaymentDetailDto> GetPaymentAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<RevenueSummaryDto> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<PagedResult<InvoiceDto>> ListInvoicesAsync(InvoiceListQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<PagedResult<CustomerDto>> ListCustomersAsync(CustomerListQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<RecoverySummaryDto> GetRecoverySummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<PagedResult<IssueDto>> ListIssuesAsync(IssueListQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<PagedResult<SuggestionDto>> ListSuggestionsAsync(SuggestionListQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
}
