using Microsoft.EntityFrameworkCore;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Repositories;

public sealed class RoutingRuleRepository(AppDbContext db) : IRoutingRuleRepository
{
    public Task<RoutingRule?> GetByTaskTypeAsync(string taskType, CancellationToken cancellationToken) =>
        db.RoutingRules.AsNoTracking().FirstOrDefaultAsync(r => r.TaskType == taskType, cancellationToken);

    public async Task<IReadOnlyList<RoutingRule>> ListAsync(CancellationToken cancellationToken) =>
        await db.RoutingRules.AsNoTracking().OrderBy(r => r.TaskType).ToListAsync(cancellationToken);

    public Task<RoutingRule?> GetTrackedByTaskTypeAsync(string taskType, CancellationToken cancellationToken) =>
        db.RoutingRules.FirstOrDefaultAsync(r => r.TaskType == taskType, cancellationToken);

    public void Add(RoutingRule rule) => db.RoutingRules.Add(rule);
}
