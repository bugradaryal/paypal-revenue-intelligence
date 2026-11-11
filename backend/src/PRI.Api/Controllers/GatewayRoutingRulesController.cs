using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Shared.Constants;

namespace PRI.Api.Controllers;

/// <summary>Görev tipine göre model yönlendirme kuralları. Okuma tüm panel kullanıcılarına, değiştirme yalnızca admin'e açıktır.</summary>
[Authorize]
[Route(ApiRoutes.Gateway + "/routing-rules")]
public sealed class GatewayRoutingRulesController(IRoutingRuleService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoutingRuleDto>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(cancellationToken));

    /// <summary>Görev tipi için kuralı oluşturur veya günceller; yalnızca katalogdaki etkin modeller seçilebilir.</summary>
    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPut]
    public async Task<ActionResult<RoutingRuleDto>> Set(SetRoutingRuleRequest request, CancellationToken cancellationToken) =>
        Ok(await service.SetAsync(request, cancellationToken));
}
