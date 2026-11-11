using PRI.Domain.Common;
using PRI.Domain.Revenue.Enums;

namespace PRI.Domain.Revenue.Entities;

/// <summary>
/// Tespit edilmiş tek bir gelir kaybı (başarısız ödeme, çok uzun süredir bekleyen ödeme veya vadesi geçmiş fatura).
/// Tespit ve çözüm tamamen kural tabanlıdır (<see cref="IssueRules"/>); LLM'in hiçbir rolü yoktur.
/// Aynı (tür, ödeme, fatura) için tek kayıt tutulur.
/// </summary>
public sealed class RevenueIssue : BaseEntity
{
    // EF Core için.
    private RevenueIssue()
    {
    }

    public IssueType Type { get; private set; }

    public IssueStatus Status { get; private set; }

    public Guid? CustomerId { get; private set; }

    public Guid? PaymentId { get; private set; }

    public Guid? InvoiceId { get; private set; }

    /// <summary>Riskteki tutar: ödeme tutarı veya fatura tutarı (kısmi ödeme bilgisi kaynaktan gelmediği için fatura tutarının tamamı).</summary>
    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public PaymentFailureCode? FailureCode { get; private set; }

    public DataSource DataSource { get; private set; }

    /// <summary>Kaybın gerçekte başladığı an (başarısızlık anı, bekleme başlangıcı veya vade günü sonu).</summary>
    public DateTime ProblemSinceUtc { get; private set; }

    /// <summary>Kurtarılan tutar; yalnızca Recovered durumunda dolu (= Amount).</summary>
    public decimal RecoveredAmount { get; private set; }

    public DateTime? ResolvedAtUtc { get; private set; }

    public RecoveryEvidence? Evidence { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static RevenueIssue Create(
        IssueType type,
        Guid? customerId,
        Guid? paymentId,
        Guid? invoiceId,
        decimal amount,
        string currency,
        PaymentFailureCode? failureCode,
        DataSource dataSource,
        DateTime problemSinceUtc,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);

        if (paymentId is null && invoiceId is null)
        {
            throw new ArgumentException("An issue must reference a payment or an invoice.");
        }

        return new RevenueIssue
        {
            Type = type,
            Status = IssueStatus.Open,
            CustomerId = customerId,
            PaymentId = paymentId,
            InvoiceId = invoiceId,
            Amount = amount,
            Currency = currency.Trim().ToUpperInvariant(),
            FailureCode = failureCode,
            DataSource = dataSource,
            ProblemSinceUtc = problemSinceUtc,
            UpdatedAtUtc = nowUtc,
            CreatedAtUtc = nowUtc
        };
    }

    public void MarkRecovered(RecoveryEvidence evidence, DateTime resolvedAtUtc, DateTime nowUtc)
    {
        if (Status != IssueStatus.Open)
        {
            return;
        }

        Status = IssueStatus.Recovered;
        RecoveredAmount = Amount;
        Evidence = evidence;
        ResolvedAtUtc = resolvedAtUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkVoid(DateTime nowUtc)
    {
        if (Status != IssueStatus.Open)
        {
            return;
        }

        Status = IssueStatus.Void;
        ResolvedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public int DaysOutstanding(DateTime nowUtc) => Math.Max((int)(nowUtc - ProblemSinceUtc).TotalDays, 0);
}
