using ClientFlow.Application.Workspace;
using ClientFlow.Api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace ClientFlow.Api.Controllers;

[ApiController]
[Route("api/invitations")]
[EnableRateLimiting(ApiRateLimitSettings.PolicyName)]
public sealed class InvitationsController(WorkspaceService workspace) : ControllerBase
{
    [HttpPost("accept")]
    public async Task<IActionResult> Accept(AcceptInvitation input, CancellationToken ct)
    {
        var organizationId = await workspace.AcceptInviteAsync(input.Token ?? "", ct);

        return Ok(new
        {
            organizationId
        });
    }
}
public sealed record AcceptInvitation(string? Token);
