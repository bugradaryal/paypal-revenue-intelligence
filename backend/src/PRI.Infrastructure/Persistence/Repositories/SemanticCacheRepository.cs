using Microsoft.EntityFrameworkCore;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Repositories;

public sealed class SemanticCacheRepository(AppDbContext db) : ISemanticCacheRepository
{
    public Task<SemanticCacheEntry?> FindExactAsync(
        Guid clientId, string contextHash, string queryHash, DateTime nowUtc, CancellationToken cancellationToken) =>
        db.SemanticCacheEntries
            .Where(e => e.ClientId == clientId && e.ContextHash == contextHash && e.QueryHash == queryHash && e.ExpiresAtUtc > nowUtc)
            .OrderByDescending(e => e.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CacheCandidate>> ListCandidatesAsync(
        Guid clientId, string contextHash, DateTime nowUtc, int maxCandidates, CancellationToken cancellationToken)
    {
        var rows = await db.SemanticCacheEntries.AsNoTracking()
            .Where(e => e.ClientId == clientId && e.ContextHash == contextHash && e.ExpiresAtUtc > nowUtc && e.Embedding != null)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(maxCandidates)
            .Select(e => new { e.Id, e.Embedding, e.NumbersFingerprint })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new CacheCandidate(r.Id, r.Embedding!, r.NumbersFingerprint)).ToList();
    }

    public Task<SemanticCacheEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.SemanticCacheEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Add(SemanticCacheEntry entry) => db.SemanticCacheEntries.Add(entry);

    public Task<int> DeleteAllAsync(CancellationToken cancellationToken) =>
        db.SemanticCacheEntries.ExecuteDeleteAsync(cancellationToken);
}
