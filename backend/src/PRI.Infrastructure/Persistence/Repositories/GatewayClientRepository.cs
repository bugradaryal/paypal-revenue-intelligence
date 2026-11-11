using Microsoft.EntityFrameworkCore;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Repositories;

public sealed class GatewayClientRepository(AppDbContext db) : IGatewayClientRepository
{
    public Task<GatewayClient?> GetFirstPartyAsync(CancellationToken cancellationToken) =>
        db.GatewayClients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.IsFirstParty && c.IsActive, cancellationToken);

    public Task<GatewayClient?> GetActiveByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken) =>
        db.GatewayClients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.IsActive && !c.IsFirstParty && c.ApiKeyHash == apiKeyHash, cancellationToken);

    public Task<GatewayClient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.GatewayClients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<GatewayClient>> ListAsync(CancellationToken cancellationToken) =>
        await db.GatewayClients.AsNoTracking()
            .OrderByDescending(c => c.IsFirstParty).ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken) =>
        db.GatewayClients.AnyAsync(c => c.Name == name, cancellationToken);

    public void Add(GatewayClient client) => db.GatewayClients.Add(client);
}
