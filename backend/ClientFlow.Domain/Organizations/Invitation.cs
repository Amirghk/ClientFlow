using ClientFlow.Domain.Shared;
namespace ClientFlow.Domain.Organizations;

public sealed class Invitation
{
    private Invitation()
    {
    }
    public Invitation(Guid organizationId, string tokenHash)
    {
        OrganizationId = organizationId;
        TokenHash = tokenHash;
    }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrganizationId
    {
        get; private set;
    }
    public string TokenHash { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; private set; } = DateTimeOffset.UtcNow.AddDays(7);
    public Guid Version { get; private set; } = Guid.NewGuid();
    public bool Revoked
    {
        get; private set;
    }
    public DateTimeOffset? AcceptedAt
    {
        get; private set;
    }

    public void Accept(DateTimeOffset now)
    {
        if (Revoked || AcceptedAt is not null || now >= ExpiresAt)
        {
            throw new BusinessRuleException("This invitation is expired, revoked, or already used.");
        }

        AcceptedAt = now;
        Version = Guid.NewGuid();
    }
    public void Revoke(Guid version)
    {
        if (Version != version)
        {
            throw new StaleRecordException();
        }

        Revoked = true;
        Version = Guid.NewGuid();
    }
}
