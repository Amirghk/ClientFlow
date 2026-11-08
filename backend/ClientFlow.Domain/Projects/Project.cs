using ClientFlow.Domain.Shared;
using ClientFlow.Domain.Sales;
namespace ClientFlow.Domain.Projects;

public enum ProjectStatus
{
    Planned, Active, OnHold, Completed, Cancelled
}
public enum TaskStatus
{
    ToDo, InProgress, Done
}
public enum TaskPriority
{
    Low, Medium, High, Urgent
}

public sealed class Project : WorkspaceRecord
{
    private Project()
    {
    }
    public Project(Guid organizationId, Guid companyId)
    {
        OrganizationId = organizationId;
        CompanyId = companyId;
    }
    public Guid CompanyId
    {
        get; private set;
    }
    public Guid? SourceOpportunityId
    {
        get; private set;
    }
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public ProjectStatus Status
    {
        get; private set;
    }
    public DateOnly? StartDate
    {
        get; private set;
    }
    public DateOnly? DueDate
    {
        get; private set;
    }

    public static Project FromOpportunity(Opportunity opportunity)
    {
        opportunity.RequireConvertible();

        return new Project(opportunity.OrganizationId, opportunity.CompanyId)
        {
            Name = opportunity.Name,
            SourceOpportunityId = opportunity.Id
        };
    }

    public void Update(Guid companyId, string name, string? description, string status, DateOnly? startDate, DateOnly? dueDate, Guid version)
    {
        CheckVersion(version);
        RequireActive();
        if (startDate > dueDate)
        {
            throw new BusinessRuleException("Due date cannot precede start date.");
        }

        CompanyId = companyId;
        Name = Rules.Text(name, "Name");
        Description = Rules.Text(description, "Description", 4000, false);
        Status = Rules.EnumValue<ProjectStatus>(status);
        StartDate = startDate;
        DueDate = dueDate;
        Touch();
    }
}

public sealed class ProjectTask : WorkspaceRecord
{
    private ProjectTask()
    {
    }
    public ProjectTask(Guid organizationId, Guid projectId)
    {
        OrganizationId = organizationId;
        ProjectId = projectId;
    }
    public Guid ProjectId
    {
        get; private set;
    }
    public Guid? AssignedToUserId
    {
        get; private set;
    }
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public TaskStatus Status
    {
        get; private set;
    }
    public TaskPriority Priority { get; private set; } = TaskPriority.Medium;
    public DateOnly? DueDate
    {
        get; private set;
    }

    public void Update(string title, string? description, Guid? assignedToUserId, string status, string priority, DateOnly? dueDate, Guid version)
    {
        CheckVersion(version);
        RequireActive();
        Title = Rules.Text(title, "Title");
        Description = Rules.Text(description, "Description", 4000, false);
        AssignedToUserId = assignedToUserId;
        Status = Rules.EnumValue<TaskStatus>(status);
        Priority = Rules.EnumValue<TaskPriority>(priority);
        DueDate = dueDate;
        Touch();
    }

    public void Unassign()
    {
        AssignedToUserId = null;
        Touch();
    }
}
