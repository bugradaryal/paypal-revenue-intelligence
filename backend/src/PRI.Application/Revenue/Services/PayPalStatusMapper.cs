using PRI.Domain.Revenue.Enums;

namespace PRI.Application.Revenue.Services;

/// <summary>PayPal durum kodlarını kendi durumlarımıza eşler. Tanınmayan kod uydurulmaz, false döner (kayıt atlanır ve sayılır).</summary>
public static class PayPalStatusMapper
{
    // PayPal Transaction Search: S = başarılı, P = beklemede, D = reddedildi, V = iade/ters kayıt.
    public static bool TryMapTransactionStatus(string? code, out PaymentStatus status)
    {
        switch (code?.Trim().ToUpperInvariant())
        {
            case "S":
                status = PaymentStatus.Completed;
                return true;
            case "P":
                status = PaymentStatus.Pending;
                return true;
            case "D":
                status = PaymentStatus.Failed;
                return true;
            case "V":
                status = PaymentStatus.Refunded;
                return true;
            default:
                status = default;
                return false;
        }
    }

    public static bool TryMapInvoiceStatus(string? providerStatus, out InvoiceStatus status)
    {
        switch (providerStatus?.Trim().ToUpperInvariant())
        {
            case "DRAFT":
            case "SCHEDULED":
                status = InvoiceStatus.Draft;
                return true;
            case "SENT":
            case "UNPAID":
                status = InvoiceStatus.Sent;
                return true;
            case "PAYMENT_PENDING":
                status = InvoiceStatus.PaymentPending;
                return true;
            case "PARTIALLY_PAID":
                status = InvoiceStatus.PartiallyPaid;
                return true;
            case "PAID":
            case "MARKED_AS_PAID":
                status = InvoiceStatus.Paid;
                return true;
            case "CANCELLED":
                status = InvoiceStatus.Cancelled;
                return true;
            case "REFUNDED":
            case "PARTIALLY_REFUNDED":
            case "MARKED_AS_REFUNDED":
                status = InvoiceStatus.Refunded;
                return true;
            default:
                status = default;
                return false;
        }
    }
}
