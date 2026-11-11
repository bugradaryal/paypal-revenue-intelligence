using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Shared.Constants;

namespace PRI.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Gateway + "/models")]
public sealed class GatewayModelsController(IModelCatalogService catalogService) : ApiControllerBase
{
    /// <summary>Model kataloğu ve resmi token fiyatları (fiyat kaynağı ve kontrol tarihiyle).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProviderModelDto>>> List(CancellationToken cancellationToken) =>
        Ok(await catalogService.ListAsync(cancellationToken));
}
