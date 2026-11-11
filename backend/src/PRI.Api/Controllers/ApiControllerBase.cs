using Microsoft.AspNetCore.Mvc;

namespace PRI.Api.Controllers;

/// <summary>Tüm controller'ların tabanı. Route'lar ApiRoutes.Base ("api/v1") ile başlar.</summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
}
