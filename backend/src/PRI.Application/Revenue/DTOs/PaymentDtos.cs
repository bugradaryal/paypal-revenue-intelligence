using PRI.Application.Common.DTOs;
using PRI.Domain.Revenue.Enums;

namespace PRI.Application.Revenue.DTOs;

public sealed record PaymentDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    MoneyDto Amount,
    PaymentStatus Status,
    PaymentFailureCode? FailureCode,
    string? PaypalTransactionId,
    DataSource DataSource,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record PaymentStatusEventDto(PaymentStatus Status, DateTime OccurredAt);

public sealed record PaymentDetailDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    MoneyDto Amount,
    PaymentStatus Status,
    PaymentFailureCode? FailureCode,
    string? PaypalTransactionId,
    DataSource DataSource,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<PaymentStatusEventDto> StatusHistory);

public sealed class PaymentListQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Tekrarlanabilir: status=failed&amp;status=pending</summary>
    public string[] Status { get; init; } = [];

    public string? Search { get; init; }

    public Guid? CustomerId { get; init; }

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    /// <summary>createdAt | amount</summary>
    public string SortBy { get; init; } = "createdAt";

    /// <summary>asc | desc</summary>
    public string SortDir { get; init; } = "desc";
}
