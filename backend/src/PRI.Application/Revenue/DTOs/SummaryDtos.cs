using PRI.Application.Common.DTOs;

namespace PRI.Application.Revenue.DTOs;

public sealed record RevenueSeriesPointDto(DateTime Date, MoneyDto Revenue, MoneyDto Recovered);

public sealed record RevenueSummaryDto(
    DateRangeDto Range,
    MoneyDto TotalRevenue,
    MoneyDto FailedAmount,
    MoneyDto PendingAmount,
    MoneyDto RecoveredRevenue,
    int PaymentCount,
    int FailedCount,
    int PendingCount,
    int ExcludedPaymentCount,
    IReadOnlyList<RevenueSeriesPointDto> Series);
