using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.Common.DTOs;
using PRI.Shared.Constants;

namespace PRI.Api.Controllers;

/// <summary>AI Cost ekranı için kullanım uçları (yalnızca panel kullanıcıları, JWT).</summary>
[Authorize]
[Route(ApiRoutes.Gateway + "/usage")]
public sealed class GatewayUsageController(IGatewayQueries queries) : ApiControllerBase
{
    // DateRangeQueryValidator from/to alanlarının dolu olduğunu garanti eder. from dahil, to hariçtir.

    [HttpGet("summary")]
    public async Task<ActionResult<UsageSummaryDto>> Summary(
        [FromQuery] DateRangeQuery query, CancellationToken cancellationToken) =>
        Ok(await queries.GetSummaryAsync(query.From!.Value.UtcDateTime, query.To!.Value.UtcDateTime, cancellationToken));

    [HttpGet("series")]
    public async Task<ActionResult<UsageSeriesDto>> Series(
        [FromQuery] DateRangeQuery query, CancellationToken cancellationToken) =>
        Ok(await queries.GetSeriesAsync(query.From!.Value.UtcDateTime, query.To!.Value.UtcDateTime, cancellationToken));

    [HttpGet("breakdown")]
    public async Task<ActionResult<UsageBreakdownDto>> Breakdown(
        [FromQuery] DateRangeQuery query, CancellationToken cancellationToken) =>
        Ok(await queries.GetBreakdownAsync(query.From!.Value.UtcDateTime, query.To!.Value.UtcDateTime, cancellationToken));
}
