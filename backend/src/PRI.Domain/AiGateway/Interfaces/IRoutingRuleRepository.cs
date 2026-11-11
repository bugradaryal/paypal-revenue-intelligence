using PRI.Domain.AiGateway.Entities;

namespace PRI.Domain.AiGateway.Interfaces;

public interface IRoutingRuleRepository
{
    Task<RoutingRule?> GetByTaskTypeAsync(string taskType, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoutingRule>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Güncellenecek kuralı takip edilen olarak getirir.</summary>
    Task<RoutingRule?> GetTrackedByTaskTypeAsync(string taskType, CancellationToken cancellationToken);

    void Add(RoutingRule rule);
}
