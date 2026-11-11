using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;

namespace PRI.Domain.Revenue;

public enum IssueOutcome
{
    StillOpen = 1,
    Recovered = 2,
    Void = 3
}

public sealed record IssueResolution(IssueOutcome Outcome, RecoveryEvidence? Evidence = null, DateTime? ResolvedAtUtc = null)
{
    public static readonly IssueResolution StillOpen = new(IssueOutcome.StillOpen);

    public static readonly IssueResolution Void = new(IssueOutcome.Void);
}

/// <summary>Bir gelir kaybının tespit edildiği ve çözüldüğü iş kuralları. Saf fonksiyonlardır; LLM veya veritabanı bilmez.</summary>
public static class IssueRules
{
    /// <summary>Bu süreden uzun "pending" kalan ödeme sorunlu sayılır.</summary>
    public static readonly TimeSpan PendingThreshold = TimeSpan.FromDays(3);

    /// <summary>Başarısız ödemeden sonra aynı müşteri/tutarla yapılan ödeme, bu pencere içindeyse yeniden deneme sayılır.</summary>
    public static readonly TimeSpan RetryMatchWindow = TimeSpan.FromDays(14);

    /// <summary>Tespit yalnızca son bu kadar günün ödemelerine bakar.</summary>
    public const int LookbackDays = 90;

    public static bool IsStalePending(PaymentFact payment, DateTime nowUtc) =>
        payment.Status == PaymentStatus.Pending && nowUtc - payment.InitiatedAtUtc >= PendingThreshold;

    /// <summary>Invoice.IsOverdue ile aynı kural: gönderilmiş veya kısmen ödenmiş ve vadesi geçmiş.</summary>
    public static bool IsOverdue(InvoiceFact invoice, DateOnly todayUtc) =>
        invoice.DueDate is { } due
        && due < todayUtc
        && (invoice.Status == InvoiceStatus.Sent || invoice.Status == InvoiceStatus.PartiallyPaid);

    public static DateTime OverdueSince(DateOnly dueDate) =>
        dueDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    /// <summary>
    /// Açık bir kaybın güncel durumunu değerlendirir. Kaynak kayıt bulunamıyorsa (null) hüküm verilmez, kayıp açık kalır.
    /// </summary>
    public static IssueResolution Evaluate(
        RevenueIssue issue,
        PaymentFact? payment,
        InvoiceFact? invoice,
        IReadOnlyCollection<PaymentFact> completedPayments,
        DateOnly todayUtc,
        DateTime nowUtc) =>
        issue.Type switch
        {
            IssueType.FailedPayment => EvaluateFailed(issue, payment, invoice, completedPayments, nowUtc),
            IssueType.PendingPayment => EvaluatePending(payment),
            IssueType.OverdueInvoice => EvaluateOverdue(invoice, todayUtc),
            _ => IssueResolution.StillOpen
        };

    private static IssueResolution EvaluateFailed(
        RevenueIssue issue,
        PaymentFact? payment,
        InvoiceFact? invoice,
        IReadOnlyCollection<PaymentFact> completedPayments,
        DateTime nowUtc)
    {
        if (payment is null)
        {
            return IssueResolution.StillOpen;
        }

        if (payment.Status == PaymentStatus.Completed)
        {
            return Recovered(RecoveryEvidence.StatusChanged, payment.UpdatedAtUtc);
        }

        if (payment.Status == PaymentStatus.Refunded)
        {
            return IssueResolution.Void;
        }

        if (invoice is { Status: InvoiceStatus.Paid })
        {
            return Recovered(RecoveryEvidence.LinkedInvoicePayment, invoice.UpdatedAtUtc);
        }

        if (issue.InvoiceId is { } invoiceId)
        {
            var linked = completedPayments
                .Where(c => c.Id != payment.Id && c.InvoiceId == invoiceId && c.InitiatedAtUtc > payment.InitiatedAtUtc)
                .OrderBy(c => c.InitiatedAtUtc)
                .FirstOrDefault();

            if (linked is not null)
            {
                return Recovered(RecoveryEvidence.LinkedInvoicePayment, linked.UpdatedAtUtc);
            }
        }

        var retry = completedPayments
            .Where(c => c.Id != payment.Id
                        && c.CustomerId == payment.CustomerId
                        && c.Currency == payment.Currency
                        && c.Amount == payment.Amount
                        && c.InitiatedAtUtc > payment.InitiatedAtUtc
                        && c.InitiatedAtUtc <= payment.UpdatedAtUtc + RetryMatchWindow)
            .OrderBy(c => c.InitiatedAtUtc)
            .FirstOrDefault();

        return retry is null
            ? IssueResolution.StillOpen
            : Recovered(RecoveryEvidence.MatchingRetryPayment, retry.UpdatedAtUtc);
    }

    private static IssueResolution EvaluatePending(PaymentFact? payment) =>
        payment?.Status switch
        {
            PaymentStatus.Completed => Recovered(RecoveryEvidence.StatusChanged, payment.UpdatedAtUtc),
            // Başarısız olan bekleyen ödeme ayrı bir "failed_payment" kaybı olarak tespit edilir; iade edilen ise artık kayıp değildir.
            PaymentStatus.Failed or PaymentStatus.Refunded => IssueResolution.Void,
            _ => IssueResolution.StillOpen
        };

    private static IssueResolution EvaluateOverdue(InvoiceFact? invoice, DateOnly todayUtc)
    {
        if (invoice is null)
        {
            return IssueResolution.StillOpen;
        }

        return invoice.Status switch
        {
            InvoiceStatus.Paid => Recovered(RecoveryEvidence.StatusChanged, invoice.UpdatedAtUtc),
            InvoiceStatus.Cancelled or InvoiceStatus.Refunded or InvoiceStatus.Draft => IssueResolution.Void,
            InvoiceStatus.Sent or InvoiceStatus.PartiallyPaid when !IsOverdue(invoice, todayUtc) => IssueResolution.Void,
            _ => IssueResolution.StillOpen
        };
    }

    private static IssueResolution Recovered(RecoveryEvidence evidence, DateTime resolvedAtUtc) =>
        new(IssueOutcome.Recovered, evidence, resolvedAtUtc);
}

/// <summary>Bir kayıp için hangi adımın önerileceğini ve sistemin onaydan sonra çalıştırıp çalıştıramayacağını belirler.</summary>
public static class RecoveryPlanner
{
    public static (RecoveryActionType Type, bool Executable) Plan(RevenueIssue issue, InvoiceFact? invoice)
    {
        var type = issue.Type switch
        {
            IssueType.OverdueInvoice or IssueType.PendingPayment => RecoveryActionType.SendReminder,
            _ => issue.FailureCode switch
            {
                // Geçici olabilecek nedenler: aynı yöntemle yeniden denemek mantıklı.
                PaymentFailureCode.CardDeclined or PaymentFailureCode.InsufficientFunds => RecoveryActionType.RetryPayment,
                _ => RecoveryActionType.OfferAlternativeMethod
            }
        };

        // Sistem yalnızca PayPal faturası için hatırlatma gönderebilir; diğer adımlar tavsiyedir.
        var executable = type == RecoveryActionType.SendReminder
                         && invoice is { DataSource: DataSource.Paypal }
                         && invoice.Status is InvoiceStatus.Sent or InvoiceStatus.PartiallyPaid;

        return (type, executable);
    }
}
