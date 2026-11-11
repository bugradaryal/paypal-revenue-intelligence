using PRI.Application.Revenue.Interfaces;
using PRI.Domain.Common.Interfaces;
using PRI.Domain.Revenue;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;
using PRI.Domain.Revenue.Interfaces;

namespace PRI.Application.Revenue.Services;

/// <summary>
/// Gelir kayıplarını kurallarla (<see cref="IssueRules"/>) tespit eder ve açık olanların çözüm durumunu günceller.
/// Tamamen deterministiktir; yapay zekâ kullanılmaz. Yeniden çalıştırmak güvenlidir (aynı kayıp tekrar oluşmaz).
/// </summary>
public sealed class RevenueIssueSync(
    IRevenueIssueRepository issues,
    IRevenueFactsReader facts,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRevenueIssueSync
{
    public async Task<IssueSyncResult> RefreshAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var open = await issues.ListOpenAsync(cancellationToken);
        var recent = await facts.ListPaymentsSinceAsync(now.AddDays(-IssueRules.LookbackDays), cancellationToken);

        var paymentsById = recent.ToDictionary(p => p.Id);
        var missingPaymentIds = open
            .Where(i => i.PaymentId is not null)
            .Select(i => i.PaymentId!.Value)
            .Where(id => !paymentsById.ContainsKey(id))
            .Distinct()
            .ToList();
        if (missingPaymentIds.Count > 0)
        {
            foreach (var payment in await facts.ListPaymentsByIdsAsync(missingPaymentIds, cancellationToken))
            {
                paymentsById[payment.Id] = payment;
            }
        }

        var unsettled = await facts.ListUnsettledInvoicesAsync(cancellationToken);
        var invoicesById = unsettled.ToDictionary(i => i.Id);
        var missingInvoiceIds = open
            .Where(i => i.InvoiceId is not null)
            .Select(i => i.InvoiceId!.Value)
            .Concat(recent.Where(p => p.Status == PaymentStatus.Failed && p.InvoiceId is not null).Select(p => p.InvoiceId!.Value))
            .Where(id => !invoicesById.ContainsKey(id))
            .Distinct()
            .ToList();
        if (missingInvoiceIds.Count > 0)
        {
            foreach (var invoice in await facts.ListInvoicesByIdsAsync(missingInvoiceIds, cancellationToken))
            {
                invoicesById[invoice.Id] = invoice;
            }
        }

        var completed = recent.Where(p => p.Status == PaymentStatus.Completed).ToList();
        var recovered = 0;
        var voided = 0;

        // 1) Açık kayıpların güncel durumu.
        foreach (var issue in open)
        {
            (recovered, voided) = Apply(issue, Evaluate(issue), (recovered, voided), now);
        }

        // 2) Yeni kayıplar.
        var existing = await issues.ListKeysAsync(cancellationToken);
        var created = 0;

        foreach (var payment in recent)
        {
            IssueType type;
            DateTime since;

            if (payment.Status == PaymentStatus.Failed)
            {
                type = IssueType.FailedPayment;
                since = payment.UpdatedAtUtc;
            }
            else if (IssueRules.IsStalePending(payment, now))
            {
                type = IssueType.PendingPayment;
                since = payment.InitiatedAtUtc;
            }
            else
            {
                continue;
            }

            if (!existing.Add(new IssueKey(type, payment.Id, null)))
            {
                continue;
            }

            var issue = RevenueIssue.Create(
                type, payment.CustomerId, payment.Id, payment.InvoiceId, payment.Amount, payment.Currency,
                payment.FailureCode, payment.DataSource, since, now);

            (recovered, voided) = Apply(issue, Evaluate(issue), (recovered, voided), now);
            if (AddUnlessVoid(issue))
            {
                created++;
            }
        }

        foreach (var invoice in unsettled)
        {
            if (!IssueRules.IsOverdue(invoice, today) || invoice.DueDate is not { } due)
            {
                continue;
            }

            if (!existing.Add(new IssueKey(IssueType.OverdueInvoice, null, invoice.Id)) || invoice.Amount <= 0)
            {
                continue;
            }

            var issue = RevenueIssue.Create(
                IssueType.OverdueInvoice, invoice.CustomerId, null, invoice.Id, invoice.Amount, invoice.Currency,
                null, invoice.DataSource, IssueRules.OverdueSince(due), now);

            (recovered, voided) = Apply(issue, Evaluate(issue), (recovered, voided), now);
            if (AddUnlessVoid(issue))
            {
                created++;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new IssueSyncResult(created, recovered, voided);

        IssueResolution Evaluate(RevenueIssue issue)
        {
            var payment = issue.PaymentId is { } paymentId && paymentsById.TryGetValue(paymentId, out var p) ? p : null;
            var invoiceId = issue.InvoiceId ?? payment?.InvoiceId;
            var invoice = invoiceId is { } id && invoicesById.TryGetValue(id, out var i) ? i : null;

            return IssueRules.Evaluate(issue, payment, invoice, completed, today, now);
        }

        bool AddUnlessVoid(RevenueIssue issue)
        {
            if (issue.Status == IssueStatus.Void)
            {
                return false;
            }

            issues.Add(issue);

            return true;
        }
    }

    private static (int Recovered, int Voided) Apply(
        RevenueIssue issue, IssueResolution resolution, (int Recovered, int Voided) counts, DateTime now)
    {
        switch (resolution.Outcome)
        {
            case IssueOutcome.Recovered:
                issue.MarkRecovered(resolution.Evidence!.Value, resolution.ResolvedAtUtc ?? now, now);
                return (counts.Recovered + 1, counts.Voided);
            case IssueOutcome.Void:
                issue.MarkVoid(now);
                return (counts.Recovered, counts.Voided + 1);
            default:
                return counts;
        }
    }
}
