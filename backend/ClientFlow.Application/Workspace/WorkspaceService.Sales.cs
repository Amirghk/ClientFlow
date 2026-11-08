using ClientFlow.Domain.Sales;
using ClientFlow.Domain.Projects;
using ClientFlow.Domain.Shared;
namespace ClientFlow.Application.Workspace;

public sealed partial class WorkspaceService
{
    public async Task<Page<Opportunity>> OpportunitiesAsync(Guid org, WorkspaceQuery query, CancellationToken ct)
    {
        await Access(org, ct);
        query.Validate();
        if (!string.IsNullOrEmpty(query.Status))
        {
            Rules.EnumValue<OpportunityStage>(query.Status);
        }

        return await store.OpportunitiesAsync(org, query, ct);
    }
    public async Task<Opportunity> OpportunityAsync(Guid org, Guid id, CancellationToken ct)
    {
        await Access(org, ct);

        return await store.OpportunityAsync(org, id, ct) ?? throw new ResourceNotFoundException();
    }
    public async Task<Opportunity> SaveOpportunityAsync(Guid org, Guid? id, OpportunityInput input, CancellationToken ct)
    {
        var access = await Access(org, ct);
        var item = id.HasValue ? await store.OpportunityAsync(org, id.Value, ct) ?? throw new ResourceNotFoundException() : new Opportunity(org, input.CompanyId);
        await RequireCompany(org, input.CompanyId, ct, !id.HasValue || input.CompanyId != item.CompanyId);
        if (input.ContactId is { } contactId)
        {
            var contact = await store.ContactAsync(org, contactId, ct) ?? throw new ResourceNotFoundException();
            if (contact.CompanyId != input.CompanyId)
            {
                throw new BusinessRuleException("Contact must belong to the selected company.");
            }

            if (!id.HasValue || contactId != item.ContactId)
            {
                contact.RequireActive();
            }
        }
        item.Update(input.CompanyId, input.ContactId, input.Name, input.Value, input.Currency, input.ExpectedCloseDate, id.HasValue ? input.Version : item.Version);
        if (!id.HasValue)
        {
            store.AddOpportunity(item);
        }

        await Commit(access, id.HasValue ? "updated" : "created", "opportunities", item.Id, item.Name, ct);

        return item;
    }
    public async Task<Opportunity> ChangeStageAsync(Guid org, Guid id, StageInput input, CancellationToken ct)
    {
        var access = await Access(org, ct);
        var item = await store.OpportunityAsync(org, id, ct) ?? throw new ResourceNotFoundException();
        item.ChangeStage(input.Stage, input.Version);
        await Commit(access, "stage changed", "opportunities", id, $"{item.Name} → {item.Stage}", ct);

        return item;
    }
    public async Task<Project> ConvertAsync(Guid org, Guid id, Guid version, CancellationToken ct)
    {
        var access = await Access(org, ct);
        var existing = await store.ConvertedProjectAsync(org, id, ct);
        if (existing is not null)
        {

            return existing;
        }
        var opportunity = await store.OpportunityAsync(org, id, ct) ?? throw new ResourceNotFoundException();
        opportunity.CheckVersion(version);
        await RequireCompany(org, opportunity.CompanyId, ct);
        var project = Project.FromOpportunity(opportunity);
        // Touch the source so a simultaneous stage change conflicts with conversion.
        opportunity.ChangeStage(opportunity.Stage.ToString(), version);
        store.AddProject(project);
        await Commit(access, "converted", "projects", project.Id, $"{opportunity.Name} converted to a project", ct);

        return project;
    }
}
