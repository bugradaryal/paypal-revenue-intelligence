using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.Common;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Shared.Constants;
using PRI.Shared.Models;

namespace PRI.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Revenue + "/insights")]
public sealed class RevenueInsightsController(IInsightService insights) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<InsightDto>>> List(
        [FromQuery] InsightListQuery query, CancellationToken cancellationToken) =>
        Ok(await insights.ListAsync(query, cancellationToken));

    /// <summary>Son N günün içgörüsünü üretir (rakamlar C# ile, yorum yapay zekâ veya şablonla). Dil: Accept-Language.</summary>
    [HttpPost("generate")]
    public async Task<ActionResult<InsightDto>> Generate(
        [FromQuery] GenerateInsightQuery query, CancellationToken cancellationToken)
    {
        var language = SupportedLanguages.Resolve(Request.Headers.AcceptLanguage.ToString());

        return Ok(await insights.GenerateAsync(query.Days, language, cancellationToken));
    }
}
