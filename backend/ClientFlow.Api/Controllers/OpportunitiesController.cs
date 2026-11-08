using ClientFlow.Application.Workspace;
using ClientFlow.Api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace ClientFlow.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/opportunities")]
[EnableRateLimiting(ApiRateLimitSettings.PolicyName)]
public sealed class OpportunitiesController(WorkspaceService workspace) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid organizationId, [FromQuery] WorkspaceQuery query, CancellationToken ct)
    {
        var page = await workspace.OpportunitiesAsync(organizationId, query, ct);

        return Ok(new
        {
            page.Items,
            page = page.PageNumber,
            page.PageSize,
            page.Total
        });
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid organizationId, Guid id, CancellationToken ct)
    {

        return Ok(await workspace.OpportunityAsync(organizationId, id, ct));
    }
    [HttpPost]
    public async Task<IActionResult> Create(Guid organizationId, OpportunityInput input, CancellationToken ct)
    {
        var item = await workspace.SaveOpportunityAsync(organizationId, null, input, ct);

        return Created($"/api/organizations/{organizationId}/opportunities/{item.Id}", item);
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid organizationId, Guid id, OpportunityInput input, CancellationToken ct)
    {

        return Ok(await workspace.SaveOpportunityAsync(organizationId, id, input, ct));
    }
    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid organizationId, Guid id, VersionInput input, CancellationToken ct)
    {

        return Ok(await workspace.ArchiveAsync(organizationId, "opportunities", id, true, input.Version, ct));
    }
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid organizationId, Guid id, VersionInput input, CancellationToken ct)
    {

        return Ok(await workspace.ArchiveAsync(organizationId, "opportunities", id, false, input.Version, ct));
    }

    [HttpPost("{id:guid}/stage")]
    public async Task<IActionResult> Stage(Guid organizationId, Guid id, StageInput input, CancellationToken ct)
    {

        return Ok(await workspace.ChangeStageAsync(organizationId, id, input, ct));
    }
    [HttpPost("{id:guid}/convert")]
    public async Task<IActionResult> Convert(Guid organizationId, Guid id, VersionInput input, CancellationToken ct)
    {

        return Ok(await workspace.ConvertAsync(organizationId, id, input.Version, ct));
    }
}
