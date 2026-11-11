using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Shared.Constants;
using PRI.Shared.Models;

namespace PRI.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Revenue + "/invoices")]
public sealed class InvoicesController(IRevenueQueries queries) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> List(
        [FromQuery] InvoiceListQuery query, CancellationToken cancellationToken) =>
        Ok(await queries.ListInvoicesAsync(query, cancellationToken));
}
