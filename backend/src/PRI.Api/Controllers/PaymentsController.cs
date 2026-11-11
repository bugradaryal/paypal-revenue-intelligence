using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Shared.Constants;
using PRI.Shared.Models;

namespace PRI.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Revenue + "/payments")]
public sealed class PaymentsController(IRevenueQueries queries) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PaymentDto>>> List(
        [FromQuery] PaymentListQuery query, CancellationToken cancellationToken) =>
        Ok(await queries.ListPaymentsAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDetailDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await queries.GetPaymentAsync(id, cancellationToken));
}
