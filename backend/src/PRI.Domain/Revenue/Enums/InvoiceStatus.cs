namespace PRI.Domain.Revenue.Enums;

/// <summary>PayPal fatura durumlarının sadeleştirilmiş hali. "Vadesi geçmiş" saklanmaz, Invoice.IsOverdue ile hesaplanır.</summary>
public enum InvoiceStatus
{
    Draft = 1,
    Sent = 2,
    PaymentPending = 3,
    PartiallyPaid = 4,
    Paid = 5,
    Cancelled = 6,
    Refunded = 7
}
