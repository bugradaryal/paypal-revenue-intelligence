using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Repositories;

public sealed class UsageRecordRepository(AppDbContext db) : IUsageRecordRepository
{
    public void Add(UsageRecord record) => db.UsageRecords.Add(record);
}
