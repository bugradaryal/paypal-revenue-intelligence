using Microsoft.EntityFrameworkCore;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Interfaces;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Repositories;

public sealed class WebhookEventRepository(AppDbContext db) : IWebhookEventRepository
{
    public Task<bool> ExistsAsync(string externalEventId, CancellationToken cancellationToken) =>
        db.WebhookEvents.AnyAsync(e => e.ExternalEventId == externalEventId, cancellationToken);

    public void Add(WebhookEvent webhookEvent) => db.WebhookEvents.Add(webhookEvent);
}
