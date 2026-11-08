using ClientFlow.Domain.Shared;
namespace ClientFlow.Domain.Crm;

public sealed class Company : WorkspaceRecord
{
    private Company()
    {
    }
    public Company(Guid organizationId, string name)
    {
        OrganizationId = organizationId;
        Name = Rules.Text(name, "Name");
    }
    public string Name { get; private set; } = "";
    public string Industry { get; private set; } = "";
    public string Website { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string Phone { get; private set; } = "";

    public void Update(string name, string? industry, string? website, string? email, string? phone, Guid version)
    {
        CheckVersion(version);
        RequireActive();
        var url = Rules.Text(website, "Website", 500, false);
        if (url.Length > 0 && (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
        {
            throw new BusinessRuleException("Website must be an HTTP or HTTPS URL.");
        }

        Name = Rules.Text(name, "Name");
        Industry = Rules.Text(industry, "Industry", 120, false);
        Website = url;
        Email = Rules.Email(email);
        Phone = Rules.Text(phone, "Phone", 50, false);
        Touch();
    }
}
