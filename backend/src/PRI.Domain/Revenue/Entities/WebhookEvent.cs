using PRI.Domain.Common;

namespace PRI.Domain.Revenue.Entities;

/// <summary>PayPal'dan gelen ve imzası doğrulanmış webhook olayı. ExternalEventId ile tekrar teslimler (idempotency) ayıklanır.</summary>
public sealed class WebhookEvent : BaseEntity
{
    // EF Core için.
    private WebhookEvent()
    {
    }

    public string ExternalEventId { get; private set; } = string.Empty;

    public string EventType { get; private set; } = string.Empty;

    /// <summary>Olayın ham JSON'u (jsonb).</summary>
    public string PayloadJson { get; private set; } = string.Empty;

    public DateTime ReceivedAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    /// <summary>İşlemin sonucu (ör. payment_updated, payment_not_synced, ignored).</summary>
    public string? Outcome { get; private set; }

    public static WebhookEvent Receive(string externalEventId, string eventType, string payloadJson, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        return new WebhookEvent
        {
            ExternalEventId = externalEventId.Trim(),
            EventType = eventType.Trim(),
            PayloadJson = payloadJson,
            ReceivedAtUtc = nowUtc,
            CreatedAtUtc = nowUtc
        };
    }

    public void MarkProcessed(string outcome, DateTime nowUtc)
    {
        Outcome = outcome;
        ProcessedAtUtc = nowUtc;
    }
}
