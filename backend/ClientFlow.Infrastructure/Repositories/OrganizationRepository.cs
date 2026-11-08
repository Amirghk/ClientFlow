using ClientFlow.Application.Organizations;
using ClientFlow.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ClientFlow.Infrastructure.Repositories;

internal sealed class OrganizationRepository(ClientFlowDbContext db) : IOrganizationRepository
{
    public async Task<OrganizationPage> ListForMemberAsync(string subject, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = from membership in MembershipsFor(subject)
                    join organization in db.Organizations on membership.OrganizationId equals organization.Id
                    select new
                    {
                        Organization = organization,
                        membership.Role
                    };

        var total = await query.CountAsync(cancellationToken);
        var items = await query.AsNoTracking()
            .OrderBy(x => x.Organization.Name).ThenBy(x => x.Organization.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new OrganizationResponse(x.Organization.Id, x.Organization.Name,
                x.Organization.CreatedAt, x.Role.ToString(), x.Organization.Version))
            .ToListAsync(cancellationToken);

        return new OrganizationPage(items, page, pageSize, total);
    }

    public async Task<OrganizationAccess?> GetForMemberAsync(Guid organizationId, string subject, CancellationToken cancellationToken)
    {
        var query = from membership in MembershipsFor(subject)
                    join organization in db.Organizations.Include(x => x.Memberships)
                        on membership.OrganizationId equals organization.Id
                    where organization.Id == organizationId
                    select new
                    {
                        Organization = organization,
                        membership.UserId
                    };
        var access = await query.SingleOrDefaultAsync(cancellationToken);

        return access is null ? null : new OrganizationAccess(access.Organization, access.UserId);
    }

    public void Add(Organization organization)
    {
        db.Organizations.Add(organization);
    }

    private IQueryable<OrganizationMembership> MembershipsFor(string subject)
    {

        return from membership in db.Memberships
               join user in db.Users on membership.UserId equals user.Id
               where user.ExternalIdentityId == subject
               select membership;
    }
}
