using PRI.Domain.Common;
using PRI.Domain.Revenue.Enums;

namespace PRI.Domain.Revenue.Entities;

public sealed class Invoice : BaseEntity
{
    // EF Core için.
    private Invoice()
    {
    }

    public DataSource DataSource { get; private set; }

    /// <summary>Kaynaktaki fatura kimliği (PayPal invoice id).</summary>
    public string ExternalId { get; private set; } = string.Empty;

    /// <summary>Faturadaki numara; PayPal işlemleri bu numarayla faturaya bağlanır.</summary>
    public string? InvoiceNumber { get; private set; }

    public Guid? CustomerId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public InvoiceStatus Status { get; private set; }

    /// <summary>Kaynağın ham durum değeri (denetim için saklanır).</summary>
    public string ProviderStatus { get; private set; } = string.Empty;

    public DateOnly? DueDate { get; private set; }

    public DateTime? IssuedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime SyncedAtUtc { get; private set; }

    public static Invoice Create(
        DataSource dataSource,
        string externalId,
        string? invoiceNumber,
        Guid? customerId,
        decimal amount,
        string currency,
        InvoiceStatus status,
        string providerStatus,
        DateOnly? dueDate,
        DateTime? issuedAtUtc,
        DateTime updatedAtUtc,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        return new Invoice
        {
            DataSource = dataSource,
            ExternalId = externalId.Trim(),
            InvoiceNumber = string.IsNullOrWhiteSpace(invoiceNumber) ? null : invoiceNumber.Trim(),
            CustomerId = customerId,
            Amount = amount,
            Currency = currency.Trim().ToUpperInvariant(),
            Status = status,
            ProviderStatus = providerStatus,
            DueDate = dueDate,
            IssuedAtUtc = issuedAtUtc,
            UpdatedAtUtc = updatedAtUtc,
            SyncedAtUtc = nowUtc,
            CreatedAtUtc = nowUtc
        };
    }

    public void ApplyProviderUpdate(
        Guid? customerId,
        decimal amount,
        InvoiceStatus status,
        string providerStatus,
        DateOnly? dueDate,
        DateTime updatedAtUtc,
        DateTime nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        CustomerId = customerId ?? CustomerId;
        Amount = amount;
        Status = status;
        ProviderStatus = providerStatus;
        DueDate = dueDate;
        UpdatedAtUtc = updatedAtUtc;
        SyncedAtUtc = nowUtc;
    }

    /// <summary>Gönderilmiş (ya da kısmen ödenmiş) ve vade tarihi geçmiş fatura vadesi geçmiş sayılır.</summary>
    public bool IsOverdue(DateOnly todayUtc) =>
        DueDate is { } dueDate
        && dueDate < todayUtc
        && (Status == InvoiceStatus.Sent || Status == InvoiceStatus.PartiallyPaid);
}
