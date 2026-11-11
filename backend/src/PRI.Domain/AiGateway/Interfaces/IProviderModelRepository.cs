using PRI.Domain.AiGateway.Entities;

namespace PRI.Domain.AiGateway.Interfaces;

public interface IProviderModelRepository
{
    Task<IReadOnlyList<ProviderModel>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Etkin modeller arasında en ucuzu (çıktı fiyatına göre); hiç yoksa null.</summary>
    Task<ProviderModel?> GetCheapestEnabledAsync(CancellationToken cancellationToken);

    /// <summary>Tasarruf hesabının baz modeli (etkin olması gerekmez); tanımlı değilse null.</summary>
    Task<ProviderModel?> GetBaselineAsync(CancellationToken cancellationToken);
}
