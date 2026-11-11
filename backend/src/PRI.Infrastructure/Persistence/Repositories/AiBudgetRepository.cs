using Microsoft.EntityFrameworkCore;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Repositories;

public sealed class AiBudgetRepository(AppDbContext db) : IAiBudgetRepository
{
    public Task<AiBudget?> GetAsync(CancellationToken cancellationToken) =>
        db.AiBudgets.OrderBy(b => b.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);

    public void Add(AiBudget budget) => db.AiBudgets.Add(budget);
}
