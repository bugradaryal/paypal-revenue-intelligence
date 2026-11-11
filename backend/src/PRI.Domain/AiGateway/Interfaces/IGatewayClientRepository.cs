using PRI.Domain.AiGateway.Entities;

namespace PRI.Domain.AiGateway.Interfaces;

public interface IGatewayClientRepository
{
    Task<GatewayClient?> GetFirstPartyAsync(CancellationToken cancellationToken);

    /// <summary>API anahtarının özetiyle etkin üçüncü taraf istemciyi bulur.</summary>
    Task<GatewayClient?> GetActiveByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken);

    /// <summary>Takip edilen (güncellenebilir) istemci.</summary>
    Task<GatewayClient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<GatewayClient>> ListAsync(CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken);

    void Add(GatewayClient client);
}
