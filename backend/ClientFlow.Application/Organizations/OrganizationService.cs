using ClientFlow.Application.Abstractions;
using ClientFlow.Application.Identity;
using ClientFlow.Domain.Organizations;
using ClientFlow.Domain.Shared;
using ClientFlow.Application.Workspace;

namespace ClientFlow.Application.Organizations;

public sealed class OrganizationService(
    IOrganizationRepository organizations,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IWorkspaceRepository workspace)
{
    public async Task<OrganizationResult<OrganizationPage>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page is < 1 or > 100000 || pageSize is < 1 or > 100)
        {

            return new(null, OrganizationFailure.InvalidPagination);
        }

        var result = await organizations.ListForMemberAsync(currentUser.Subject, page, pageSize, cancellationToken);

        return new(result);
    }

    public async Task<OrganizationResult<OrganizationResponse>> CreateAsync(string? name, CancellationToken cancellationToken)
    {
        OrganizationName validatedName;

        try
        {
            validatedName = OrganizationName.Create(name);
        }
        catch (InvalidOrganizationNameException)
        {

            return new(null, OrganizationFailure.InvalidName);
        }

        var user = await users.GetOrCreateAsync(currentUser.Subject, cancellationToken);
        var organization = new Organization(validatedName, user.Id);
        organizations.Add(organization);
        workspace.AddActivity(new ActivityEntry(organization.Id, user.Id, user.DisplayName, "created", "organizations", organization.Id, "Organization created"));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new(ToResponse(organization, user.Id));
    }

    public async Task<OrganizationResult<OrganizationResponse>> GetAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var access = await organizations.GetForMemberAsync(organizationId, currentUser.Subject, cancellationToken);

        return access is null
            ? new(null, OrganizationFailure.NotFound)
            : new(ToResponse(access.Organization, access.UserId));
    }

    public async Task<OrganizationResult<OrganizationResponse>> RenameAsync(Guid organizationId, string? name, CancellationToken cancellationToken, Guid? version = null)
    {
        var access = await organizations.GetForMemberAsync(organizationId, currentUser.Subject, cancellationToken);

        if (access is null)
        {

            return new(null, OrganizationFailure.NotFound);
        }

        try
        {
            if (access.Organization.RoleFor(access.UserId) != OrganizationRole.Admin)
            {
                throw new OrganizationAccessDeniedException();
            }

            if (!version.HasValue)
            {
                throw new BusinessRuleException("The current organization version is required. Reload and retry.");
            }

            access.Organization.CheckVersion(version.Value);
            access.Organization.Rename(name!, access.UserId);
        }
        catch (OrganizationAccessDeniedException)
        {

            return new(null, OrganizationFailure.Forbidden);
        }
        catch (InvalidOrganizationNameException)
        {

            return new(null, OrganizationFailure.InvalidName);
        }

        var actor = await workspace.UserAsync(access.UserId, cancellationToken);
        workspace.AddActivity(new ActivityEntry(organizationId, actor.Id, actor.DisplayName, "renamed", "organizations", organizationId, "Organization renamed"));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new(ToResponse(access.Organization, access.UserId));
    }

    private static OrganizationResponse ToResponse(Organization organization, Guid userId)
    {

        return new(organization.Id, organization.Name, organization.CreatedAt, organization.RoleFor(userId)!.Value.ToString(), organization.Version);
    }
}
