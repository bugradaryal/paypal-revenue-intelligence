using PRI.Application.Common.DTOs;
using PRI.Domain.Revenue.Enums;

namespace PRI.Application.Revenue.DTOs;

public sealed record InvoiceDto(
    Guid Id,
    string? InvoiceNumber,
    Guid? CustomerId,
    string? CustomerName,
    MoneyDto Amount,
    InvoiceStatus Status,
    bool IsOverdue,
    DateOnly? DueDate,
    DateTime? IssuedAt,
    DateTime UpdatedAt,
    string? PaypalInvoiceId,
    DataSource DataSource);

public sealed class InvoiceListQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string[] Status { get; init; } = [];

    public string? Search { get; init; }

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    /// <summary>issuedAt | dueDate | amount</summary>
    public string SortBy { get; init; } = "issuedAt";

    public string SortDir { get; init; } = "desc";
}

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string? Email,
    DataSource DataSource,
    int PaymentCount,
    int FailedPaymentCount,
    DateTime CreatedAt);

public sealed class CustomerListQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    /// <summary>createdAt | name</summary>
    public string SortBy { get; init; } = "createdAt";

    public string SortDir { get; init; } = "desc";
}
