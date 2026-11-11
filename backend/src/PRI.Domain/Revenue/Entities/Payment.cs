using PRI.Domain.Common;
using PRI.Domain.Revenue.Enums;

namespace PRI.Domain.Revenue.Entities;

public sealed class Payment : BaseEntity
{
    private readonly List<PaymentEvent> _events = [];

    // EF Core için.
    private Payment()
    {
    }

    public Guid CustomerId { get; private set; }

    public Guid? InvoiceId { get; private set; }

    public DataSource DataSource { get; private set; }

    /// <summary>Kaynaktaki işlem kimliği (PayPal transaction id).</summary>
    public string ExternalId { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public PaymentStatus Status { get; private set; }

    /// <summary>Yalnızca Failed durumunda dolu.</summary>
    public PaymentFailureCode? FailureCode { get; private set; }

    /// <summary>Kaynakta işlemin başlatıldığı an.</summary>
    public DateTime InitiatedAtUtc { get; private set; }

    /// <summary>Kaynakta işlemin son güncellendiği an.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Bu kaydın bizim tarafımızdan en son senkronlandığı an.</summary>
    public DateTime SyncedAtUtc { get; private set; }

    public IReadOnlyCollection<PaymentEvent> Events => _events;

    public static Payment Create(
        Guid customerId,
        DataSource dataSource,
        string externalId,
        decimal amount,
        string currency,
        PaymentStatus status,
        PaymentFailureCode? failureCode,
        DateTime initiatedAtUtc,
        DateTime updatedAtUtc,
        Guid? invoiceId,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);

        var payment = new Payment
        {
            CustomerId = customerId,
            InvoiceId = invoiceId,
            DataSource = dataSource,
            ExternalId = externalId.Trim(),
            Amount = amount,
            Currency = currency.Trim().ToUpperInvariant(),
            Status = status,
            FailureCode = NormalizeFailureCode(status, failureCode),
            InitiatedAtUtc = initiatedAtUtc,
            UpdatedAtUtc = updatedAtUtc,
            SyncedAtUtc = nowUtc,
            CreatedAtUtc = nowUtc
        };

        payment._events.Add(PaymentEvent.Create(payment.Id, status, updatedAtUtc, nowUtc));

        return payment;
    }

    /// <summary>Kaynaktan gelen güncel durumu uygular. Durum veya hata kodu değiştiyse true döner.</summary>
    public bool ApplyProviderUpdate(
        PaymentStatus status,
        PaymentFailureCode? failureCode,
        DateTime providerUpdatedAtUtc,
        DateTime nowUtc)
    {
        SyncedAtUtc = nowUtc;

        // İade edilmiş ödeme geri "completed" olmaz. PayPal orijinal işlemi iadeden sonra da "başarılı" gösterebilir.
        if (Status == PaymentStatus.Refunded && status == PaymentStatus.Completed)
        {
            return false;
        }

        var newFailureCode = NormalizeFailureCode(status, failureCode);
        var statusChanged = status != Status;

        if (!statusChanged && newFailureCode == FailureCode)
        {
            return false;
        }

        if (statusChanged)
        {
            _events.Add(PaymentEvent.Create(Id, status, providerUpdatedAtUtc, nowUtc));
        }

        Status = status;
        FailureCode = newFailureCode;
        UpdatedAtUtc = providerUpdatedAtUtc;

        return true;
    }

    public void LinkInvoice(Guid invoiceId) => InvoiceId = invoiceId;

    // Hata kodu yalnızca Failed ödemelerde anlamlıdır; neden bilinmiyorsa Other kullanılır.
    private static PaymentFailureCode? NormalizeFailureCode(PaymentStatus status, PaymentFailureCode? failureCode) =>
        status == PaymentStatus.Failed ? failureCode ?? PaymentFailureCode.Other : null;
}
