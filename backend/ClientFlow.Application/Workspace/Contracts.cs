using ClientFlow.Domain.Crm;
using ClientFlow.Domain.Projects;
using ClientFlow.Domain.Sales;
using ClientFlow.Domain.Organizations;
using ClientFlow.Domain.Identity;
using ClientFlow.Domain.Shared;
namespace ClientFlow.Application.Workspace;

public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int Total);
public sealed class WorkspaceQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search
    {
        get; set;
    }
    public bool Archived
    {
        get; set;
    }
    public string Sort { get; set; } = "name";
    public string? Status
    {
        get; set;
    }
    public Guid? CompanyId
    {
        get; set;
    }
    public Guid? ProjectId
    {
        get; set;
    }
    public Guid? AssignedToUserId
    {
        get; set;
    }

    public void Validate()
    {
        if (Page is < 1 or > 100000 || PageSize is < 1 or > 100 || Search?.Length > 160)
        {
            throw new BusinessRuleException("Invalid pagination or search: page 1–100000, pageSize 1–100, search at most 160 characters.");
        }

        if (Sort is not ("name" or "nameDesc" or "newest"))
        {
            throw new BusinessRuleException("Unsupported sort order.");
        }
    }
}
public sealed record CompanyInput(string Name, string? Industry, string? Website, string? Email, string? Phone, Guid Version);
public sealed record ContactInput(Guid CompanyId, string FirstName, string LastName, string? Email, string? Phone, string? JobTitle, Guid Version);
public sealed record OpportunityInput(Guid CompanyId, Guid? ContactId, string Name, decimal Value, string Currency, DateOnly? ExpectedCloseDate, Guid Version);
public sealed record ProjectInput(Guid CompanyId, string Name, string? Description, string Status, DateOnly? StartDate, DateOnly? DueDate, Guid Version);
public sealed record TaskInput(string Title, string? Description, Guid? AssignedToUserId, string Status, string Priority, DateOnly? DueDate, Guid Version);
public sealed record VersionInput(Guid Version);
public sealed record StageInput(string Stage, Guid Version);
public sealed record RoleInput(string Role, Guid Version);
public sealed record ProfileInput(string DisplayName, Guid Version);
public sealed record MemberView(Guid UserId, string DisplayName, string Role);
public sealed record InvitationView(Guid Id, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, bool Revoked, DateTimeOffset? AcceptedAt, Guid Version);
public sealed record InvitationCreated(Guid Id, string Token, DateTimeOffset ExpiresAt);
public sealed record ProjectDetails(Project Project, int TotalTasks, int CompletedTasks, int Progress);
public sealed record CurrencyTotal(string Currency, decimal OpenValue, decimal WonValue);
public sealed record StageTotal(string Stage, int Count);
public sealed record Dashboard(int Companies, int ActiveOpportunities, int ActiveProjects, int OverdueTasks,
    IReadOnlyList<CurrencyTotal> Values, IReadOnlyList<StageTotal> Stages, IReadOnlyList<ActivityEntry> Activity);
public sealed class ResourceNotFoundException() : Exception("Resource not found.");
public sealed class AccessForbiddenException() : Exception("Administrator access is required.");

public interface IWorkspaceRepository
{
    Task<Company?> CompanyAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<Contact?> ContactAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<Opportunity?> OpportunityAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<Project?> ProjectAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<ProjectTask?> TaskAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<Project?> ConvertedProjectAsync(Guid organizationId, Guid opportunityId, CancellationToken ct);
    Task<Page<Company>> CompaniesAsync(Guid organizationId, WorkspaceQuery query, CancellationToken ct);
    Task<Page<Contact>> ContactsAsync(Guid organizationId, WorkspaceQuery query, CancellationToken ct);
    Task<Page<Opportunity>> OpportunitiesAsync(Guid organizationId, WorkspaceQuery query, CancellationToken ct);
    Task<Page<Project>> ProjectsAsync(Guid organizationId, WorkspaceQuery query, CancellationToken ct);
    Task<Page<ProjectTask>> TasksAsync(Guid organizationId, WorkspaceQuery query, CancellationToken ct);
    Task<ProjectDetails> ProjectDetailsAsync(Project project, CancellationToken ct);
    Task<Dashboard> DashboardAsync(Guid organizationId, DateOnly today, CancellationToken ct);
    Task<AppUser> UserAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<MemberView>> MembersAsync(Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<InvitationView>> InvitationsAsync(Guid organizationId, CancellationToken ct);
    Task<Invitation?> InvitationAsync(Guid organizationId, Guid id, CancellationToken ct);
    Task<Invitation?> InvitationByHashAsync(string hash, CancellationToken ct);
    Task<Organization?> OrganizationAsync(Guid id, CancellationToken ct);
    Task UnassignTasksAsync(Guid organizationId, Guid userId, CancellationToken ct);
    void AddCompany(Company company);
    void AddContact(Contact contact);
    void AddOpportunity(Opportunity opportunity);
    void AddProject(Project project);
    void AddTask(ProjectTask task);
    void AddInvitation(Invitation invitation);
    void AddActivity(ActivityEntry activity);
}
