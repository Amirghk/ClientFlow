using ClientFlow.Domain.Projects;
using ClientFlow.Domain.Shared;
namespace ClientFlow.Application.Workspace;

public sealed partial class WorkspaceService
{
    public async Task<Page<Project>> ProjectsAsync(Guid org, WorkspaceQuery query, CancellationToken ct)
    {
        await Access(org, ct);
        query.Validate();
        if (!string.IsNullOrEmpty(query.Status))
        {
            Rules.EnumValue<ProjectStatus>(query.Status);
        }

        return await store.ProjectsAsync(org, query, ct);
    }
    public async Task<ProjectDetails> ProjectAsync(Guid org, Guid id, CancellationToken ct)
    {
        await Access(org, ct);
        var project = await store.ProjectAsync(org, id, ct) ?? throw new ResourceNotFoundException();

        return await store.ProjectDetailsAsync(project, ct);
    }
    public async Task<Project> SaveProjectAsync(Guid org, Guid? id, ProjectInput input, CancellationToken ct)
    {
        var access = await Access(org, ct);
        var project = id.HasValue ? await store.ProjectAsync(org, id.Value, ct) ?? throw new ResourceNotFoundException() : new Project(org, input.CompanyId);
        await RequireCompany(org, input.CompanyId, ct, !id.HasValue || input.CompanyId != project.CompanyId);
        project.Update(input.CompanyId, input.Name, input.Description, input.Status, input.StartDate, input.DueDate, id.HasValue ? input.Version : project.Version);
        if (!id.HasValue)
        {
            store.AddProject(project);
        }

        await Commit(access, id.HasValue ? "updated" : "created", "projects", project.Id, project.Name, ct);

        return project;
    }
    public async Task<Page<ProjectTask>> TasksAsync(Guid org, WorkspaceQuery query, CancellationToken ct)
    {
        await Access(org, ct);
        query.Validate();
        if (!string.IsNullOrEmpty(query.Status))
        {
            Rules.EnumValue<ClientFlow.Domain.Projects.TaskStatus>(query.Status);
        }

        return await store.TasksAsync(org, query, ct);
    }
    public async Task<ProjectTask> TaskAsync(Guid org, Guid id, CancellationToken ct)
    {
        await Access(org, ct);

        return await store.TaskAsync(org, id, ct) ?? throw new ResourceNotFoundException();
    }
    public async Task<ProjectTask> SaveTaskAsync(Guid org, Guid projectId, Guid? id, TaskInput input, CancellationToken ct)
    {
        var access = await Access(org, ct);
        var project = await store.ProjectAsync(org, projectId, ct) ?? throw new ResourceNotFoundException();
        if (!id.HasValue)
        {
            project.RequireActive();
        }

        var task = id.HasValue ? await store.TaskAsync(org, id.Value, ct) ?? throw new ResourceNotFoundException() : new ProjectTask(org, projectId);
        if (task.ProjectId != projectId)
        {
            throw new ResourceNotFoundException();
        }

        if (input.AssignedToUserId is { } assignee && access.Organization.RoleFor(assignee) is null)
        {
            throw new BusinessRuleException("Assignee must be an organization member.");
        }

        task.Update(input.Title, input.Description, input.AssignedToUserId, input.Status, input.Priority, input.DueDate, id.HasValue ? input.Version : task.Version);
        if (!id.HasValue)
        {
            store.AddTask(task);
        }

        await Commit(access, id.HasValue ? "updated" : "created", "tasks", task.Id, task.Title, ct);

        return task;
    }
}
