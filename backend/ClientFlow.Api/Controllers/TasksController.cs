using ClientFlow.Application.Workspace;
using ClientFlow.Api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace ClientFlow.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}")]
[EnableRateLimiting(ApiRateLimitSettings.PolicyName)]
public sealed class TasksController(WorkspaceService workspace) : ControllerBase
{
    [HttpGet("tasks")]
    public async Task<IActionResult> List(Guid organizationId, [FromQuery] WorkspaceQuery query, CancellationToken ct)
    {
        var page = await workspace.TasksAsync(organizationId, query, ct);

        return Ok(new
        {
            page.Items,
            page = page.PageNumber,
            page.PageSize,
            page.Total
        });
    }
    [HttpGet("tasks/{id:guid}")]
    public async Task<IActionResult> Get(Guid organizationId, Guid id, CancellationToken ct)
    {

        return Ok(await workspace.TaskAsync(organizationId, id, ct));
    }
    [HttpPost("projects/{projectId:guid}/tasks")]
    public async Task<IActionResult> Create(Guid organizationId, Guid projectId, TaskInput input, CancellationToken ct)
    {
        var task = await workspace.SaveTaskAsync(organizationId, projectId, null, input, ct);

        return Created($"/api/organizations/{organizationId}/tasks/{task.Id}", task);
    }
    [HttpPut("projects/{projectId:guid}/tasks/{id:guid}")]
    public async Task<IActionResult> Update(Guid organizationId, Guid projectId, Guid id, TaskInput input, CancellationToken ct)
    {

        return Ok(await workspace.SaveTaskAsync(organizationId, projectId, id, input, ct));
    }
    [HttpPost("tasks/{id:guid}/{operation}")]
    public async Task<IActionResult> Archive(Guid organizationId, Guid id, string operation, VersionInput input, CancellationToken ct)
    {
        if (operation is not ("archive" or "restore"))
        {

            return NotFound();
        }

        return Ok(await workspace.ArchiveAsync(organizationId, "tasks", id, operation == "archive", input.Version, ct));
    }
}
