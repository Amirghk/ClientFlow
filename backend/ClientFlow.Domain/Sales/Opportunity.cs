using ClientFlow.Domain.Shared;
namespace ClientFlow.Domain.Sales;

public enum OpportunityStage
{
    New, Qualified, Proposal, Negotiation, Won, Lost
}

public sealed class Opportunity : WorkspaceRecord
{
    private Opportunity()
    {
    }
    public Opportunity(Guid organizationId, Guid companyId)
    {
        OrganizationId = organizationId;
        CompanyId = companyId;
    }
    public Guid CompanyId
    {
        get; private set;
    }
    public Guid? ContactId
    {
        get; private set;
    }
    public string Name { get; private set; } = "";
    public decimal Value
    {
        get; private set;
    }
    public string Currency { get; private set; } = "USD";
    public OpportunityStage Stage
    {
        get; private set;
    }
    public DateOnly? ExpectedCloseDate
    {
        get; private set;
    }

    public void Update(Guid companyId, Guid? contactId, string name, decimal value, string currency, DateOnly? closeDate, Guid version)
    {
        CheckVersion(version);
        RequireActive();
        if (value < 0 || value > 999999999999.99m || decimal.Round(value, 2) != value)
        {
            throw new BusinessRuleException("Value must be non-negative with at most two decimal places.");
        }

        var code = Rules.Text(currency, "Currency", 3).ToUpperInvariant();
        if (code.Length != 3 || code.Any(c => c < 'A' || c > 'Z'))
        {
            throw new BusinessRuleException("Use a three-letter currency code.");
        }

        CompanyId = companyId;
        ContactId = contactId;
        Name = Rules.Text(name, "Name");
        Value = value;
        Currency = code;
        ExpectedCloseDate = closeDate;
        Touch();
    }

    public void ChangeStage(string stage, Guid version)
    {
        CheckVersion(version);
        RequireActive();
        Stage = Rules.EnumValue<OpportunityStage>(stage);
        Touch();
    }

    public void RequireConvertible()
    {
        RequireActive();
        if (Stage != OpportunityStage.Won)
        {
            throw new BusinessRuleException("Only won opportunities can become projects.");
        }
    }
}
