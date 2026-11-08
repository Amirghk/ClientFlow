using ClientFlow.Application.Workspace;
using ClientFlow.Api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace ClientFlow.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/contacts")]
[EnableRateLimiting(ApiRateLimitSettings.PolicyName)]
public sealed class ContactsController(WorkspaceService workspace) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid organizationId, [FromQuery] WorkspaceQuery query, CancellationToken ct)
    {
        var page = await workspace.ContactsAsync(organizationId, query, ct);

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

        return Ok(await workspace.ContactAsync(organizationId, id, ct));
    }
    [HttpPost]
    public async Task<IActionResult> Create(Guid organizationId, ContactInput input, CancellationToken ct)
    {
        var item = await workspace.SaveContactAsync(organizationId, null, input, ct);

        return Created($"/api/organizations/{organizationId}/contacts/{item.Id}", item);
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid organizationId, Guid id, ContactInput input, CancellationToken ct)
    {

        return Ok(await workspace.SaveContactAsync(organizationId, id, input, ct));
    }
    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid organizationId, Guid id, VersionInput input, CancellationToken ct)
    {

        return Ok(await workspace.ArchiveAsync(organizationId, "contacts", id, true, input.Version, ct));
    }
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid organizationId, Guid id, VersionInput input, CancellationToken ct)
    {

        return Ok(await workspace.ArchiveAsync(organizationId, "contacts", id, false, input.Version, ct));
    }
}
