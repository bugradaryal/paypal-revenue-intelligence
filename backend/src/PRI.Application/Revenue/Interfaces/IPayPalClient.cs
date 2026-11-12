namespace PRI.Application.Revenue.Interfaces;

/// <summary>PayPal Transaction Search sonucundaki tek işlem (yalnızca ihtiyacımız olan alanlar).</summary>
public sealed record PayPalTransaction(
    string TransactionId,
    string StatusCode,
    string? EventCode,
    decimal Amount,
    string Currency,
    DateTime InitiatedAtUtc,
    DateTime UpdatedAtUtc,
    string? PayerAccountId,
    string? PayerEmail,
    string? PayerName,
    string? InvoiceNumber,
    string? ReferenceTransactionId,
    string? ReferenceIdType);

/// <param name="SkippedCount">Zorunlu alanları eksik olduğu için okunamayan kayıt sayısı.</param>
public sealed record PayPalTransactionPage(
    IReadOnlyList<PayPalTransaction> Items,
    int Page,
    int TotalPages,
    int SkippedCount);

public sealed record PayPalInvoice(
    string InvoiceId,
    string? InvoiceNumber,
    string StatusCode,
    decimal Amount,
    string Currency,
    DateOnly? DueDate,
    DateTime? IssuedAtUtc,
    DateTime? UpdatedAtUtc,
    string? RecipientEmail,
    string? RecipientName);

public sealed record PayPalInvoicePage(
    IReadOnlyList<PayPalInvoice> Items,
    int Page,
    int TotalPages,
    int SkippedCount);

/// <summary>PayPal Sandbox API'si için sağlayıcıdan bağımsız arayüz. Gerçek HTTP çağrıları Infrastructure'dadır.</summary>
public interface IPayPalClient
{
    bool IsConfigured { get; }

    bool HasWebhookId { get; }

    string Environment { get; }

    /// <summary>Checks OAuth credentials and read access to PayPal Transaction Search without writing data.</summary>
    Task TestConnectionAsync(CancellationToken cancellationToken);

    /// <summary>Transaction Search. PayPal tek istekte en fazla 31 günlük aralığa izin verir.</summary>
    Task<PayPalTransactionPage> SearchTransactionsAsync(
        DateTime fromUtc, DateTime toUtc, int page, CancellationToken cancellationToken);

    Task<PayPalInvoicePage> ListInvoicesAsync(int page, CancellationToken cancellationToken);

    /// <summary>PayPal'ın standart fatura hatırlatmasını alıcıya gönderir (POST /v2/invoicing/invoices/{id}/remind).</summary>
    Task SendInvoiceReminderAsync(string invoiceId, CancellationToken cancellationToken);
}
