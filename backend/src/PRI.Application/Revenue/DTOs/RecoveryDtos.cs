using PRI.Application.Common.DTOs;
using PRI.Domain.Revenue.Enums;

namespace PRI.Application.Revenue.DTOs;

/// <param name="LostAmount">Aralıkta BAŞLAYAN (problemSince) kayıpların toplamı (void olanlar hariç, yalnızca USD).</param>
/// <param name="RecoveredAmount">Bu kayıplardan kurtarılanların toplamı. Kural tabanlı eşlemedir; kurtarmanın yapay zekâ veya bir öneri sayesinde olduğunu iddia etmez.</param>
/// <param name="RecoveryRate">RecoveredAmount / LostAmount (0..1); kayıp yoksa 0.</param>
/// <param name="OpenSuggestionCount">Şu an onay bekleyen (proposed) öneri sayısı; aralıktan bağımsızdır.</param>
public sealed record RecoverySummaryDto(
    DateRangeDto Range,
    MoneyDto LostAmount,
    MoneyDto RecoveredAmount,
    double RecoveryRate,
    int OpenSuggestionCount);

public sealed record SuggestionDto(
    Guid Id,
    Guid IssueId,
    IssueType IssueType,
    Guid? PaymentId,
    Guid? InvoiceId,
    string? CustomerName,
    RecoveryActionType Type,
    string Rationale,
    string RationaleLanguage,
    ContentSource RationaleSource,
    MoneyDto ExpectedRecovery,
    RecoveryActionStatus Status,
    bool Executable,
    string? ExecutionError,
    DataSource DataSource,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed class SuggestionListQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Tekrarlanabilir: status=proposed&amp;status=approved</summary>
    public string[] Status { get; init; } = [];

    public Guid? PaymentId { get; init; }

    /// <summary>createdAt | expectedRecovery</summary>
    public string SortBy { get; init; } = "createdAt";

    public string SortDir { get; init; } = "desc";
}

/// <param name="Decision">"approve" | "reject"</param>
public sealed record SuggestionDecisionRequest(string Decision);

public sealed record GenerateSuggestionsResultDto(int CreatedCount);

public sealed record IssueDto(
    Guid Id,
    IssueType Type,
    IssueStatus Status,
    Guid? PaymentId,
    Guid? InvoiceId,
    Guid? CustomerId,
    string? CustomerName,
    MoneyDto Amount,
    MoneyDto RecoveredAmount,
    PaymentFailureCode? FailureCode,
    DataSource DataSource,
    DateTime ProblemSince,
    DateTime DetectedAt,
    DateTime? ResolvedAt,
    RecoveryEvidence? Evidence);

public sealed class IssueListQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string[] Status { get; init; } = [];

    public string[] Type { get; init; } = [];

    /// <summary>problemSince | amount</summary>
    public string SortBy { get; init; } = "problemSince";

    public string SortDir { get; init; } = "desc";
}

// ---------- Insights ----------

public sealed record FailureReasonDto(PaymentFailureCode Code, int Count, MoneyDto Amount);

/// <param name="RevenueChangePercent">Önceki döneme göre değişim (%); önceki dönem 0 ise null.</param>
public sealed record PreviousPeriodDto(
    MoneyDto Revenue,
    MoneyDto FailedAmount,
    double? RevenueChangePercent,
    double? FailedAmountChangePercent);

/// <summary>Tüm değerler C# ile hesaplanır ve yalnızca USD'dir (diğer para birimleri hariç).</summary>
/// <param name="FailureRate">Başarısız ödeme sayısı / toplam ödeme sayısı (0..1).</param>
public sealed record InsightMetricsDto(
    MoneyDto Revenue,
    MoneyDto FailedAmount,
    MoneyDto PendingAmount,
    int PaymentCount,
    int FailedCount,
    double FailureRate,
    MoneyDto LostAmount,
    MoneyDto RecoveredAmount,
    double RecoveryRate,
    int OverdueInvoiceCount,
    MoneyDto OverdueInvoiceAmount,
    IReadOnlyList<FailureReasonDto> TopFailureReasons,
    PreviousPeriodDto Previous);

/// <param name="Commentary">Rakamlar üzerine kısa yorum. CommentarySource "ai" ise yapay zekâ yazmıştır ve yalnızca metrics'teki rakamları kullanabilir (doğrulanır); "rule" ise şablondur.</param>
/// <param name="GatewayRequestId">Yorumu üreten gateway isteği (yalnızca "ai").</param>
public sealed record InsightDto(
    Guid Id,
    DateTime PeriodFrom,
    DateTime PeriodTo,
    InsightMetricsDto Metrics,
    string Commentary,
    string CommentaryLanguage,
    ContentSource CommentarySource,
    Guid? GatewayRequestId,
    DateTime CreatedAt);

public sealed class InsightListQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public sealed class GenerateInsightQuery
{
    /// <summary>Geriye dönük kaç gün (1-90).</summary>
    public int Days { get; init; } = 30;
}
