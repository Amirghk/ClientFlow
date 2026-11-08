using ClientFlow.Domain.Crm;
using ClientFlow.Domain.Organizations;
using ClientFlow.Domain.Projects;
using ClientFlow.Domain.Sales;
using ClientFlow.Domain.Shared;

namespace ClientFlow.Tests;

public sealed class WorkspaceDomainTests
{
    [Fact]
    public void Conversion_requires_a_won_active_opportunity_and_preserves_its_snapshot()
    {
        var deal = new Opportunity(Guid.NewGuid(), Guid.NewGuid());
        deal.Update(deal.CompanyId, null, "Original scope", 250, "usd", null, deal.Version);
        Assert.Throws<BusinessRuleException>(() => Project.FromOpportunity(deal));
        deal.ChangeStage("Won", deal.Version);
        var project = Project.FromOpportunity(deal);
        deal.Update(deal.CompanyId, null, "Revised scope", 999, "EUR", null, deal.Version);
        deal.ChangeStage("New", deal.Version);

        Assert.Equal("Original scope", project.Name);
        Assert.Equal(deal.Id, project.SourceOpportunityId);
        Assert.Equal(OpportunityStage.New, deal.Stage);
        deal.ChangeStage("Won", deal.Version);
        deal.SetArchived(true, deal.Version);
        Assert.Throws<BusinessRuleException>(() => Project.FromOpportunity(deal));
    }

    [Fact]
    public void Archived_records_cannot_be_edited_and_restore_requires_current_version()
    {
        var company = new Company(Guid.NewGuid(), "Studio");
        var stale = company.Version;
        company.SetArchived(true, stale);

        Assert.Throws<BusinessRuleException>(() => company.Update("Edited", null, null, null, null, company.Version));
        Assert.Throws<StaleRecordException>(() => company.SetArchived(false, stale));
        company.SetArchived(false, company.Version);
        company.Update("Restored", null, null, null, null, company.Version);
        Assert.Equal("Restored", company.Name);
    }

    [Fact]
    public void Invitation_grants_only_member_access_and_is_expiring_and_single_use()
    {
        var organization = new Organization("Studio", Guid.NewGuid());
        var invitation = new Invitation(organization.Id, "hash");
        var member = Guid.NewGuid();
        organization.JoinByInvitation(member, invitation, invitation.CreatedAt);

        Assert.Equal(OrganizationRole.Member, organization.RoleFor(member));
        Assert.Throws<BusinessRuleException>(() => organization.JoinByInvitation(Guid.NewGuid(), invitation, invitation.CreatedAt));
        var expired = new Invitation(organization.Id, "other");
        Assert.Throws<BusinessRuleException>(() => organization.JoinByInvitation(Guid.NewGuid(), expired, expired.ExpiresAt));
        var revoked = new Invitation(organization.Id, "revoked");
        revoked.Revoke(revoked.Version);
        Assert.Throws<BusinessRuleException>(() => organization.JoinByInvitation(Guid.NewGuid(), revoked, revoked.CreatedAt));
    }

    [Fact]
    public void Last_admin_is_protected_and_members_cannot_change_roles()
    {
        var admin = Guid.NewGuid();
        var organization = new Organization("Studio", admin);
        var member = Guid.NewGuid();
        organization.AddMember(member, admin);

        Assert.Throws<BusinessRuleException>(() => organization.RemoveMember(admin, admin, organization.Version));
        Assert.Throws<BusinessRuleException>(() => organization.ChangeRole(admin, OrganizationRole.Member, admin, organization.Version));
        Assert.Throws<OrganizationAccessDeniedException>(() => organization.ChangeRole(member, OrganizationRole.Admin, member, organization.Version));
        var oldVersion = organization.Version;
        organization.ChangeRole(member, OrganizationRole.Admin, admin, oldVersion);
        Assert.Throws<StaleRecordException>(() => organization.RemoveMember(admin, admin, oldVersion));
        organization.RemoveMember(admin, member, organization.Version);
        Assert.Equal(OrganizationRole.Admin, organization.RoleFor(member));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0.001)]
    [InlineData(1000000000000)]
    public void Invalid_monetary_values_are_rejected(decimal value)
    {
        var deal = new Opportunity(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<BusinessRuleException>(() => deal.Update(deal.CompanyId, null, "Deal", value, "USD", null, deal.Version));
    }
}
