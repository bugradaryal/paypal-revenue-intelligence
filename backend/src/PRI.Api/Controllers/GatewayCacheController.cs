using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Shared.Constants;

namespace PRI.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Gateway + "/cache")]
public sealed class GatewayCacheController(ISemanticCacheService cacheService) : ApiControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<CacheStatsDto>> Stats(CancellationToken cancellationToken) =>
        Ok(await cacheService.GetStatsAsync(cancellationToken));

    /// <summary>Tüm önbellek kayıtlarını siler (geçmiş cache istatistikleri korunur).</summary>
    [Authorize(Roles = AuthRoles.Admin)]
    [HttpDelete]
    public async Task<ActionResult<ClearCacheResultDto>> Clear(CancellationToken cancellationToken) =>
        Ok(await cacheService.ClearAsync(cancellationToken));
}
