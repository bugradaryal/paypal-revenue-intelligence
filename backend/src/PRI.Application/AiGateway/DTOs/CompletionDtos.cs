using PRI.Application.Common.DTOs;
using PRI.Domain.AiGateway.Enums;

namespace PRI.Application.AiGateway.DTOs;

public sealed record CompletionMessageDto(string Role, string Content);

/// <param name="Model">Model kimliği (ör. gpt-6-luna) ya da "auto". Boş veya "auto" ise gateway yönlendirme kurallarıyla seçer.</param>
/// <param name="TaskType">Otomatik yönlendirme için görev tipi ipucu (ör. classification, summarization, analysis). Bilinmiyorsa "default" kuralı uygulanır.</param>
/// <param name="MaxOutputTokens">Üretilecek en fazla token (gizli akıl yürütme token'ları da buna sayılır).</param>
/// <param name="Temperature">Verilirse sağlayıcıya reasoning_effort=none ile birlikte gönderilir (modeller aksi halde reddeder).</param>
/// <param name="ReasoningEffort">none | low | medium | high | xhigh | max. Boşsa sağlayıcı varsayılanı.</param>
/// <param name="Cache">false ise bu istek için semantic cache kullanılmaz (arama da yazma da yapılmaz). Boşsa true.</param>
/// <param name="OptimizePrompt">false ise mesajlar olduğu gibi gönderilir. Boşsa true (yalnızca anlamı değiştirmeyen boşluk temizliği yapılır).</param>
public sealed record CompletionRequest(
    string? Model,
    string? TaskType,
    IReadOnlyList<CompletionMessageDto> Messages,
    int? MaxOutputTokens,
    double? Temperature,
    string? ReasoningEffort,
    bool? Cache = null,
    bool? OptimizePrompt = null);

public sealed record CompletionUsageDto(int InputTokens, int CachedInputTokens, int OutputTokens);

/// <param name="Mode">"explicit" (model istemci tarafından sabitlendi) veya "auto" (yönlendirme kuralıyla seçildi).</param>
/// <param name="Rule">Uygulanan yönlendirme kuralının görev tipi; sabit modelde veya kural yoksa null.</param>
/// <param name="FallbackUsed">İlk model başarısız olup yedek modele geçildiyse true.</param>
/// <param name="AttemptedModels">Denenen modeller, sırayla.</param>
public sealed record RoutingInfoDto(
    string Mode,
    string? TaskType,
    string? Rule,
    bool FallbackUsed,
    IReadOnlyList<string> AttemptedModels);

/// <param name="Id">Başarılı denemenin kullanım kaydının kimliği; cache hit'te cache aramasının kaydının kimliği.</param>
/// <param name="Usage">Bu istekte sağlayıcıya ödenen gerçek token'lar; cache hit'te 0.</param>
/// <param name="Cost">Yalnızca tamamlama maliyeti (cache hit'te 0). Embedding maliyeti cache.embeddingCost alanındadır.</param>
public sealed record CompletionResponse(
    Guid Id,
    string Content,
    string Model,
    AiProvider Provider,
    CompletionUsageDto Usage,
    MoneyDto Cost,
    int LatencyMs,
    string? FinishReason,
    RoutingInfoDto Routing,
    CompletionCacheDto Cache,
    PromptOptimizationDto PromptOptimization);

public sealed record ProviderModelDto(
    Guid Id,
    AiProvider Provider,
    string ModelId,
    string DisplayName,
    ModelTier Tier,
    MoneyDto InputPricePerMillion,
    MoneyDto? CachedInputPricePerMillion,
    MoneyDto OutputPricePerMillion,
    bool IsEnabled,
    bool IsBaseline,
    string PricingSourceUrl,
    DateOnly PricingCheckedOn);
