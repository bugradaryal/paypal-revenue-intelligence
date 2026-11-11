using PRI.Application.Revenue.DTOs;
using PRI.Domain.Revenue.Enums;
using PRI.Shared.Models;

namespace PRI.Application.Revenue.Interfaces;

public sealed record IssueSyncResult(int Created, int Recovered, int Voided);

public interface IRevenueIssueSync
{
    /// <summary>Kayıpları kurallarla tespit eder, açık olanların çözüm durumunu günceller ve kaydeder.</summary>
    Task<IssueSyncResult> RefreshAsync(CancellationToken cancellationToken);
}

public interface ISuggestionService
{
    /// <summary>Kayıpları yeniler, önerisi olmayan açık kayıplar için (en fazla 10) öneri üretir.</summary>
    Task<GenerateSuggestionsResultDto> GenerateAsync(string language, CancellationToken cancellationToken);

    /// <summary>Öneriyi onaylar veya reddeder. Çalıştırılabilir bir öneri onaylanınca arka planda çalıştırılır.</summary>
    Task<SuggestionDto> DecideAsync(Guid id, string decision, Guid userId, CancellationToken cancellationToken);
}

public interface IRecoveryActionQueue
{
    ValueTask EnqueueAsync(Guid actionId);
}

public interface IRecoveryActionExecutor
{
    /// <summary>Onaylı ve çalıştırılabilir öneriyi uygular; sonuç Executed veya Failed olarak kaydedilir. Başka durumdakileri yok sayar.</summary>
    Task ExecuteAsync(Guid actionId, CancellationToken cancellationToken);
}

/// <summary>Üretilen metin ve nasıl üretildiği (yapay zekâ veya şablon).</summary>
public sealed record WrittenText(string Text, ContentSource Source, Guid? GatewayRequestId);

public interface IRationaleWriter
{
    Task<WrittenText> WriteRationaleAsync(RationaleFacts facts, string language, CancellationToken cancellationToken);

    Task<WrittenText> WriteInsightCommentaryAsync(InsightFacts facts, string language, CancellationToken cancellationToken);
}

public sealed record RationaleFacts(
    RecoveryActionType ActionType,
    IssueType IssueType,
    decimal Amount,
    string Currency,
    PaymentFailureCode? FailureCode,
    int DaysOutstanding);

public sealed record FailureReasonFact(PaymentFailureCode Code, int Count, decimal Amount);

/// <summary>Bir dönemin USD rakamları (ham decimal).</summary>
public sealed record InsightFacts(
    int PeriodDays,
    decimal Revenue,
    decimal Failed,
    decimal Pending,
    int PaymentCount,
    int FailedCount,
    decimal Lost,
    decimal Recovered,
    int OverdueCount,
    decimal OverdueAmount,
    IReadOnlyList<FailureReasonFact> TopReasons,
    decimal PreviousRevenue,
    decimal PreviousFailed);

public interface IInsightService
{
    Task<InsightDto> GenerateAsync(int days, string language, CancellationToken cancellationToken);

    Task<PagedResult<InsightDto>> ListAsync(InsightListQuery query, CancellationToken cancellationToken);
}
