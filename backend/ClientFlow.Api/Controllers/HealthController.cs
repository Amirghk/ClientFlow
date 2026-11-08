using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClientFlow.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("health")]
public sealed class HealthController(HealthCheckService healthChecks) : ControllerBase
{
    [HttpGet("live")]
    public IActionResult Live()
    {

        return Content("Healthy", "text/plain");
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var report = await healthChecks.CheckHealthAsync(check => check.Tags.Contains("ready"), cancellationToken);

        return new ContentResult
        {
            Content = report.Status.ToString(),
            ContentType = "text/plain",
            StatusCode = report.Status == HealthStatus.Unhealthy
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status200OK
        };
    }
}
