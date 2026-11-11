using Microsoft.EntityFrameworkCore;
using PRI.Domain.Revenue;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;
using PRI.Domain.Revenue.Interfaces;
using PRI.Infrastructure.Persistence.Context;
using PRI.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.Options;

namespace PRI.Infrastructure.Persistence.Repositories;

public sealed class RevenueIssueRepository(AppDbContext db, IOptions<SeedSettings> seedOptions) : IRevenueIssueRepository
{
    public async Task<IReadOnlyList<RevenueIssue>> ListOpenAsync(CancellationToken cancellationToken)
    {
        var issues = db.RevenueIssues.Where(i => i.Status == IssueStatus.Open);
        if (!seedOptions.Value.DemoData)
        {
            issues = issues.Where(i => i.DataSource == DataSource.Paypal);
        }

        return await issues.ToListAsync(cancellationToken);
    }

    public async Task<HashSet<IssueKey>> ListKeysAsync(CancellationToken cancellationToken)
    {
        var issues = db.RevenueIssues.AsNoTracking();
        if (!seedOptions.Value.DemoData)
        {
            issues = issues.Where(i => i.DataSource == DataSource.Paypal);
        }

        var rows = await issues
            .Select(i => new { i.Type, i.PaymentId, i.InvoiceId })
            .ToListAsync(cancellationToken);

        // Ödeme tabanlı kayıplar ödemeyle, fatura tabanlı kayıplar faturayla tanımlanır.
        return rows.Select(r => new IssueKey(r.Type, r.PaymentId, r.PaymentId is null ? r.InvoiceId : null)).ToHashSet();
    }

    public void Add(RevenueIssue issue) => db.RevenueIssues.Add(issue);
}

public sealed class RecoveryActionRepository(AppDbContext db, IOptions<SeedSettings> seedOptions) : IRecoveryActionRepository
{
    public Task<RecoveryAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var actions = db.RecoveryActions.AsQueryable();
        if (!seedOptions.Value.DemoData)
        {
            actions = actions.Where(a => a.DataSource == DataSource.Paypal);
        }

        return actions.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<HashSet<Guid>> ListIssueIdsWithBlockingActionsAsync(CancellationToken cancellationToken)
    {
        var actions = db.RecoveryActions.AsNoTracking();
        if (!seedOptions.Value.DemoData)
        {
            actions = actions.Where(a => a.DataSource == DataSource.Paypal);
        }

        return (await actions
            .Where(a => a.Status != RecoveryActionStatus.Failed)
            .Select(a => a.IssueId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();
    }

    public void Add(RecoveryAction action) => db.RecoveryActions.Add(action);
}

public sealed class AiInsightRepository(AppDbContext db) : IAiInsightRepository
{
    public void Add(AiInsight insight) => db.AiInsights.Add(insight);

    public async Task<(IReadOnlyList<AiInsight> Items, int Total)> ListLatestAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var total = await db.AiInsights.AsNoTracking().CountAsync(cancellationToken);
        var items = await db.AiInsights.AsNoTracking()
            .OrderByDescending(i => i.CreatedAtUtc).ThenBy(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}

public sealed class RevenueFactsReader(AppDbContext db, IOptions<SeedSettings> seedOptions) : IRevenueFactsReader
{
    public async Task<IReadOnlyList<PaymentFact>> ListPaymentsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken) =>
        await ReportablePayments()
            .Where(p => p.InitiatedAtUtc >= sinceUtc)
            .Select(p => new PaymentFact(
                p.Id, p.CustomerId, p.InvoiceId, p.DataSource, p.Amount, p.Currency, p.Status, p.FailureCode,
                p.InitiatedAtUtc, p.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentFact>> ListPaymentsByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var list = ids.ToList();

        return await ReportablePayments()
            .Where(p => list.Contains(p.Id))
            .Select(p => new PaymentFact(
                p.Id, p.CustomerId, p.InvoiceId, p.DataSource, p.Amount, p.Currency, p.Status, p.FailureCode,
                p.InitiatedAtUtc, p.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InvoiceFact>> ListUnsettledInvoicesAsync(CancellationToken cancellationToken)
    {
        var statuses = new List<InvoiceStatus> { InvoiceStatus.Sent, InvoiceStatus.PartiallyPaid, InvoiceStatus.PaymentPending };

        return await ReportableInvoices()
            .Where(i => statuses.Contains(i.Status))
            .Select(i => new InvoiceFact(
                i.Id, i.CustomerId, i.DataSource, i.ExternalId, i.Amount, i.Currency, i.Status, i.DueDate, i.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InvoiceFact>> ListInvoicesByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var list = ids.ToList();

        return await ReportableInvoices()
            .Where(i => list.Contains(i.Id))
            .Select(i => new InvoiceFact(
                i.Id, i.CustomerId, i.DataSource, i.ExternalId, i.Amount, i.Currency, i.Status, i.DueDate, i.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Payment> ReportablePayments()
    {
        var payments = db.Payments.AsNoTracking();
        return seedOptions.Value.DemoData ? payments : payments.Where(p => p.DataSource == DataSource.Paypal);
    }

    private IQueryable<Invoice> ReportableInvoices()
    {
        var invoices = db.Invoices.AsNoTracking();
        return seedOptions.Value.DemoData ? invoices : invoices.Where(i => i.DataSource == DataSource.Paypal);
    }
}
