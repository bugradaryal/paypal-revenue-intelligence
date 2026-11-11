using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;

namespace PRI.Application.AiGateway.Services;

public static class PromptOptimizerExtensions
{
    /// <summary>DTO'lar üzerinden optimizasyon (POST /gateway/prompt/optimize). Sağlayıcı çağrılmaz, maliyet yoktur.</summary>
    public static OptimizePromptResponse Optimize(this IPromptOptimizer optimizer, OptimizePromptRequest request)
    {
        var result = optimizer.Optimize(request.Messages.Select(m => new ProviderMessage(m.Role, m.Content)).ToList());

        return new OptimizePromptResponse(
            result.Messages.Select(m => new CompletionMessageDto(m.Role, m.Content)).ToList(),
            new PromptOptimizationDto(
                result.Applied, result.CharactersBefore, result.CharactersAfter, result.CharactersSaved, result.EstimatedTokensSaved));
    }
}
