using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.Common.DTOs;
using PRI.Domain.AiGateway.Interfaces;

namespace PRI.Application.AiGateway.Services;

public sealed class ModelCatalogService(IProviderModelRepository models) : IModelCatalogService
{
    private const string Usd = "USD";

    public async Task<IReadOnlyList<ProviderModelDto>> ListAsync(CancellationToken cancellationToken)
    {
        var all = await models.ListAsync(cancellationToken);

        return all
            .Select(m => new ProviderModelDto(
                m.Id,
                m.Provider,
                m.ModelId,
                m.DisplayName,
                m.Tier,
                MoneyDto.FromPrecise(m.InputPricePerMillionUsd, Usd),
                m.CachedInputPricePerMillionUsd is { } cached ? MoneyDto.FromPrecise(cached, Usd) : null,
                MoneyDto.FromPrecise(m.OutputPricePerMillionUsd, Usd),
                m.IsEnabled,
                m.IsBaseline,
                m.PricingSourceUrl,
                m.PricingCheckedOn))
            .ToList();
    }
}
