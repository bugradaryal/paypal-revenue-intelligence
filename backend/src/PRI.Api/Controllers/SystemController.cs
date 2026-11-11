using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using PRI.Shared.Constants;
using PRI.Shared.Models;

namespace PRI.Api.Controllers;

[Route(ApiRoutes.Base + "/system")]
public sealed class SystemController(IHostEnvironment environment, TimeProvider timeProvider) : ApiControllerBase
{
    /// <summary>Bağlantı ve CORS testi için.</summary>
    [HttpGet("ping")]
    public ActionResult<PingResponse> Ping()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

        return Ok(new PingResponse(
            "PayPal Revenue Intelligence API",
            version,
            environment.EnvironmentName,
            PayPalEnvironment.Sandbox,
            timeProvider.GetUtcNow().UtcDateTime));
    }
}
