using ClientFlow.Application.Abstractions;
using ClientFlow.Application.Identity;
using ClientFlow.Application.Organizations;
using ClientFlow.Domain.Crm;
using ClientFlow.Domain.Projects;
using ClientFlow.Domain.Sales;
using ClientFlow.Domain.Shared;
using ClientFlow.Domain.Organizations;
namespace ClientFlow.Application.Workspace;

public sealed partial class WorkspaceService(IWorkspaceRepository store, IOrganizationRepository organizations,
    IUserRepository users, IUnitOfWork unitOfWork, ICurrentUser currentUser)
{
    private async Task<OrganizationAccess> Access(Guid organizationId, CancellationToken ct, bool admin = false)
    {
        var access = await organizations.GetForMemberAsync(organizationId, currentUser.Subject, ct)
            ?? throw new ResourceNotFoundException();
        if (admin && access.Organization.RoleFor(access.UserId) != OrganizationRole.Admin)
        {
            throw new AccessForbiddenException();
        }

        return access;
    }

    private async Task Commit(OrganizationAccess access, string action, string type, Guid id, string description, CancellationToken ct)
    {
        var actor = await store.UserAsync(access.UserId, ct);
        store.AddActivity(new ActivityEntry(access.Organization.Id, actor.Id, actor.DisplayName, action, type, id, description));
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Company> RequireCompany(Guid org, Guid id, CancellationToken ct, bool active = true)
    {
        var company = await store.CompanyAsync(org, id, ct) ?? throw new ResourceNotFoundException();
        if (active)
        {
            company.RequireActive();
        }

        return company;
    }

    public async Task<object> ArchiveAsync(Guid org, string type, Guid id, bool archived, Guid version, CancellationToken ct)
    {
        var access = await Access(org, ct);
        WorkspaceRecord record = type switch
        {
            "companies" => await store.CompanyAsync(org, id, ct) ?? throw new ResourceNotFoundException(),
            "contacts" => await store.ContactAsync(org, id, ct) ?? throw new ResourceNotFoundException(),
            "opportunities" => await store.OpportunityAsync(org, id, ct) ?? throw new ResourceNotFoundException(),
            "projects" => await store.ProjectAsync(org, id, ct) ?? throw new ResourceNotFoundException(),
            "tasks" => await store.TaskAsync(org, id, ct) ?? throw new ResourceNotFoundException(),
            _ => throw new ResourceNotFoundException()
        };
        if (!archived)
        {
            if (record is Contact contact)
            {
                await RequireCompany(org, contact.CompanyId, ct);
            }

            if (record is Opportunity opportunity)
            {
                await RequireCompany(org, opportunity.CompanyId, ct);
            }

            if (record is Project project)
            {
                await RequireCompany(org, project.CompanyId, ct);
            }

            if (record is ProjectTask task)
            {
                var parent = await store.ProjectAsync(org, task.ProjectId, ct) ?? throw new ResourceNotFoundException();
                parent.RequireActive();
            }
        }
        record.SetArchived(archived, version);
        await Commit(access, archived ? "archived" : "restored", type, id, $"{type} record {(archived ? "archived" : "restored")}", ct);

        return record;
    }

    public async Task<Dashboard> DashboardAsync(Guid org, CancellationToken ct)
    {
        await Access(org, ct);

        return await store.DashboardAsync(org, DateOnly.FromDateTime(DateTime.UtcNow), ct);
    }
}
