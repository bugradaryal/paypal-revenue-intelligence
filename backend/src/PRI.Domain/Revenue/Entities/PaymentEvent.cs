using PRI.Domain.Common;
using PRI.Domain.Revenue.Enums;

namespace PRI.Domain.Revenue.Entities;

/// <summary>Bir ödemenin durum geçmişindeki tek kayıt.</summary>
public sealed class PaymentEvent : BaseEntity
{
    // EF Core için.
    private PaymentEvent()
    {
    }

    public Guid PaymentId { get; private set; }

    public PaymentStatus Status { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    internal static PaymentEvent Create(Guid paymentId, PaymentStatus status, DateTime occurredAtUtc, DateTime nowUtc) =>
        new()
        {
            PaymentId = paymentId,
            Status = status,
            OccurredAtUtc = occurredAtUtc,
            CreatedAtUtc = nowUtc
        };
}
