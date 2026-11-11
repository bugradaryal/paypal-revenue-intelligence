using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Api.Extensions;
using PRI.Application.Common;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Shared.Constants;
using PRI.Shared.Models;

namespace PRI.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Revenue + "/suggestions")]
public sealed class RevenueSuggestionsController(IRevenueQueries queries, ISuggestionService suggestions) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SuggestionDto>>> List(
        [FromQuery] SuggestionListQuery query, CancellationToken cancellationToken) =>
        Ok(await queries.ListSuggestionsAsync(query, cancellationToken));

    /// <summary>
    /// Kayıpları yeniler ve önerisi olmayan açık kayıplar için (en fazla 10) öneri üretir. Gerekçe dili Accept-Language
    /// başlığından seçilir (en, de, tr, es; varsayılan en). Arka planda otomatik üretim yoktur; yalnızca bu çağrı üretir.
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<GenerateSuggestionsResultDto>> Generate(CancellationToken cancellationToken)
    {
        var language = SupportedLanguages.Resolve(Request.Headers.AcceptLanguage.ToString());

        return Ok(await suggestions.GenerateAsync(language, cancellationToken));
    }

    /// <summary>
    /// Öneriyi onaylar veya reddeder. Çalıştırılabilir (executable) bir öneri onaylanınca arka planda çalıştırılır ve
    /// durumu birkaç saniye içinde executed veya failed olur.
    /// </summary>
    [HttpPost("{id:guid}/decision")]
    public async Task<ActionResult<SuggestionDto>> Decide(
        Guid id, SuggestionDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(await suggestions.DecideAsync(id, request.Decision, User.GetUserId(), cancellationToken));
}
