using PRI.Domain.Revenue.Enums;

namespace PRI.Domain.Revenue;

/// <summary>Tespit ve çözüm kurallarının okuduğu, ödemenin salt okunur özeti.</summary>
public sealed record PaymentFact(
    Guid Id,
    Guid CustomerId,
    Guid? InvoiceId,
    DataSource DataSource,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    PaymentFailureCode? FailureCode,
    DateTime InitiatedAtUtc,
    DateTime UpdatedAtUtc);

/// <summary>Faturanın salt okunur özeti.</summary>
public sealed record InvoiceFact(
    Guid Id,
    Guid? CustomerId,
    DataSource DataSource,
    string ExternalId,
    decimal Amount,
    string Currency,
    InvoiceStatus Status,
    DateOnly? DueDate,
    DateTime UpdatedAtUtc);
