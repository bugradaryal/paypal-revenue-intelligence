using Microsoft.EntityFrameworkCore;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Repositories;

public sealed class ProviderModelRepository(AppDbContext db) : IProviderModelRepository
{
    public async Task<IReadOnlyList<ProviderModel>> ListAsync(CancellationToken cancellationToken) =>
        await db.ProviderModels.AsNoTracking()
            .OrderBy(m => m.Tier).ThenBy(m => m.OutputPricePerMillionUsd).ThenBy(m => m.ModelId)
            .ToListAsync(cancellationToken);

    public Task<ProviderModel?> GetCheapestEnabledAsync(CancellationToken cancellationToken) =>
        db.ProviderModels.AsNoTracking()
            .Where(m => m.IsEnabled)
            .OrderBy(m => m.OutputPricePerMillionUsd).ThenBy(m => m.InputPricePerMillionUsd).ThenBy(m => m.ModelId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<ProviderModel?> GetBaselineAsync(CancellationToken cancellationToken) =>
        db.ProviderModels.AsNoTracking()
            .FirstOrDefaultAsync(m => m.IsBaseline, cancellationToken);
}
