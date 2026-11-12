using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Shared.Constants;

namespace PRI.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Revenue + "/cash-flow")]
public sealed class CashFlowOutlookController(ICashFlowOutlookQueries queries) : ApiControllerBase
{
    /// <summary>Invoice-backed receivables schedule; no probabilistic revenue prediction is implied.</summary>
    [HttpGet("outlook")]
    public async Task<ActionResult<CashFlowOutlookDto>> Outlook(CancellationToken cancellationToken) =>
        Ok(await queries.GetOutlookAsync(cancellationToken));
}
