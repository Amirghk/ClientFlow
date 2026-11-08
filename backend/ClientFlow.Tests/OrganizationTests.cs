using ClientFlow.Domain.Organizations;

namespace ClientFlow.Tests;

public sealed class OrganizationTests
{
    [Fact]
    public void New_organization_has_its_owner_as_administrator()
    {
        var ownerId = Guid.NewGuid();

        var organization = new Organization("  Agency  ", ownerId);

        Assert.Equal("Agency", organization.Name);
        Assert.Equal(OrganizationRole.Admin, organization.RoleFor(ownerId));
        Assert.Single(organization.Memberships);
    }

    [Fact]
    public void Administrator_can_rename_the_organization()
    {
        var ownerId = Guid.NewGuid();
        var organization = new Organization("Agency", ownerId);

        organization.Rename("  New agency  ", ownerId);

        Assert.Equal("New agency", organization.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Non_administrators_cannot_rename_or_add_members(bool isMember)
    {
        var ownerId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var organization = new Organization("Agency", ownerId);

        if (isMember)
        {
            organization.AddMember(actorId, ownerId);
        }

        Assert.Throws<OrganizationAccessDeniedException>(() => organization.Rename("Changed", actorId));
        Assert.Throws<OrganizationAccessDeniedException>(() => organization.AddMember(Guid.NewGuid(), actorId));
        Assert.Equal("Agency", organization.Name);
    }

    [Fact]
    public void Administrator_adds_a_member_without_granting_admin_rights()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var organization = new Organization("Agency", ownerId);

        organization.AddMember(memberId, ownerId);

        Assert.Equal(OrganizationRole.Member, organization.RoleFor(memberId));
        Assert.Throws<InvalidOperationException>(() => organization.AddMember(memberId, ownerId));
        Assert.Equal(2, organization.Memberships.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Invalid_rename_preserves_existing_name(string? name)
    {
        var ownerId = Guid.NewGuid();
        var organization = new Organization("Agency", ownerId);

        Assert.Throws<InvalidOrganizationNameException>(() => organization.Rename(name!, ownerId));
        Assert.Equal("Agency", organization.Name);
    }

    [Fact]
    public void Name_length_boundary_is_enforced()
    {
        var valid = OrganizationName.Create(new string('x', 160));

        Assert.Equal(160, valid.Value.Length);
        Assert.Throws<InvalidOrganizationNameException>(() => OrganizationName.Create(new string('x', 161)));
    }
}
