using PRI.Application.Common.DTOs;
using PRI.Domain.Revenue.Enums;

namespace PRI.Application.Revenue.DTOs;

public sealed record CashFlowOutlookDto(
    DateOnly AsOfDateUtc,
    MoneyDto OpenReceivables,
    int OpenInvoiceCount,
    MoneyDto OverdueReceivables,
    int OverdueInvoiceCount,
    MoneyDto DueWithin7Days,
    int DueWithin7DaysCount,
    MoneyDto DueWithin30Days,
    int DueWithin30DaysCount,
    MoneyDto DueWithin90Days,
    int DueWithin90DaysCount,
    MoneyDto UnscheduledReceivables,
    int UnscheduledInvoiceCount,
    IReadOnlyList<OverdueAgingBucketDto> OverdueAging,
    IReadOnlyList<CashFlowWeekDto> WeeklySchedule,
    IReadOnlyList<CurrencyExposureDto> ForeignCurrencyExposure,
    IReadOnlyList<CashFlowInvoiceDto> PriorityInvoices);

public sealed record CashFlowInvoiceFact(
    Guid Id,
    string? InvoiceNumber,
    string? CustomerName,
    decimal Amount,
    decimal PaidAmount,
    string Currency,
    InvoiceStatus Status,
    DateOnly? DueDate,
    DataSource DataSource);

public sealed record OverdueAgingBucketDto(string Key, MoneyDto Amount, int InvoiceCount);

public sealed record CashFlowWeekDto(DateOnly From, DateOnly ToExclusive, MoneyDto Amount, int InvoiceCount);

public sealed record CurrencyExposureDto(string Currency, MoneyDto OpenAmount, int InvoiceCount);

public sealed record CashFlowInvoiceDto(
    Guid Id,
    string? InvoiceNumber,
    string? CustomerName,
    MoneyDto BalanceDue,
    InvoiceStatus Status,
    DateOnly? DueDate,
    int? DaysOverdue,
    DataSource DataSource);
