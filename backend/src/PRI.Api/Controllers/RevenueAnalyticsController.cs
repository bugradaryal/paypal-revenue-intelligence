using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.Common.DTOs;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Shared.Constants;

namespace PRI.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Revenue + "/analytics")]
public sealed class RevenueAnalyticsController(IRevenueAnalyticsQueries queries) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RevenueAnalyticsDto>> Get(
        [FromQuery] DateRangeQuery query,
        CancellationToken cancellationToken) =>
        Ok(await queries.GetRevenueAnalyticsAsync(
            query.From!.Value.UtcDateTime,
            query.To!.Value.UtcDateTime,
            cancellationToken));
}
