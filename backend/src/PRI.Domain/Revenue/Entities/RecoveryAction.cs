using PRI.Domain.Common;
using PRI.Domain.Revenue.Enums;

namespace PRI.Domain.Revenue.Entities;

/// <summary>
/// Bir gelir kaybı için önerilen kurtarma adımı (API'de "suggestion"). Hiçbir adım kullanıcı onayı olmadan çalışmaz:
/// Proposed → Approved | Rejected; onaylanan ve çalıştırılabilir (Executable) adım arka planda Executed | Failed olur.
/// Tutarlar ve tür kural tabanlıdır; yalnızca gerekçe metni yapay zekâ tarafından yazılmış olabilir.
/// </summary>
public sealed class RecoveryAction : BaseEntity
{
    // EF Core için.
    private RecoveryAction()
    {
    }

    public Guid IssueId { get; private set; }

    public Guid? PaymentId { get; private set; }

    public Guid? InvoiceId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public RecoveryActionType Type { get; private set; }

    public string Rationale { get; private set; } = string.Empty;

    /// <summary>en | de | tr | es</summary>
    public string RationaleLanguage { get; private set; } = "en";

    public ContentSource RationaleSource { get; private set; }

    /// <summary>Gerekçeyi üreten gateway isteğinin kimliği (yalnızca RationaleSource = Ai).</summary>
    public Guid? GatewayRequestId { get; private set; }

    /// <summary>Riskteki tutar (üst sınır); garanti veya tahmin DEĞİLDİR.</summary>
    public decimal ExpectedRecovery { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    /// <summary>true ise onaylanınca sistem adımı kendisi çalıştırır (PayPal fatura hatırlatması); false ise yalnızca tavsiyedir.</summary>
    public bool Executable { get; private set; }

    public DataSource DataSource { get; private set; }

    public RecoveryActionStatus Status { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public DateTime? DecidedAtUtc { get; private set; }

    public DateTime? ExecutedAtUtc { get; private set; }

    /// <summary>Failed durumunda hata kodu (ör. paypal_unavailable).</summary>
    public string? ExecutionError { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public bool IsDecided => Status != RecoveryActionStatus.Proposed;

    public static RecoveryAction Propose(
        RevenueIssue issue,
        RecoveryActionType type,
        bool executable,
        string rationale,
        string rationaleLanguage,
        ContentSource rationaleSource,
        Guid? gatewayRequestId,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rationale);
        ArgumentException.ThrowIfNullOrWhiteSpace(rationaleLanguage);

        return new RecoveryAction
        {
            IssueId = issue.Id,
            PaymentId = issue.PaymentId,
            InvoiceId = issue.InvoiceId,
            CustomerId = issue.CustomerId,
            Type = type,
            Rationale = rationale.Trim(),
            RationaleLanguage = rationaleLanguage,
            RationaleSource = rationaleSource,
            GatewayRequestId = gatewayRequestId,
            ExpectedRecovery = issue.Amount,
            Currency = issue.Currency,
            Executable = executable,
            DataSource = issue.DataSource,
            Status = RecoveryActionStatus.Proposed,
            UpdatedAtUtc = nowUtc,
            CreatedAtUtc = nowUtc
        };
    }

    public void Approve(Guid userId, DateTime nowUtc)
    {
        EnsureProposed();
        Status = RecoveryActionStatus.Approved;
        DecidedByUserId = userId;
        DecidedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void Reject(Guid userId, DateTime nowUtc)
    {
        EnsureProposed();
        Status = RecoveryActionStatus.Rejected;
        DecidedByUserId = userId;
        DecidedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkExecuted(DateTime nowUtc)
    {
        EnsureApprovedExecutable();
        Status = RecoveryActionStatus.Executed;
        ExecutedAtUtc = nowUtc;
        ExecutionError = null;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkFailed(string errorCode, DateTime nowUtc)
    {
        EnsureApprovedExecutable();
        Status = RecoveryActionStatus.Failed;
        ExecutionError = errorCode.Length > 100 ? errorCode[..100] : errorCode;
        UpdatedAtUtc = nowUtc;
    }

    private void EnsureProposed()
    {
        if (Status != RecoveryActionStatus.Proposed)
        {
            throw new InvalidOperationException($"The suggestion is already {Status}.");
        }
    }

    private void EnsureApprovedExecutable()
    {
        if (Status != RecoveryActionStatus.Approved || !Executable)
        {
            throw new InvalidOperationException("Only an approved, executable suggestion can be executed.");
        }
    }
}
