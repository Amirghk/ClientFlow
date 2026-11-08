namespace ClientFlow.Domain.Organizations;

public enum OrganizationRole
{
    Admin, Member
}

public sealed class OrganizationMembership
{
    private OrganizationMembership()
    {
    }

    internal OrganizationMembership(Guid organizationId, Guid userId, OrganizationRole role)
    {
        OrganizationId = organizationId;
        UserId = userId;
        Role = role;
    }

    internal void SetRole(OrganizationRole role)
    {
        Role = role;
    }

    public Guid OrganizationId
    {
        get; private set;
    }
    public Guid UserId
    {
        get; private set;
    }
    public OrganizationRole Role
    {
        get; private set;
    }
}
