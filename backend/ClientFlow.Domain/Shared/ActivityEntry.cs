namespace ClientFlow.Domain.Shared;

public sealed class ActivityEntry
{
    private ActivityEntry()
    {
    }
    public ActivityEntry(Guid organizationId, Guid actorId, string actorName, string action, string resourceType, Guid resourceId, string description)
    {
        OrganizationId = organizationId;
        ActorId = actorId;
        ActorName = actorName;
        Action = action;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Description = description;
    }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrganizationId
    {
        get; private set;
    }
    public Guid ActorId
    {
        get; private set;
    }
    public string ActorName { get; private set; } = "";
    public string Action { get; private set; } = "";
    public string ResourceType { get; private set; } = "";
    public Guid ResourceId
    {
        get; private set;
    }
    public string Description { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
}
