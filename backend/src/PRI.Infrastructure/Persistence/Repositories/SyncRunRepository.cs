using Microsoft.EntityFrameworkCore;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;
using PRI.Domain.Revenue.Interfaces;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Repositories;

public sealed class SyncRunRepository(AppDbContext db) : ISyncRunRepository
{
    public Task<SyncRun?> GetLatestAsync(CancellationToken cancellationToken) =>
        db.SyncRuns.AsNoTracking().OrderByDescending(s => s.StartedAtUtc).FirstOrDefaultAsync(cancellationToken);

    public Task<SyncRun?> GetLatestSuccessfulAsync(CancellationToken cancellationToken) =>
        db.SyncRuns.AsNoTracking()
            .Where(s => s.Status == SyncRunStatus.Succeeded)
            .OrderByDescending(s => s.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> HasRunningSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken) =>
        db.SyncRuns.AnyAsync(s => s.Status == SyncRunStatus.Running && s.StartedAtUtc >= sinceUtc, cancellationToken);

    public void Add(SyncRun run) => db.SyncRuns.Add(run);
}
