using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Shared.Constants;
using PRI.Shared.Models;

namespace PRI.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Revenue + "/customers")]
public sealed class CustomersController(IRevenueQueries queries) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerDto>>> List(
        [FromQuery] CustomerListQuery query, CancellationToken cancellationToken) =>
        Ok(await queries.ListCustomersAsync(query, cancellationToken));
}
