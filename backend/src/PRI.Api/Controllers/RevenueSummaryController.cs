using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.Common.DTOs;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Shared.Constants;

namespace PRI.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Revenue + "/summary")]
public sealed class RevenueSummaryController(IRevenueQueries queries) : ApiControllerBase
{
    /// <summary>Overview ekranı: gelir, başarısız, bekleyen ve günlük seri. from dahil, to hariçtir.</summary>
    [HttpGet]
    public async Task<ActionResult<RevenueSummaryDto>> Get(
        [FromQuery] DateRangeQuery query, CancellationToken cancellationToken)
    {
        // DateRangeQueryValidator from/to alanlarının dolu olduğunu garanti eder.
        var summary = await queries.GetSummaryAsync(
            query.From!.Value.UtcDateTime, query.To!.Value.UtcDateTime, cancellationToken);

        return Ok(summary);
    }
}
