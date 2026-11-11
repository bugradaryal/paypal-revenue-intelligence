using Microsoft.EntityFrameworkCore;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;
using PRI.Domain.Revenue.Interfaces;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    public async Task<Dictionary<string, Payment>> GetByExternalIdsAsync(
        DataSource source, IReadOnlyCollection<string> externalIds, CancellationToken cancellationToken)
    {
        var ids = externalIds.ToList();

        return await db.Payments
            .Where(p => p.DataSource == source && ids.Contains(p.ExternalId))
            .ToDictionaryAsync(p => p.ExternalId, cancellationToken);
    }

    public void Add(Payment payment) => db.Payments.Add(payment);
}
