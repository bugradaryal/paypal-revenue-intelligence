using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.Common.DTOs;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Shared.Constants;
using PRI.Shared.Models;

namespace PRI.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Revenue + "/recovery")]
public sealed class RevenueRecoveryController(IRevenueQueries queries) : ApiControllerBase
{
    /// <summary>Recovery ekranı: aralıkta başlayan kayıplar, kurtarılanlar ve onay bekleyen öneri sayısı.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<RecoverySummaryDto>> Summary(
        [FromQuery] DateRangeQuery query, CancellationToken cancellationToken) =>
        Ok(await queries.GetRecoverySummaryAsync(query.From!.Value.UtcDateTime, query.To!.Value.UtcDateTime, cancellationToken));
}

[Authorize]
[Route(ApiRoutes.Revenue + "/issues")]
public sealed class RevenueIssuesController(IRevenueQueries queries) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<IssueDto>>> List(
        [FromQuery] IssueListQuery query, CancellationToken cancellationToken) =>
        Ok(await queries.ListIssuesAsync(query, cancellationToken));
}
