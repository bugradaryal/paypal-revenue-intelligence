using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Domain.Common.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Application.AiGateway.Services;

public sealed class RoutingRuleService(
    IRoutingRuleRepository rules,
    IProviderModelRepository models,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRoutingRuleService
{
    public async Task<IReadOnlyList<RoutingRuleDto>> ListAsync(CancellationToken cancellationToken)
    {
        var catalog = (await models.ListAsync(cancellationToken)).ToDictionary(m => m.Id);

        return (await rules.ListAsync(cancellationToken))
            .OrderBy(r => r.TaskType == RoutingRule.DefaultTaskType ? 0 : 1)
            .ThenBy(r => r.TaskType)
            .Select(r => ToDto(r, catalog))
            .ToList();
    }

    public async Task<RoutingRuleDto> SetAsync(SetRoutingRuleRequest request, CancellationToken cancellationToken)
    {
        var catalog = await models.ListAsync(cancellationToken);
        var byModelId = catalog.ToDictionary(m => m.ModelId, StringComparer.OrdinalIgnoreCase);

        var primary = Resolve(request.PrimaryModelId, byModelId, nameof(request.PrimaryModelId));
        var fallbacks = (request.FallbackModelIds ?? [])
            .Select(id => Resolve(id, byModelId, nameof(request.FallbackModelIds)))
            .ToList();

        var taskType = request.TaskType.Trim().ToLowerInvariant();
        var rule = await rules.GetTrackedByTaskTypeAsync(taskType, cancellationToken);

        if (rule is null)
        {
            rule = RoutingRule.Create(taskType, primary.Id, fallbacks.Select(f => f.Id), timeProvider.GetUtcNow().UtcDateTime);
            rules.Add(rule);
        }
        else
        {
            rule.Update(primary.Id, fallbacks.Select(f => f.Id));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(rule, catalog.ToDictionary(m => m.Id));
    }

    // Yönlendirme yalnızca etkin modelleri seçer; kapalı bir modelin kurala yazılması sessizce işe yaramaz olurdu.
    private static ProviderModel Resolve(string modelId, Dictionary<string, ProviderModel> byModelId, string field)
    {
        if (!byModelId.TryGetValue(modelId.Trim(), out var model))
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                [field] = [$"Unknown model '{modelId}'. See GET /api/v1/gateway/models."]
            });
        }

        if (!model.IsEnabled)
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                [field] = [$"Model '{modelId}' is disabled."]
            });
        }

        return model;
    }

    private static RoutingRuleDto ToDto(RoutingRule rule, Dictionary<Guid, ProviderModel> catalog) =>
        new(
            rule.TaskType,
            catalog.TryGetValue(rule.PrimaryModelId, out var primary) ? primary.ModelId : rule.PrimaryModelId.ToString(),
            rule.FallbackModelIds
                .Select(id => catalog.TryGetValue(id, out var model) ? model.ModelId : id.ToString())
                .ToList());
}
