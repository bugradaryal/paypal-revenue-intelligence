using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Shared.Constants;

namespace PRI.Api.Controllers;

/// <summary>Aylık AI bütçesi (MVP: tek kayıt). Okuma tüm panel kullanıcılarına, değiştirme yalnızca admin'e açıktır.</summary>
[Authorize]
[Route(ApiRoutes.Gateway + "/budgets")]
public sealed class GatewayBudgetsController(IBudgetService budgetService) : ApiControllerBase
{
    /// <summary>Bütçe tanımlı değilse de 200 döner (isConfigured: false) ve içinde bulunulan ayın harcamasını gösterir.</summary>
    [HttpGet]
    public async Task<ActionResult<BudgetDto>> Get(CancellationToken cancellationToken) =>
        Ok(await budgetService.GetAsync(cancellationToken));

    [Authorize(Roles = AuthRoles.Admin)]
    [HttpPut]
    public async Task<ActionResult<BudgetDto>> Set(SetBudgetRequest request, CancellationToken cancellationToken) =>
        Ok(await budgetService.SetAsync(request, cancellationToken));
}
