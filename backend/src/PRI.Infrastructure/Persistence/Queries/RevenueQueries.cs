using PRI.Application.Common.DTOs;
using Microsoft.EntityFrameworkCore;
using PRI.Application.Common;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;
using PRI.Infrastructure.Persistence.Context;
using PRI.Infrastructure.Persistence.Seed;
using PRI.Shared.Exceptions;
using PRI.Shared.Models;
using Microsoft.Extensions.Options;

namespace PRI.Infrastructure.Persistence.Queries;

/// <summary>Revenue modülünün salt okunur sorguları (EF projection). Para toplamları veritabanında hesaplanır, LLM'e bırakılmaz.</summary>
public sealed class RevenueQueries(
    AppDbContext db,
    TimeProvider timeProvider,
    IOptions<SeedSettings> seedOptions) : IRevenueQueries
{
    // MVP kararı: özet rakamlar yalnızca USD'dir; diğer para birimleri excludedPaymentCount ile bildirilir.
    private const string SummaryCurrency = "USD";

    private sealed record CustomerRow(Customer Customer, int PaymentCount, int FailedPaymentCount);

    private IQueryable<Payment> ReportablePayments() =>
        seedOptions.Value.DemoData
            ? db.Payments.AsNoTracking()
            : db.Payments.AsNoTracking().Where(p => p.DataSource == DataSource.Paypal);

    private IQueryable<Invoice> ReportableInvoices() =>
        seedOptions.Value.DemoData
            ? db.Invoices.AsNoTracking()
            : db.Invoices.AsNoTracking().Where(i => i.DataSource == DataSource.Paypal);

    private IQueryable<Customer> ReportableCustomers() =>
        seedOptions.Value.DemoData
            ? db.Customers.AsNoTracking()
            : db.Customers.AsNoTracking().Where(c => c.DataSource == DataSource.Paypal);

    private IQueryable<RevenueIssue> ReportableIssues() =>
        seedOptions.Value.DemoData
            ? db.RevenueIssues.AsNoTracking()
            : db.RevenueIssues.AsNoTracking().Where(i => i.DataSource == DataSource.Paypal);

    private IQueryable<RecoveryAction> ReportableActions() =>
        seedOptions.Value.DemoData
            ? db.RecoveryActions.AsNoTracking()
            : db.RecoveryActions.AsNoTracking().Where(a => a.DataSource == DataSource.Paypal);

    // ---------- Payments ----------

    public async Task<PagedResult<PaymentDto>> ListPaymentsAsync(PaymentListQuery query, CancellationToken cancellationToken)
    {
        var payments = ReportablePayments();
        var statuses = ParseEnums<PaymentStatus>(query.Status);
        if (statuses.Count > 0)
        {
            payments = payments.Where(p => statuses.Contains(p.Status));
        }

        var rows = from p in payments
                   join c in db.Customers.AsNoTracking() on p.CustomerId equals c.Id
                   select new { Payment = p, CustomerName = c.Name, CustomerEmail = c.Email };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            rows = rows.Where(x =>
                x.CustomerName.ToLower().Contains(term)
                || (x.CustomerEmail != null && x.CustomerEmail.ToLower().Contains(term))
                || x.Payment.ExternalId.ToLower().Contains(term)
                || x.Payment.Id.ToString().Contains(term));
        }

        if (query.CustomerId is { } customerId)
        {
            rows = rows.Where(x => x.Payment.CustomerId == customerId);
        }

        if (query.From is { } from)
        {
            var fromUtc = from.UtcDateTime;
            rows = rows.Where(x => x.Payment.InitiatedAtUtc >= fromUtc);
        }

        if (query.To is { } to)
        {
            var toUtc = to.UtcDateTime;
            rows = rows.Where(x => x.Payment.InitiatedAtUtc < toUtc);
        }

        var descending = !string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var byAmount = string.Equals(query.SortBy, "amount", StringComparison.OrdinalIgnoreCase);

        var ordered = byAmount
            ? (descending ? rows.OrderByDescending(x => x.Payment.Amount) : rows.OrderBy(x => x.Payment.Amount))
            : (descending ? rows.OrderByDescending(x => x.Payment.InitiatedAtUtc) : rows.OrderBy(x => x.Payment.InitiatedAtUtc));

        var total = await rows.CountAsync(cancellationToken);
        var page = await ordered
            .ThenBy(x => x.Payment.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PaymentDto>(
            page.Select(x => ToDto(x.Payment, x.CustomerName)).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<PaymentDetailDto> GetPaymentAsync(Guid id, CancellationToken cancellationToken)
    {
        var payment = await ReportablePayments()
            .Include(p => p.Events)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException("Payment", id);

        var customerName = await db.Customers.AsNoTracking()
            .Where(c => c.Id == payment.CustomerId)
            .Select(c => c.Name)
            .FirstAsync(cancellationToken);

        var history = payment.Events
            .OrderBy(e => e.OccurredAtUtc)
            .Select(e => new PaymentStatusEventDto(e.Status, e.OccurredAtUtc))
            .ToList();

        var dto = ToDto(payment, customerName);

        return new PaymentDetailDto(
            dto.Id, dto.CustomerId, dto.CustomerName, dto.Amount, dto.Status, dto.FailureCode,
            dto.PaypalTransactionId, dto.DataSource, dto.CreatedAt, dto.UpdatedAt, history);
    }

    private static PaymentDto ToDto(Payment p, string customerName) =>
        new(p.Id, p.CustomerId, customerName, MoneyDto.From(p.Amount, p.Currency), p.Status, p.FailureCode,
            p.DataSource == DataSource.Paypal ? p.ExternalId : null, p.DataSource, p.InitiatedAtUtc, p.UpdatedAtUtc);

    // ---------- Summary ----------

    public async Task<RevenueSummaryDto> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var inRange = ReportablePayments()
            .Where(p => p.InitiatedAtUtc >= fromUtc && p.InitiatedAtUtc < toUtc);

        var excluded = await inRange.CountAsync(p => p.Currency != SummaryCurrency, cancellationToken);
        var usd = inRange.Where(p => p.Currency == SummaryCurrency);

        var byStatus = await usd
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(p => p.Amount) })
            .ToListAsync(cancellationToken);

        var completedRows = await usd
            .Where(p => p.Status == PaymentStatus.Completed)
            .Select(p => new { p.InitiatedAtUtc, p.Amount })
            .ToListAsync(cancellationToken);

        // Gün bucket'ları UTC gününe göredir; boş günler sıfırla doldurulur.
        var dailyTotals = completedRows
            .GroupBy(r => DateOnly.FromDateTime(r.InitiatedAtUtc))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

        // Kurtarılan gelir: kurtarılma anı (ResolvedAtUtc) aralıkta olan kayıplar (akış tanımı). Kural tabanlıdır, bkz. IssueRules.
        var recoveredRows = await ReportableIssues()
            .Where(i => i.Status == IssueStatus.Recovered && i.Currency == SummaryCurrency
                        && i.ResolvedAtUtc >= fromUtc && i.ResolvedAtUtc < toUtc)
            .Select(i => new { ResolvedAtUtc = i.ResolvedAtUtc!.Value, i.RecoveredAmount })
            .ToListAsync(cancellationToken);

        var dailyRecovered = recoveredRows
            .GroupBy(r => DateOnly.FromDateTime(r.ResolvedAtUtc))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.RecoveredAmount));

        var series = new List<RevenueSeriesPointDto>();
        var lastDay = DateOnly.FromDateTime(toUtc.AddTicks(-1));
        for (var day = DateOnly.FromDateTime(fromUtc); day <= lastDay; day = day.AddDays(1))
        {
            dailyTotals.TryGetValue(day, out var revenue);
            dailyRecovered.TryGetValue(day, out var recovered);
            series.Add(new RevenueSeriesPointDto(
                day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Usd(revenue),
                Usd(recovered)));
        }

        return new RevenueSummaryDto(
            new DateRangeDto(fromUtc, toUtc),
            Usd(TotalOf(byStatus.Select(s => (s.Status, s.Total)), PaymentStatus.Completed)),
            Usd(TotalOf(byStatus.Select(s => (s.Status, s.Total)), PaymentStatus.Failed)),
            Usd(TotalOf(byStatus.Select(s => (s.Status, s.Total)), PaymentStatus.Pending)),
            Usd(recoveredRows.Sum(r => r.RecoveredAmount)),
            byStatus.Sum(s => s.Count),
            byStatus.Where(s => s.Status == PaymentStatus.Failed).Sum(s => s.Count),
            byStatus.Where(s => s.Status == PaymentStatus.Pending).Sum(s => s.Count),
            excluded,
            series);
    }

    private static decimal TotalOf(IEnumerable<(PaymentStatus Status, decimal Total)> totals, PaymentStatus status) =>
        totals.Where(t => t.Status == status).Sum(t => t.Total);

    private static MoneyDto Usd(decimal amount) => MoneyDto.From(amount, SummaryCurrency);

    // ---------- Invoices ----------

    public async Task<PagedResult<InvoiceDto>> ListInvoicesAsync(InvoiceListQuery query, CancellationToken cancellationToken)
    {
        var invoices = ReportableInvoices();
        var statuses = ParseEnums<InvoiceStatus>(query.Status);
        if (statuses.Count > 0)
        {
            invoices = invoices.Where(i => statuses.Contains(i.Status));
        }

        var rows = from i in invoices
                   join c in db.Customers.AsNoTracking() on i.CustomerId equals (Guid?)c.Id into joined
                   from c in joined.DefaultIfEmpty()
                   select new { Invoice = i, CustomerName = c == null ? null : c.Name };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            rows = rows.Where(x =>
                (x.Invoice.InvoiceNumber != null && x.Invoice.InvoiceNumber.ToLower().Contains(term))
                || (x.CustomerName != null && x.CustomerName.ToLower().Contains(term)));
        }

        if (query.From is { } from)
        {
            var fromUtc = from.UtcDateTime;
            rows = rows.Where(x => x.Invoice.IssuedAtUtc >= fromUtc);
        }

        if (query.To is { } to)
        {
            var toUtc = to.UtcDateTime;
            rows = rows.Where(x => x.Invoice.IssuedAtUtc < toUtc);
        }

        var descending = !string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var ordered = query.SortBy.ToLowerInvariant() switch
        {
            "amount" => descending ? rows.OrderByDescending(x => x.Invoice.Amount) : rows.OrderBy(x => x.Invoice.Amount),
            "duedate" => descending ? rows.OrderByDescending(x => x.Invoice.DueDate) : rows.OrderBy(x => x.Invoice.DueDate),
            _ => descending ? rows.OrderByDescending(x => x.Invoice.IssuedAtUtc) : rows.OrderBy(x => x.Invoice.IssuedAtUtc)
        };

        var total = await rows.CountAsync(cancellationToken);
        var page = await ordered
            .ThenBy(x => x.Invoice.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var items = page.Select(x => new InvoiceDto(
            x.Invoice.Id,
            x.Invoice.InvoiceNumber,
            x.Invoice.CustomerId,
            x.CustomerName,
            MoneyDto.From(x.Invoice.Amount, x.Invoice.Currency),
            x.Invoice.Status,
            x.Invoice.IsOverdue(today),
            x.Invoice.DueDate,
            x.Invoice.IssuedAtUtc,
            x.Invoice.UpdatedAtUtc,
            x.Invoice.DataSource == DataSource.Paypal ? x.Invoice.ExternalId : null,
            x.Invoice.DataSource)).ToList();

        return new PagedResult<InvoiceDto>(items, query.Page, query.PageSize, total);
    }

    // ---------- Customers ----------

    public async Task<PagedResult<CustomerDto>> ListCustomersAsync(CustomerListQuery query, CancellationToken cancellationToken)
    {
        var customers = ReportableCustomers();
        var payments = ReportablePayments();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            customers = customers.Where(c =>
                c.Name.ToLower().Contains(term) || (c.Email != null && c.Email.ToLower().Contains(term)));
        }

        var descending = !string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var byName = string.Equals(query.SortBy, "name", StringComparison.OrdinalIgnoreCase);

        var ordered = byName
            ? (descending ? customers.OrderByDescending(c => c.Name) : customers.OrderBy(c => c.Name))
            : (descending ? customers.OrderByDescending(c => c.CreatedAtUtc) : customers.OrderBy(c => c.CreatedAtUtc));

        var total = await customers.CountAsync(cancellationToken);
        var page = await ordered
            .ThenBy(c => c.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CustomerRow(
                c,
                payments.Count(p => p.CustomerId == c.Id),
                payments.Count(p => p.CustomerId == c.Id && p.Status == PaymentStatus.Failed)))
            .ToListAsync(cancellationToken);

        var items = page.Select(x => new CustomerDto(
            x.Customer.Id, x.Customer.Name, x.Customer.Email, x.Customer.DataSource,
            x.PaymentCount, x.FailedPaymentCount, x.Customer.CreatedAtUtc)).ToList();

        return new PagedResult<CustomerDto>(items, query.Page, query.PageSize, total);
    }

    // ---------- Recovery ----------

    private sealed record SuggestionRow(RecoveryAction Action, IssueType IssueType, string? CustomerName);

    public async Task<RecoverySummaryDto> GetRecoverySummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        // Kohort tanımı: aralıkta BAŞLAYAN kayıplar ve bunlardan kurtarılanlar. Yalnızca USD, void kayıtlar hariç.
        var issues = ReportableIssues()
            .Where(i => i.Currency == SummaryCurrency && i.Status != IssueStatus.Void
                        && i.ProblemSinceUtc >= fromUtc && i.ProblemSinceUtc < toUtc);

        var lost = await issues.SumAsync(i => i.Amount, cancellationToken);
        var recovered = await issues.SumAsync(i => i.RecoveredAmount, cancellationToken);
        var reportableIssueIds = ReportableIssues().Select(i => i.Id);
        var openSuggestions = await ReportableActions()
            .CountAsync(a => a.Status == RecoveryActionStatus.Proposed && reportableIssueIds.Contains(a.IssueId), cancellationToken);

        return new RecoverySummaryDto(
            new DateRangeDto(fromUtc, toUtc),
            Usd(lost),
            Usd(recovered),
            lost == 0 ? 0d : Math.Round((double)(recovered / lost), 4),
            openSuggestions);
    }

    public async Task<PagedResult<IssueDto>> ListIssuesAsync(IssueListQuery query, CancellationToken cancellationToken)
    {
        var issues = ReportableIssues();
        var statuses = ParseEnums<IssueStatus>(query.Status);
        if (statuses.Count > 0)
        {
            issues = issues.Where(i => statuses.Contains(i.Status));
        }

        var types = ParseEnums<IssueType>(query.Type);
        if (types.Count > 0)
        {
            issues = issues.Where(i => types.Contains(i.Type));
        }

        var rows = from i in issues
                   join c in db.Customers.AsNoTracking() on i.CustomerId equals (Guid?)c.Id into joined
                   from c in joined.DefaultIfEmpty()
                   select new { Issue = i, CustomerName = c == null ? null : c.Name };

        var descending = !string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var byAmount = string.Equals(query.SortBy, "amount", StringComparison.OrdinalIgnoreCase);
        var ordered = byAmount
            ? (descending ? rows.OrderByDescending(x => x.Issue.Amount) : rows.OrderBy(x => x.Issue.Amount))
            : (descending ? rows.OrderByDescending(x => x.Issue.ProblemSinceUtc) : rows.OrderBy(x => x.Issue.ProblemSinceUtc));

        var total = await rows.CountAsync(cancellationToken);
        var page = await ordered
            .ThenBy(x => x.Issue.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = page.Select(x => new IssueDto(
            x.Issue.Id, x.Issue.Type, x.Issue.Status, x.Issue.PaymentId, x.Issue.InvoiceId, x.Issue.CustomerId, x.CustomerName,
            MoneyDto.From(x.Issue.Amount, x.Issue.Currency), MoneyDto.From(x.Issue.RecoveredAmount, x.Issue.Currency),
            x.Issue.FailureCode, x.Issue.DataSource, x.Issue.ProblemSinceUtc, x.Issue.CreatedAtUtc,
            x.Issue.ResolvedAtUtc, x.Issue.Evidence)).ToList();

        return new PagedResult<IssueDto>(items, query.Page, query.PageSize, total);
    }

    public async Task<PagedResult<SuggestionDto>> ListSuggestionsAsync(SuggestionListQuery query, CancellationToken cancellationToken)
    {
        var statuses = ParseEnums<RecoveryActionStatus>(query.Status);
        var rows = from a in ReportableActions()
                   join i in ReportableIssues() on a.IssueId equals i.Id
                   join c in db.Customers.AsNoTracking() on a.CustomerId equals (Guid?)c.Id into joined
                   from c in joined.DefaultIfEmpty()
                   select new { Action = a, IssueType = i.Type, CustomerName = c == null ? null : c.Name };

        if (statuses.Count > 0)
        {
            rows = rows.Where(x => statuses.Contains(x.Action.Status));
        }

        if (query.PaymentId is { } paymentId)
        {
            rows = rows.Where(x => x.Action.PaymentId == paymentId);
        }

        var descending = !string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var byAmount = string.Equals(query.SortBy, "expectedRecovery", StringComparison.OrdinalIgnoreCase);
        var ordered = byAmount
            ? (descending ? rows.OrderByDescending(x => x.Action.ExpectedRecovery) : rows.OrderBy(x => x.Action.ExpectedRecovery))
            : (descending ? rows.OrderByDescending(x => x.Action.CreatedAtUtc) : rows.OrderBy(x => x.Action.CreatedAtUtc));

        var total = await rows.CountAsync(cancellationToken);
        var page = await ordered
            .ThenBy(x => x.Action.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new SuggestionRow(x.Action, x.IssueType, x.CustomerName))
            .ToListAsync(cancellationToken);

        return new PagedResult<SuggestionDto>(page.Select(x => ToDto(x)).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<SuggestionDto> GetSuggestionAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await (from a in ReportableActions()
                         join i in ReportableIssues() on a.IssueId equals i.Id
                         join c in db.Customers.AsNoTracking() on a.CustomerId equals (Guid?)c.Id into joined
                         from c in joined.DefaultIfEmpty()
                         where a.Id == id
                         select new SuggestionRow(a, i.Type, c == null ? null : c.Name))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Suggestion", id);

        return ToDto(row);
    }

    private static SuggestionDto ToDto(SuggestionRow x) =>
        new(x.Action.Id, x.Action.IssueId, x.IssueType, x.Action.PaymentId, x.Action.InvoiceId, x.CustomerName,
            x.Action.Type, x.Action.Rationale, x.Action.RationaleLanguage, x.Action.RationaleSource,
            MoneyDto.From(x.Action.ExpectedRecovery, x.Action.Currency), x.Action.Status, x.Action.Executable,
            x.Action.ExecutionError, x.Action.DataSource, x.Action.CreatedAtUtc, x.Action.UpdatedAtUtc);

    public async Task<InsightFacts> GetInsightFactsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var payments = ReportablePayments()
            .Where(p => p.InitiatedAtUtc >= fromUtc && p.InitiatedAtUtc < toUtc && p.Currency == SummaryCurrency);

        var byStatus = await payments
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(p => p.Amount) })
            .ToListAsync(cancellationToken);

        var reasons = await payments
            .Where(p => p.Status == PaymentStatus.Failed && p.FailureCode != null)
            .GroupBy(p => p.FailureCode)
            .Select(g => new { Code = g.Key, Count = g.Count(), Amount = g.Sum(p => p.Amount) })
            .ToListAsync(cancellationToken);

        var topReasons = reasons
            .Where(r => r.Code is not null)
            .OrderByDescending(r => r.Amount)
            .Take(3)
            .Select(r => new FailureReasonFact(r.Code!.Value, r.Count, r.Amount))
            .ToList();

        var issues = ReportableIssues()
            .Where(i => i.Currency == SummaryCurrency && i.Status != IssueStatus.Void
                        && i.ProblemSinceUtc >= fromUtc && i.ProblemSinceUtc < toUtc);
        var lost = await issues.SumAsync(i => i.Amount, cancellationToken);
        var recovered = await issues.SumAsync(i => i.RecoveredAmount, cancellationToken);

        // Vadesi geçmiş faturalar "şu anki" durumdur (aralıktan bağımsız).
        var overdue = ReportableIssues()
            .Where(i => i.Type == IssueType.OverdueInvoice && i.Status == IssueStatus.Open && i.Currency == SummaryCurrency);
        var overdueCount = await overdue.CountAsync(cancellationToken);
        var overdueAmount = await overdue.SumAsync(i => i.Amount, cancellationToken);

        return new InsightFacts(
            0,
            TotalOf(byStatus.Select(s => (s.Status, s.Total)), PaymentStatus.Completed),
            TotalOf(byStatus.Select(s => (s.Status, s.Total)), PaymentStatus.Failed),
            TotalOf(byStatus.Select(s => (s.Status, s.Total)), PaymentStatus.Pending),
            byStatus.Sum(s => s.Count),
            byStatus.Where(s => s.Status == PaymentStatus.Failed).Sum(s => s.Count),
            lost,
            recovered,
            overdueCount,
            overdueAmount,
            topReasons,
            0m,
            0m);
    }

    private static List<TEnum> ParseEnums<TEnum>(IEnumerable<string> values) where TEnum : struct, Enum
    {
        var result = new List<TEnum>();

        foreach (var value in values)
        {
            if (SnakeCaseEnum.TryParse<TEnum>(value, out var parsed))
            {
                result.Add(parsed);
            }
        }

        return result;
    }
}
