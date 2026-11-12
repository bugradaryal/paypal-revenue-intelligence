using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.Common.DTOs;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Enums;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Domain.Common.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Application.AiGateway.Services;

/// <summary>
/// Gateway'in çekirdeği. Sıra: prompt optimizasyonu → yönlendirme planı → semantic cache araması (hit ise sağlayıcı çağrılmaz)
/// → bütçe denetimi → sağlayıcı çağrısı (gerekirse yedek modele geçiş) → kullanım kaydı → cache'e yazma.
/// Maliyet her zaman gerçek token sayısı ve kayıtlı resmi fiyatlardan hesaplanır. Cache yazımı hiçbir zaman başarılı bir
/// isteği başarısız yapmaz.
/// </summary>
public sealed class AiCompletionService(
    IModelRouter router,
    IProviderModelRepository models,
    IGatewayClientRepository clients,
    IUsageRecordRepository usageRecords,
    IAiProviderResolver providerResolver,
    IPromptOptimizer promptOptimizer,
    ISemanticCacheService cache,
    IBudgetService budget,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<AiCompletionService> logger) : IAiCompletionService
{
    private const string Usd = "USD";

    // Reddedilen istek veya geçersiz API anahtarı başka bir modelde de aynı sonucu verir; yedek denenmez.
    private static readonly string[] FallbackEligibleErrorCodes =
        ["provider_unavailable", "provider_rate_limited", "openai_not_configured", "gemini_not_configured"];

    public async Task<CompletionResponse> CompleteAsync(
        CompletionRequest request, Guid? clientId, CancellationToken cancellationToken)
    {
        var startedAll = Stopwatch.GetTimestamp();
        var effectiveClientId = clientId ?? await GetFirstPartyClientIdAsync(cancellationToken);

        var taskTypeHint = request.TaskType;
        if (string.IsNullOrWhiteSpace(taskTypeHint)
            || string.Equals(taskTypeHint.Trim(), "default", StringComparison.OrdinalIgnoreCase))
        {
            taskTypeHint = TaskTypeClassifier.Classify(
                request.Messages.LastOrDefault(message => message.Role == "user")?.Content);
        }

        var plan = await router.BuildPlanAsync(request.Model, taskTypeHint, cancellationToken, request.Provider);
        var isAuto = plan.Mode == "auto";
        var taskType = plan.TaskType;

        var original = request.Messages.Select(m => new ProviderMessage(m.Role, m.Content)).ToList();
        var optimization = request.OptimizePrompt ?? true
            ? promptOptimizer.Optimize(original)
            : OptimizedPrompt.Unchanged(original);
        var messages = optimization.Messages;
        var optimizationDto = new PromptOptimizationDto(
            optimization.Applied, optimization.CharactersBefore, optimization.CharactersAfter,
            optimization.CharactersSaved, optimization.EstimatedTokensSaved);

        // --- Semantic cache ---
        CacheLookupResult? lookup = null;
        if (request.Cache ?? true)
        {
            lookup = await cache.LookupAsync(
                new CacheLookupInput(
                    effectiveClientId, request.Model, taskType, request.MaxOutputTokens,
                    request.Temperature, request.ReasoningEffort, messages, request.Provider),
                cancellationToken);
        }

        if (lookup?.Hit is { } hit)
        {
            var record = cache.StageHit(lookup);
            await TrySaveBookkeepingAsync();

            return new CompletionResponse(
                record.Id,
                hit.Entry.ResponseContent,
                hit.Entry.ModelId,
                hit.Entry.Provider,
                new CompletionUsageDto(0, 0, 0),
                MoneyDto.FromPrecise(0m, Usd),
                ElapsedMs(startedAll),
                hit.Entry.FinishReason,
                new RoutingInfoDto(plan.Mode, plan.TaskType, plan.Rule, false, []),
                new CompletionCacheDto(
                    "hit",
                    hit.Outcome == CacheOutcome.ExactHit ? "exact" : "semantic",
                    hit.Outcome == CacheOutcome.SemanticHit ? hit.Similarity : null,
                    MoneyDto.FromPrecise(hit.Entry.OriginalCostUsd, Usd),
                    hit.Entry.Id,
                    MoneyDto.FromPrecise(lookup.EmbeddingCostUsd, Usd)),
                optimizationDto);
        }

        if (lookup is not null)
        {
            cache.StageMiss(lookup);
        }

        // --- Bütçe: cache hit'ler ücretsiz olduğundan denetimden önce döner ---
        try
        {
            await budget.EnsureWithinBudgetAsync(cancellationToken);
        }
        catch (BusinessRuleException)
        {
            if (lookup is not null)
            {
                // Embedding için ödenen maliyet (miss kaydı) kaybolmasın.
                await TrySaveBookkeepingAsync();
            }

            throw;
        }

        var baseline = isAuto ? await models.GetBaselineAsync(cancellationToken) : null;
        var attempted = new List<string>();

        for (var index = 0; index < plan.Models.Count; index++)
        {
            var model = plan.Models[index];
            var attempt = index + 1;
            var isLast = attempt == plan.Models.Count;

            attempted.Add(model.ModelId);

            var provider = providerResolver.Get(model.Provider);
            var providerRequest = new ProviderCompletionRequest(
                model.ModelId, messages, request.MaxOutputTokens, request.Temperature, request.ReasoningEffort);

            var requestedAt = timeProvider.GetUtcNow().UtcDateTime;
            var startedTimestamp = Stopwatch.GetTimestamp();

            ProviderCompletionResult result;
            try
            {
                result = await provider.CompleteAsync(providerRequest, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var errorCode = ex is AppException app ? app.Code : "internal_error";

                usageRecords.Add(UsageRecord.Failure(
                    effectiveClientId, model, taskType, isAuto, attempt, errorCode,
                    ElapsedMs(startedTimestamp), requestedAt, timeProvider.GetUtcNow().UtcDateTime));
                await unitOfWork.SaveChangesAsync(CancellationToken.None);

                if (!isLast && FallbackEligibleErrorCodes.Contains(errorCode))
                {
                    continue;
                }

                throw;
            }

            var latencyMs = ElapsedMs(startedTimestamp);
            var usage = UsageRecord.Success(
                effectiveClientId, model, taskType, isAuto, attempt,
                result.InputTokens, result.CachedInputTokens, result.OutputTokens,
                baseline, latencyMs, result.ProviderRequestId, requestedAt, timeProvider.GetUtcNow().UtcDateTime,
                optimization.CharactersSaved);

            usageRecords.Add(usage);
            await unitOfWork.SaveChangesAsync(cancellationToken);   // kullanım kaydı + (varsa) miss kaydı birlikte yazılır

            if (lookup is not null)
            {
                cache.StageStore(lookup, model, result, usage.CostUsd);
                await TrySaveBookkeepingAsync();
            }

            return new CompletionResponse(
                usage.Id,
                result.Content,
                model.ModelId,
                model.Provider,
                new CompletionUsageDto(usage.InputTokens, usage.CachedInputTokens, usage.OutputTokens),
                MoneyDto.FromPrecise(usage.CostUsd, Usd),
                latencyMs,
                result.FinishReason,
                new RoutingInfoDto(plan.Mode, plan.TaskType, plan.Rule, attempt > 1, attempted),
                new CompletionCacheDto(
                    lookup is null ? "bypass" : "miss",
                    null,
                    null,
                    null,
                    null,
                    MoneyDto.FromPrecise(lookup?.EmbeddingCostUsd ?? 0m, Usd)),
                optimizationDto);
        }

        // Plan boş olamaz (router en az bir model döndürür); buraya yalnızca sıfır modelde gelinir.
        throw new BusinessRuleException("no_enabled_models", "No AI model is enabled in the catalog.");
    }

    /// <summary>Cache muhasebesi (hit sayacı, arama kaydı, yeni kayıt) başarısız olsa bile istek sonucu etkilenmez.</summary>
    private async Task TrySaveBookkeepingAsync()
    {
        try
        {
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Saving semantic cache bookkeeping failed; the request result is unaffected.");
        }
    }

    private async Task<Guid> GetFirstPartyClientIdAsync(CancellationToken cancellationToken)
    {
        var client = await clients.GetFirstPartyAsync(cancellationToken)
            ?? throw new BusinessRuleException("gateway_not_initialized", "The first-party gateway client has not been seeded.");

        return client.Id;
    }

    private static int ElapsedMs(long startedTimestamp) =>
        (int)Math.Min(Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds, int.MaxValue);
}
