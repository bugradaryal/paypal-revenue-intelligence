using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Repositories;

public sealed class CacheLookupRepository(AppDbContext db) : ICacheLookupRepository
{
    public void Add(CacheLookup lookup) => db.CacheLookups.Add(lookup);
}
