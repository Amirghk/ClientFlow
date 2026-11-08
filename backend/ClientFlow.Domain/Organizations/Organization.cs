using ClientFlow.Domain.Shared;
namespace ClientFlow.Domain.Organizations;

public sealed class Organization
{
    private readonly List<OrganizationMembership> memberships = [];

    private Organization()
    {
    }

    public Organization(string name, Guid ownerId) : this(OrganizationName.Create(name), ownerId) { }

    public Organization(OrganizationName name, Guid ownerId)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("An organization requires an owner.", nameof(ownerId));
        }

        Name = name.Value;
        memberships.Add(new OrganizationMembership(Id, ownerId, OrganizationRole.Admin));
    }

    public Guid Version { get; private set; } = Guid.NewGuid();

    public void CheckVersion(Guid version)
    {
        if (version != Version)
        {
            throw new StaleRecordException();
        }
    }

    public void ChangeRole(Guid userId, OrganizationRole role, Guid actorId, Guid version)
    {
        RequireAdministrator(actorId);
        CheckVersion(version);
        var member = memberships.SingleOrDefault(x => x.UserId == userId)
            ?? throw new BusinessRuleException("Member not found.");
        if (member.Role == OrganizationRole.Admin && role != OrganizationRole.Admin && memberships.Count(x => x.Role == OrganizationRole.Admin) == 1)
        {
            throw new BusinessRuleException("The last administrator cannot be demoted.");
        }

        member.SetRole(role);
        Version = Guid.NewGuid();
    }

    public void RemoveMember(Guid userId, Guid actorId, Guid version)
    {
        RequireAdministrator(actorId);
        CheckVersion(version);
        var member = memberships.SingleOrDefault(x => x.UserId == userId)
            ?? throw new BusinessRuleException("Member not found.");
        if (member.Role == OrganizationRole.Admin && memberships.Count(x => x.Role == OrganizationRole.Admin) == 1)
        {
            throw new BusinessRuleException("The last administrator cannot be removed.");
        }

        memberships.Remove(member);
        Version = Guid.NewGuid();
    }

    public void JoinByInvitation(Guid userId, Invitation invitation, DateTimeOffset now)
    {
        if (invitation.OrganizationId != Id)
        {
            throw new BusinessRuleException("Invitation belongs to another organization.");
        }

        if (RoleFor(userId) is not null)
        {
            throw new BusinessRuleException("You are already a member.");
        }

        invitation.Accept(now);
        memberships.Add(new OrganizationMembership(Id, userId, OrganizationRole.Member));
        Version = Guid.NewGuid();
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public IReadOnlyCollection<OrganizationMembership> Memberships => memberships.AsReadOnly();

    public OrganizationRole? RoleFor(Guid userId)
    {
        var membership = memberships.SingleOrDefault(x => x.UserId == userId);

        return membership?.Role;
    }

    public void Rename(string name, Guid actorId)
    {
        RequireAdministrator(actorId);
        Name = OrganizationName.Create(name).Value;
        Version = Guid.NewGuid();
    }

    public void AddMember(Guid userId, Guid actorId)
    {
        RequireAdministrator(actorId);

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A member requires a user ID.", nameof(userId));
        }

        if (memberships.Any(x => x.UserId == userId))
        {
            throw new InvalidOperationException("The user is already an organization member.");
        }

        memberships.Add(new OrganizationMembership(Id, userId, OrganizationRole.Member));
        Version = Guid.NewGuid();
    }

    private void RequireAdministrator(Guid actorId)
    {
        if (RoleFor(actorId) != OrganizationRole.Admin)
        {
            throw new OrganizationAccessDeniedException();
        }
    }

}
