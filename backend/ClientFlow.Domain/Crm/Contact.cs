using ClientFlow.Domain.Shared;
namespace ClientFlow.Domain.Crm;

public sealed class Contact : WorkspaceRecord
{
    private Contact()
    {
    }
    public Contact(Guid organizationId, Guid companyId)
    {
        OrganizationId = organizationId;
        CompanyId = companyId;
    }
    public Guid CompanyId
    {
        get; private set;
    }
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public string JobTitle { get; private set; } = "";

    public void Update(Guid companyId, string firstName, string lastName, string? email, string? phone, string? jobTitle, Guid version)
    {
        CheckVersion(version);
        RequireActive();
        if (CompanyId != companyId)
        {
            throw new BusinessRuleException("A contact's company cannot be changed. Archive the contact and create one at the new company to preserve relationship history.");
        }

        FirstName = Rules.Text(firstName, "First name", 80);
        LastName = Rules.Text(lastName, "Last name", 80);
        Email = Rules.Email(email);
        Phone = Rules.Text(phone, "Phone", 50, false);
        JobTitle = Rules.Text(jobTitle, "Job title", 120, false);
        Touch();
    }
}
