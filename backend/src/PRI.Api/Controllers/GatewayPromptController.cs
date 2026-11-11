using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Api.Authentication;
using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.AiGateway.Services;
using PRI.Shared.Constants;

namespace PRI.Api.Controllers;

[Authorize(AuthenticationSchemes = ApiKeyDefaults.BearerOrApiKey)]
[Route(ApiRoutes.Gateway + "/prompt")]
public sealed class GatewayPromptController(IPromptOptimizer optimizer) : ApiControllerBase
{
    /// <summary>Promptu sağlayıcıya göndermeden optimize eder ve sonucu gösterir (ücretsiz, LLM çağrısı yok).</summary>
    [HttpPost("optimize")]
    public ActionResult<OptimizePromptResponse> Optimize(OptimizePromptRequest request) =>
        Ok(optimizer.Optimize(request));
}
