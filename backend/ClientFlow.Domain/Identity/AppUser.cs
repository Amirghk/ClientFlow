using ClientFlow.Domain.Shared;
namespace ClientFlow.Domain.Identity;

public sealed class AppUser
{
    private AppUser()
    {
    }

    public AppUser(string externalIdentityId)
    {
        if (string.IsNullOrWhiteSpace(externalIdentityId) || externalIdentityId.Length > 255)
        {
            throw new ArgumentException("A valid external identity is required.", nameof(externalIdentityId));
        }

        ExternalIdentityId = externalIdentityId;
    }

    public string DisplayName { get; private set; } = "Team member";
    public Guid Version { get; private set; } = Guid.NewGuid();

    public void UpdateProfile(string displayName, Guid version)
    {
        if (Version != version)
        {
            throw new StaleRecordException();
        }

        DisplayName = Rules.Text(displayName, "Display name", 100);
        Version = Guid.NewGuid();
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string ExternalIdentityId { get; private set; } = string.Empty;
}
