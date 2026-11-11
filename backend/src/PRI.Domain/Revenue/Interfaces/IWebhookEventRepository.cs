using PRI.Domain.Revenue.Entities;

namespace PRI.Domain.Revenue.Interfaces;

public interface IWebhookEventRepository
{
    Task<bool> ExistsAsync(string externalEventId, CancellationToken cancellationToken);

    void Add(WebhookEvent webhookEvent);
}
