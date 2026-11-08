using ClientFlow.Api.Configuration;
using ClientFlow.Application.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClientFlow.Api.Controllers;

[ApiController]
[Route("api/me")]
[EnableRateLimiting(ApiRateLimitSettings.PolicyName)]
public sealed class IdentityController(IdentityService identity, ClientFlow.Application.Workspace.WorkspaceService workspace) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Onboard(CancellationToken ct)
    {

        return Ok(await workspace.OnboardAsync(ct));
    }
    [HttpPut]
    public async Task<IActionResult> Profile(ClientFlow.Application.Workspace.ProfileInput input, CancellationToken ct)
    {

        return Ok(await workspace.UpdateProfileAsync(input, ct));
    }

    [HttpGet]
    public async Task<ActionResult<IdentityResponse>> Get(CancellationToken cancellationToken)
    {
        var response = await identity.GetCurrentAsync(cancellationToken);

        return Ok(response);
    }
}
