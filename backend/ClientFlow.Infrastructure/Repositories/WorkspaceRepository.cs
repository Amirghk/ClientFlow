using ClientFlow.Application.Workspace;
using ClientFlow.Domain.Crm;
using ClientFlow.Domain.Identity;
using ClientFlow.Domain.Organizations;
using ClientFlow.Domain.Projects;
using ClientFlow.Domain.Sales;
using ClientFlow.Domain.Shared;
using Microsoft.EntityFrameworkCore;
namespace ClientFlow.Infrastructure.Repositories;

internal sealed class WorkspaceRepository(ClientFlowDbContext db) : IWorkspaceRepository
{
    public Task<Company?> CompanyAsync(Guid org, Guid id, CancellationToken ct) => db.Set<Company>().SingleOrDefaultAsync(x => x.OrganizationId == org && x.Id == id, ct);
    public Task<Contact?> ContactAsync(Guid org, Guid id, CancellationToken ct) => db.Set<Contact>().SingleOrDefaultAsync(x => x.OrganizationId == org && x.Id == id, ct);
    public Task<Opportunity?> OpportunityAsync(Guid org, Guid id, CancellationToken ct) => db.Set<Opportunity>().SingleOrDefaultAsync(x => x.OrganizationId == org && x.Id == id, ct);
    public Task<Project?> ProjectAsync(Guid org, Guid id, CancellationToken ct) => db.Set<Project>().SingleOrDefaultAsync(x => x.OrganizationId == org && x.Id == id, ct);
    public Task<ProjectTask?> TaskAsync(Guid org, Guid id, CancellationToken ct) => db.Set<ProjectTask>().SingleOrDefaultAsync(x => x.OrganizationId == org && x.Id == id, ct);
    public Task<Project?> ConvertedProjectAsync(Guid org, Guid id, CancellationToken ct) => db.Set<Project>().SingleOrDefaultAsync(x => x.OrganizationId == org && x.SourceOpportunityId == id, ct);
    public Task<AppUser> UserAsync(Guid id, CancellationToken ct) => db.Users.SingleAsync(x => x.Id == id, ct);
    public Task<Organization?> OrganizationAsync(Guid id, CancellationToken ct) => db.Organizations.Include(x => x.Memberships).SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Invitation?> InvitationAsync(Guid org, Guid id, CancellationToken ct) => db.Set<Invitation>().SingleOrDefaultAsync(x => x.OrganizationId == org && x.Id == id, ct);
    public Task<Invitation?> InvitationByHashAsync(string hash, CancellationToken ct) => db.Set<Invitation>().SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
    public void AddCompany(Company item) => db.Add(item);
    public void AddContact(Contact item) => db.Add(item);
    public void AddOpportunity(Opportunity item) => db.Add(item);
    public void AddProject(Project item) => db.Add(item);
    public void AddTask(ProjectTask item) => db.Add(item);
    public void AddInvitation(Invitation item) => db.Add(item);
    public void AddActivity(ActivityEntry item) => db.Add(item);

    public async Task<Page<Company>> CompaniesAsync(Guid org, WorkspaceQuery q, CancellationToken ct)
    {
        var items = db.Set<Company>().AsNoTracking().Where(x => x.OrganizationId == org && x.IsArchived == q.Archived);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.ToLowerInvariant();
            items = items.Where(x => x.Name.ToLower().Contains(term) || x.Industry.ToLower().Contains(term));
        }

