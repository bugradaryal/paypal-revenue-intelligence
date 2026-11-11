using Microsoft.EntityFrameworkCore;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;
using PRI.Domain.Revenue.Interfaces;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public async Task<Dictionary<string, Customer>> GetByExternalIdsAsync(
        DataSource source, IReadOnlyCollection<string> externalIds, CancellationToken cancellationToken)
    {
        var ids = externalIds.ToList();

        return await db.Customers
            .Where(c => c.DataSource == source && ids.Contains(c.ExternalId))
            .ToDictionaryAsync(c => c.ExternalId, cancellationToken);
    }

    public void Add(Customer customer) => db.Customers.Add(customer);
}
