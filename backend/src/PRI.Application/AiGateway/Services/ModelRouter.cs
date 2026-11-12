using PRI.Application.AiGateway.Interfaces;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Enums;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Application.AiGateway.Services;

/// <summary>
/// Kural tabanlı model yönlendirici. Model sabitlenmişse yalnızca o model denenir (sessizce başka modele geçilmez).
/// "auto" veya boşsa görev kuralının modelleri ve diğer sağlayıcıların adayları görev uygunluğu ile fiyata göre sıralanır.
/// </summary>
public sealed class ModelRouter(IProviderModelRepository models, IRoutingRuleRepository rules) : IModelRouter
{
    private const string Auto = "auto";

    public async Task<RoutePlan> BuildPlanAsync(
        string? requestedModel,
        string? taskType,
        CancellationToken cancellationToken,
        AiProvider? provider = null)
    {
        var normalizedTaskType = string.IsNullOrWhiteSpace(taskType) ? null : taskType.Trim().ToLowerInvariant();
        var enabled = (await models.ListAsync(cancellationToken)).Where(m => m.IsEnabled).ToList();

        if (provider is not null && provider is not (AiProvider.Openai or AiProvider.Gemini))
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["provider"] = ["Only OpenAI and Gemini providers are supported."]
            });
        }

        var pinned = requestedModel?.Trim();
        if (!string.IsNullOrEmpty(pinned) && !string.Equals(pinned, Auto, StringComparison.OrdinalIgnoreCase))
        {
            var model = enabled.FirstOrDefault(m => m.ModelId == pinned)
                ?? throw new RequestValidationException(new Dictionary<string, string[]>
                {
                    ["model"] = [$"Unknown or disabled model '{pinned}'. See GET /api/v1/gateway/models."]
                });

            if (provider is not null && model.Provider != provider)
            {
                throw new RequestValidationException(new Dictionary<string, string[]>
                {
                    ["provider"] = [$"Model '{pinned}' does not belong to provider '{provider}'."]
                });
            }

            return new RoutePlan([model], "explicit", normalizedTaskType, null);
        }

        var candidates = provider is null
            ? enabled
            : enabled.Where(model => model.Provider == provider).ToList();
        if (provider is not null && candidates.Count == 0)
        {
            throw new BusinessRuleException(
                "provider_not_available",
                $"No enabled model is available for provider '{provider}'.");
        }

        var rule = normalizedTaskType is null ? null : await rules.GetByTaskTypeAsync(normalizedTaskType, cancellationToken);
        rule ??= await rules.GetByTaskTypeAsync(RoutingRule.DefaultTaskType, cancellationToken);

        var plan = new List<ProviderModel>();
        if (rule is not null)
        {
            foreach (var id in new[] { rule.PrimaryModelId }.Concat(rule.FallbackModelIds))
            {
                var model = candidates.FirstOrDefault(m => m.Id == id);
                if (model is not null && plan.All(p => p.Id != model.Id))
                {
                    plan.Add(model);
                }
            }

            if (plan.Count > 0)
            {
                var routingCandidates = new List<ProviderModel>(plan);
                if (provider is null)
                {
                    var representedProviders = plan.Select(model => model.Provider).ToHashSet();
                    var providerAlternatives = enabled
                        .Where(model => model.Provider is AiProvider.Openai or AiProvider.Gemini
                                        && !representedProviders.Contains(model.Provider))
                        .GroupBy(model => model.Provider)
                        .Select(group => group
                            .OrderByDescending(model => ScoreModelForTask(model, normalizedTaskType))
                            .ThenBy(model => model.OutputPricePerMillionUsd)
                            .ThenBy(model => model.InputPricePerMillionUsd)
                            .First());
                    routingCandidates.AddRange(providerAlternatives);
                }

                var ordered = routingCandidates
                    .OrderByDescending(m => ScoreModelForTask(m, normalizedTaskType))
                    .ThenBy(m => m.OutputPricePerMillionUsd)
                    .ThenBy(m => m.InputPricePerMillionUsd)
                    .ToList();

                return new RoutePlan(ordered, "auto", normalizedTaskType, rule.TaskType);
            }
        }

        var heuristic = candidates
            .OrderByDescending(m => ScoreModelForTask(m, normalizedTaskType))
            .ThenBy(m => m.OutputPricePerMillionUsd)
            .ThenBy(m => m.InputPricePerMillionUsd)
            .ToList();

        if (heuristic.Count == 0)
        {
            throw new BusinessRuleException("no_enabled_models", "No AI model is enabled in the catalog.");
        }

        var top = heuristic.Take(2).ToList();
        return new RoutePlan(top, "auto", normalizedTaskType, null);
    }

    private static int ScoreModelForTask(ProviderModel model, string? taskType)
    {
        var profile = ClassifyTask(taskType);

        return profile switch
        {
            TaskProfile.Simple => model.Tier switch
            {
                ModelTier.Economy => 90,
                ModelTier.Standard => 70,
                ModelTier.Premium => 40,
                _ => 10
            },
            TaskProfile.Complex => model.Tier switch
            {
                ModelTier.Economy => 25,
                ModelTier.Standard => 90,
                ModelTier.Premium => 80,
                _ => 10
            },
            _ => model.Tier switch
            {
                ModelTier.Economy => 80,
                ModelTier.Standard => 75,
                ModelTier.Premium => 60,
                _ => 10
            }
        };
    }

    private static TaskProfile ClassifyTask(string? taskType)
    {
        if (string.IsNullOrWhiteSpace(taskType))
        {
            return TaskProfile.Balanced;
        }

        var normalized = taskType.Trim().ToLowerInvariant();

        if (normalized.Contains("analysis") || normalized.Contains("reason") || normalized.Contains("compare") || normalized.Contains("audit") || normalized.Contains("plan") || normalized.Contains("debug") || normalized.Contains("investig") || normalized.Contains("root") || normalized.Contains("risk"))
        {
            return TaskProfile.Complex;
        }

        if (normalized.Contains("summary") || normalized.Contains("summar") || normalized.Contains("class") || normalized.Contains("label") || normalized.Contains("extract") || normalized.Contains("translate") || normalized.Contains("rewrite") || normalized.Contains("detect"))
        {
            return TaskProfile.Simple;
        }

        return TaskProfile.Balanced;
    }

    private enum TaskProfile
    {
        Simple,
        Balanced,
        Complex
    }
}
