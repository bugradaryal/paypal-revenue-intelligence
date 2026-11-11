using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Shared.Constants;

namespace PRI.Api.Controllers;

/// <summary>Üçüncü taraf Gateway istemcilerinin ve API anahtarlarının yönetimi (yalnızca admin).</summary>
[Authorize(Roles = AuthRoles.Admin)]
[Route(ApiRoutes.Gateway + "/clients")]
public sealed class GatewayClientsController(IGatewayClientService clientService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GatewayClientDto>>> List(CancellationToken cancellationToken) =>
        Ok(await clientService.ListAsync(cancellationToken));

    /// <summary>Yeni istemci ve API anahtarı oluşturur. Düz anahtar yalnızca bu yanıtta bir kez döner.</summary>
    [HttpPost]
    public async Task<ActionResult<CreatedGatewayClientDto>> Create(
        CreateClientRequest request, CancellationToken cancellationToken)
    {
        var created = await clientService.CreateAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>İstemciyi kapatır; anahtarı artık kabul edilmez.</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await clientService.DeactivateAsync(id, cancellationToken);

        return NoContent();
    }
}
