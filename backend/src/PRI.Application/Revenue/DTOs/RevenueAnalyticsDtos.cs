using PRI.Application.Common.DTOs;
using PRI.Domain.Revenue.Enums;

namespace PRI.Application.Revenue.DTOs;

public sealed record RevenueAnalyticsDto(
    DateRangeDto Range,
    DateOnly AsOfDateUtc,
    RevenueLeakageDto Leakage,
    IReadOnlyList<RevenueAnalyticsDayDto> Daily,
    IReadOnlyList<FailureReasonAnalysisDto> FailureReasons,
    IReadOnlyList<FailureTrendPointDto> FailureTrend,
    IReadOnlyList<RevenueCohortDto> Cohorts,
    RevenueAnomalyReportDto Anomalies,
    int ExcludedPaymentCount);

public sealed record RevenueLeakageDto(
    MoneyDto TransactionActivity,
    MoneyDto CompletedAmount,
    int CompletedCount,
    MoneyDto FailedAttemptAmount,
    int FailedCount,
    MoneyDto PendingAmount,
    int PendingCount,
    MoneyDto RefundedAmount,
    int RefundedCount,
    MoneyDto CurrentOpenReceivables,
    int CurrentOpenInvoiceCount,
    MoneyDto CurrentOverdueReceivables,
    int CurrentOverdueInvoiceCount,
    bool CancellationTrackingAvailable);

public sealed record RevenueAnalyticsDayDto(
    DateOnly Date,
    MoneyDto CompletedAmount,
    MoneyDto FailedAmount,
    MoneyDto PendingAmount,
    MoneyDto RefundedAmount);

public sealed record FailureReasonAnalysisDto(string Code, int Count, MoneyDto AmountUsd);

public sealed record FailureTrendPointDto(DateOnly Date, string Code, int Count);

public sealed record RevenueCohortDto(
    DateOnly CohortMonth,
    int CohortSize,
    IReadOnlyList<RevenueCohortPeriodDto> Periods);

public sealed record RevenueCohortPeriodDto(
    int AgeInMonths,
    int ActiveCustomers,
    double RetentionRate,
    MoneyDto RevenuePerCustomer);

public sealed record RevenueAnomalyReportDto(
    int WindowDays,
    FailureRateSpikeDto? FailureRateSpike,
    IReadOnlyList<RevenueDropAnomalyDto> RevenueDrops);

public sealed record FailureRateSpikeDto(
    double CurrentRate,
    double BaselineRate,
    int CurrentFailedCount,
    int CurrentPaymentCount,
    int BaselinePaymentCount);

public sealed record RevenueDropAnomalyDto(
    string Currency,
    MoneyDto CurrentRevenue,
    MoneyDto BaselineRevenue,
    double ChangePercent,
    int CurrentPaymentCount,
    int BaselinePaymentCount);

public sealed record RevenueAnalyticsPaymentFact(
    Guid CustomerId,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    PaymentFailureCode? FailureCode,
    DateTime InitiatedAtUtc);

public sealed record RevenueAnalyticsFirstPayment(Guid CustomerId, DateTime FirstPaymentAtUtc);
