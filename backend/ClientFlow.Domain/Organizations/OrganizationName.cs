namespace ClientFlow.Domain.Organizations;

public sealed record OrganizationName
{
    private OrganizationName(string value)
    {
        Value = value;
    }

    public string Value
    {
        get;
    }

    public static OrganizationName Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 160)
        {
            throw new InvalidOrganizationNameException();
        }

        return new OrganizationName(value.Trim());
    }
}
