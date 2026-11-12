using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Api.Extensions;
using PRI.Application.Common.DTOs;
using PRI.Application.Common.Interfaces;
using PRI.Shared.Constants;

namespace PRI.Api.Controllers;

[Authorize(Roles = AuthRoles.Admin)]
[Route(ApiRoutes.Base + "/admin/users")]
public sealed class AdminUsersController(IUserManagementService users) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken cancellationToken) =>
        Ok(await users.ListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(
        CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await users.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<UserDto>> Activate(Guid id, CancellationToken cancellationToken) =>
        Ok(await users.ActivateAsync(id, cancellationToken));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<UserDto>> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Ok(await users.DeactivateAsync(id, User.GetUserId(), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await users.DeleteAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
