using PRI.Application.AiGateway.Interfaces;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Application.AiGateway.Services;

/// <summary>
/// Kural tabanlı model yönlendirici. Model sabitlenmişse yalnızca o model denenir (sessizce başka modele geçilmez).
/// "auto" veya boşsa görev tipine uyan yönlendirme kuralı, yoksa "default" kuralı, o da yoksa en ucuz etkin model kullanılır.
/// </summary>
public sealed class ModelRouter(IProviderModelRepository models, IRoutingRuleRepository rules) : IModelRouter
{
    private const string Auto = "auto";

    public async Task<RoutePlan> BuildPlanAsync(string? requestedModel, string? taskType, CancellationToken cancellationToken)
    {
        var normalizedTaskType = string.IsNullOrWhiteSpace(taskType) ? null : taskType.Trim().ToLowerInvariant();
        var enabled = (await models.ListAsync(cancellationToken)).Where(m => m.IsEnabled).ToList();

        var pinned = requestedModel?.Trim();
        if (!string.IsNullOrEmpty(pinned) && !string.Equals(pinned, Auto, StringComparison.OrdinalIgnoreCase))
        {
            var model = enabled.FirstOrDefault(m => m.ModelId == pinned)
                ?? throw new RequestValidationException(new Dictionary<string, string[]>
                {
                    ["model"] = [$"Unknown or disabled model '{pinned}'. See GET /api/v1/gateway/models."]
                });

            return new RoutePlan([model], "explicit", normalizedTaskType, null);
        }

        var rule = normalizedTaskType is null ? null : await rules.GetByTaskTypeAsync(normalizedTaskType, cancellationToken);
        rule ??= await rules.GetByTaskTypeAsync(RoutingRule.DefaultTaskType, cancellationToken);

        var plan = new List<ProviderModel>();
        if (rule is not null)
        {
            foreach (var id in new[] { rule.PrimaryModelId }.Concat(rule.FallbackModelIds))
            {
                var model = enabled.FirstOrDefault(m => m.Id == id);
                if (model is not null && plan.All(p => p.Id != model.Id))
                {
                    plan.Add(model);
                }
            }
        }

        if (plan.Count == 0)
        {
            var cheapest = enabled.OrderBy(m => m.OutputPricePerMillionUsd).ThenBy(m => m.InputPricePerMillionUsd).FirstOrDefault()
                ?? throw new BusinessRuleException("no_enabled_models", "No AI model is enabled in the catalog.");

            plan.Add(cheapest);
            return new RoutePlan(plan, "auto", normalizedTaskType, null);
        }

        return new RoutePlan(plan, "auto", normalizedTaskType, rule!.TaskType);
    }
}
