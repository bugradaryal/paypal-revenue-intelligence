using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Api.Authentication;
using PRI.Api.Extensions;
using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Shared.Constants;

namespace PRI.Api.Controllers;

[Authorize(AuthenticationSchemes = ApiKeyDefaults.BearerOrApiKey)]
[Route(ApiRoutes.Gateway + "/completions")]
public sealed class GatewayCompletionsController(IAiCompletionService completionService) : ApiControllerBase
{
    /// <summary>
    /// Sohbet tamamlama. Panel kullanıcıları JWT ile, üçüncü taraf istemciler X-Api-Key ile çağırır.
    /// 60 saniyeye kadar sürebilir. Maliyet gerçek token sayısı ve kayıtlı resmi fiyatlarla hesaplanır.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CompletionResponse>> Complete(
        CompletionRequest request, CancellationToken cancellationToken) =>
        Ok(await completionService.CompleteAsync(request, User.GetClientId(), cancellationToken));
}