        return await PageAsync(Sort(items, q, x => x.Name), q, ct);
    }
    public async Task<Page<Contact>> ContactsAsync(Guid org, WorkspaceQuery q, CancellationToken ct)
    {
        var items = db.Set<Contact>().AsNoTracking().Where(x => x.OrganizationId == org && x.IsArchived == q.Archived);
        if (q.CompanyId.HasValue)
        {
            items = items.Where(x => x.CompanyId == q.CompanyId);
        }

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.ToLowerInvariant();
            items = items.Where(x => (x.FirstName + " " + x.LastName).ToLower().Contains(term) || x.Email.ToLower().Contains(term));
        }

        return await PageAsync(Sort(items, q, x => x.FirstName + " " + x.LastName), q, ct);
    }
    public async Task<Page<Opportunity>> OpportunitiesAsync(Guid org, WorkspaceQuery q, CancellationToken ct)
    {
        var items = db.Set<Opportunity>().AsNoTracking().Where(x => x.OrganizationId == org && x.IsArchived == q.Archived);
        if (q.CompanyId.HasValue)
        {
            items = items.Where(x => x.CompanyId == q.CompanyId);
        }

        if (!string.IsNullOrEmpty(q.Status))
        {
            var stage = Rules.EnumValue<OpportunityStage>(q.Status);
            items = items.Where(x => x.Stage == stage);
        }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.ToLowerInvariant();
            items = items.Where(x => x.Name.ToLower().Contains(term));
        }

        return await PageAsync(Sort(items, q, x => x.Name), q, ct);
    }
    public async Task<Page<Project>> ProjectsAsync(Guid org, WorkspaceQuery q, CancellationToken ct)
    {
        var items = db.Set<Project>().AsNoTracking().Where(x => x.OrganizationId == org && x.IsArchived == q.Archived);
        if (q.CompanyId.HasValue)
        {
            items = items.Where(x => x.CompanyId == q.CompanyId);
        }

        if (!string.IsNullOrEmpty(q.Status))
        {
            var status = Rules.EnumValue<ProjectStatus>(q.Status);
            items = items.Where(x => x.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.ToLowerInvariant();
            items = items.Where(x => x.Name.ToLower().Contains(term));
        }

        return await PageAsync(Sort(items, q, x => x.Name), q, ct);
    }
    public async Task<Page<ProjectTask>> TasksAsync(Guid org, WorkspaceQuery q, CancellationToken ct)
    {
        var items = db.Set<ProjectTask>().AsNoTracking().Where(x => x.OrganizationId == org && x.IsArchived == q.Archived);
        if (q.ProjectId.HasValue)
        {
            items = items.Where(x => x.ProjectId == q.ProjectId);
        }

        if (q.AssignedToUserId.HasValue)
        {
            items = items.Where(x => x.AssignedToUserId == q.AssignedToUserId);
        }

        if (!string.IsNullOrEmpty(q.Status))
        {
            var status = Rules.EnumValue<ClientFlow.Domain.Projects.TaskStatus>(q.Status);
            items = items.Where(x => x.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.ToLowerInvariant();
            items = items.Where(x => x.Title.ToLower().Contains(term));
        }

        return await PageAsync(Sort(items, q, x => x.Title), q, ct);
    }
    private static IOrderedQueryable<T> Sort<T>(IQueryable<T> query, WorkspaceQuery q, System.Linq.Expressions.Expression<Func<T, string>> name) where T : WorkspaceRecord
    {

        return (q.Sort switch
        {
            "nameDesc" => query.OrderByDescending(name),
            "newest" => query.OrderByDescending(x => x.CreatedAt),
            _ => query.OrderBy(name)
        }).ThenBy(x => x.Id);
    }
    private static async Task<Page<T>> PageAsync<T>(IQueryable<T> query, WorkspaceQuery q, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);

        return new(items, q.Page, q.PageSize, total);
    }
    public async Task<ProjectDetails> ProjectDetailsAsync(Project project, CancellationToken ct)
    {
        var tasks = db.Set<ProjectTask>().Where(x => x.OrganizationId == project.OrganizationId && x.ProjectId == project.Id && !x.IsArchived);
        var total = await tasks.CountAsync(ct);
        var done = await tasks.CountAsync(x => x.Status == ClientFlow.Domain.Projects.TaskStatus.Done, ct);

        return new(project, total, done, total == 0 ? 0 : (int)Math.Round(100d * done / total));
    }
    public async Task<IReadOnlyList<MemberView>> MembersAsync(Guid org, CancellationToken ct)
    {

        return await (from member in db.Memberships.AsNoTracking()
                      join user in db.Users on member.UserId equals user.Id
                      where member.OrganizationId == org
                      orderby user.DisplayName, user.Id
                      select new MemberView(user.Id, user.DisplayName, member.Role.ToString())).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<InvitationView>> InvitationsAsync(Guid org, CancellationToken ct)
    {

        return await db.Set<Invitation>().AsNoTracking().Where(x => x.OrganizationId == org).OrderByDescending(x => x.CreatedAt).Take(100)
            .Select(x => new InvitationView(x.Id, x.CreatedAt, x.ExpiresAt, x.Revoked, x.AcceptedAt, x.Version)).ToListAsync(ct);
    }
    public async Task UnassignTasksAsync(Guid org, Guid userId, CancellationToken ct)
    {
        var tasks = await db.Set<ProjectTask>().Where(x => x.OrganizationId == org && x.AssignedToUserId == userId).ToListAsync(ct);
        foreach (var task in tasks)
        {
            task.Unassign();
        }
    }
    public async Task<Dashboard> DashboardAsync(Guid org, DateOnly today, CancellationToken ct)
    {
        var companies = await db.Set<Company>().CountAsync(x => x.OrganizationId == org && !x.IsArchived, ct);
        var opportunities = await db.Set<Opportunity>().AsNoTracking().Where(x => x.OrganizationId == org && !x.IsArchived)
            .Select(x => new { x.Stage, x.Value, x.Currency }).ToListAsync(ct);
        var active = opportunities.Count(x => x.Stage is not (OpportunityStage.Won or OpportunityStage.Lost));
        var values = opportunities.GroupBy(x => x.Currency).OrderBy(x => x.Key)
            .Select(g => new CurrencyTotal(g.Key, g.Where(x => x.Stage is not (OpportunityStage.Won or OpportunityStage.Lost)).Sum(x => x.Value), g.Where(x => x.Stage == OpportunityStage.Won).Sum(x => x.Value))).ToList();
        var stages = Enum.GetValues<OpportunityStage>().Select(s => new StageTotal(s.ToString(), opportunities.Count(x => x.Stage == s))).ToList();
        var projects = await db.Set<Project>().CountAsync(x => x.OrganizationId == org && !x.IsArchived && x.Status == ProjectStatus.Active, ct);
        var overdue = await (from task in db.Set<ProjectTask>()
                             join project in db.Set<Project>() on new
                             {
                                 task.OrganizationId,
                                 Id = task.ProjectId
                             } equals new
                             {
                                 project.OrganizationId,
                                 project.Id
                             }
                             where task.OrganizationId == org && !task.IsArchived && !project.IsArchived && project.Status != ProjectStatus.Cancelled && project.Status != ProjectStatus.Completed
                                 && task.Status != ClientFlow.Domain.Projects.TaskStatus.Done && task.DueDate < today
                             select task).CountAsync(ct);
        var activity = await db.Set<ActivityEntry>().AsNoTracking().Where(x => x.OrganizationId == org).OrderByDescending(x => x.CreatedAt).Take(15).ToListAsync(ct);

        return new(companies, active, projects, overdue, values, stages, activity);
    }
}
