using PRI.Application.AiGateway.DTOs;
using PRI.Domain.AiGateway.Entities;

namespace PRI.Application.AiGateway.Interfaces;

public interface IAiCompletionService
{
    /// <param name="clientId">API anahtarıyla doğrulanan istemci; null ise (panel kullanıcısı, JWT) first-party istemci kullanılır.</param>
    Task<CompletionResponse> CompleteAsync(CompletionRequest request, Guid? clientId, CancellationToken cancellationToken);
}

public interface IModelCatalogService
{
    Task<IReadOnlyList<ProviderModelDto>> ListAsync(CancellationToken cancellationToken);
}

/// <param name="Models">Sırayla denenecek modeller (ilki ana model).</param>
/// <param name="Mode">"explicit" veya "auto".</param>
/// <param name="Rule">Uygulanan kuralın görev tipi; yoksa null.</param>
public sealed record RoutePlan(IReadOnlyList<ProviderModel> Models, string Mode, string? TaskType, string? Rule);

public interface IModelRouter
{
    Task<RoutePlan> BuildPlanAsync(string? requestedModel, string? taskType, CancellationToken cancellationToken);
}

public interface IGatewayClientService
{
    Task<CreatedGatewayClientDto> CreateAsync(CreateClientRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<GatewayClientDto>> ListAsync(CancellationToken cancellationToken);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>Gateway kullanım verisinin salt okunur sorguları (EF projection, Infrastructure'da).</summary>
public interface IGatewayQueries
{
    Task<UsageSummaryDto> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<UsageSeriesDto> GetSeriesAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<UsageBreakdownDto> GetBreakdownAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<CacheTotals> GetCacheTotalsAsync(DateTime nowUtc, CancellationToken cancellationToken);
}

public interface IRoutingRuleService
{
    Task<IReadOnlyList<RoutingRuleDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Görev tipi için kuralı oluşturur veya günceller (yalnızca etkin modeller seçilebilir).</summary>
    Task<RoutingRuleDto> SetAsync(SetRoutingRuleRequest request, CancellationToken cancellationToken);
}
