using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.Common.Interfaces;

namespace PRI.Application.AiGateway.Services;

/// <summary>
/// IAiGateway'in süreç içi uygulaması: birinci taraf istemci (panel) adına, gateway'in normal akışını (yönlendirme, cache, bütçe,
/// maliyet kaydı) kullanarak çağrı yapar. Diğer modüller gateway'e yalnızca bu arayüzle bağlanır.
/// </summary>
public sealed class InProcessAiGateway(IAiCompletionService completions) : IAiGateway
{
    public async Task<AiTextResult> GenerateAsync(AiTextRequest request, CancellationToken cancellationToken)
    {
        var response = await completions.CompleteAsync(
            new CompletionRequest(
                "auto",
                request.TaskType,
                [new CompletionMessageDto("system", request.SystemPrompt), new CompletionMessageDto("user", request.UserPrompt)],
                request.MaxOutputTokens,
                null,
                "none"),
            null,
            cancellationToken);

        return new AiTextResult(response.Content, response.FinishReason == "length", response.Id);
    }
}
