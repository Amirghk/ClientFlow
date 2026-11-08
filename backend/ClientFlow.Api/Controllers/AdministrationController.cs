using ClientFlow.Application.Workspace;
using ClientFlow.Api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace ClientFlow.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}")]
[EnableRateLimiting(ApiRateLimitSettings.PolicyName)]
public sealed class AdministrationController(WorkspaceService workspace) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(Guid organizationId, CancellationToken ct)
    {

        return Ok(await workspace.DashboardAsync(organizationId, ct));
    }
    [HttpGet("members")]
    public async Task<IActionResult> Members(Guid organizationId, CancellationToken ct)
    {

        return Ok(await workspace.MembersAsync(organizationId, ct));
    }
    [HttpPut("members/{userId:guid}/role")]
    public async Task<IActionResult> Role(Guid organizationId, Guid userId, RoleInput input, CancellationToken ct)
    {
        await workspace.ChangeRoleAsync(organizationId, userId, input, ct);

        return NoContent();
    }
    [HttpPost("members/{userId:guid}/remove")]
    public async Task<IActionResult> Remove(Guid organizationId, Guid userId, VersionInput input, CancellationToken ct)
    {
        await workspace.RemoveMemberAsync(organizationId, userId, input.Version, ct);

        return NoContent();
    }
    [HttpGet("invitations")]
    public async Task<IActionResult> Invitations(Guid organizationId, CancellationToken ct)
    {

        return Ok(await workspace.InvitationsAsync(organizationId, ct));
    }
    [HttpPost("invitations")]
    public async Task<IActionResult> Invite(Guid organizationId, CancellationToken ct)
    {

        return Ok(await workspace.InviteAsync(organizationId, ct));
    }
    [HttpPost("invitations/{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid organizationId, Guid id, VersionInput input, CancellationToken ct)
    {
        await workspace.RevokeAsync(organizationId, id, input.Version, ct);

        return NoContent();
    }
}

