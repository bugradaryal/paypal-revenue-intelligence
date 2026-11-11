using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Shared.Constants;

namespace PRI.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Revenue + "/sync")]
public sealed class SyncController(IPayPalSyncService syncService) : ApiControllerBase
{
    [HttpPut("/" + ApiRoutes.Base + "/paypal/credentials")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<PayPalCredentialsSavedDto>> SaveCredentials(
        [FromBody] SavePayPalCredentialsRequest request,
        [FromServices] IPayPalCredentialService credentialService,
        CancellationToken cancellationToken) =>
        Ok(await credentialService.SaveAsync(request, cancellationToken));

    [HttpGet("/" + ApiRoutes.Base + "/paypal/connection")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<PayPalConnectionDto>> Connection(CancellationToken cancellationToken) =>
        Ok(await syncService.GetConnectionAsync(cancellationToken));

    [HttpPost("/" + ApiRoutes.Base + "/paypal/test-connection")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<PayPalTestConnectionDto>> TestConnection(CancellationToken cancellationToken) =>
        Ok(await syncService.TestConnectionAsync(cancellationToken));

    /// <summary>PayPal Sandbox'tan son N günü içe alır. Çalışma bitene kadar bekler (60 sn'ye kadar sürebilir).</summary>
    [HttpPost]
    [HttpPost("/" + ApiRoutes.Base + "/paypal/sync")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<SyncRunDto>> Run([FromQuery] SyncRequest request, CancellationToken cancellationToken) =>
        Ok(await syncService.SyncAsync(request.Days, cancellationToken));

    /// <summary>En son senkron çalışması. Hiç çalışmadıysa 404 döner.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<SyncRunDto>> Status(CancellationToken cancellationToken) =>
        Ok(await syncService.GetLatestAsync(cancellationToken));
}
