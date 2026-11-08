using ClientFlow.Domain.Organizations;

namespace ClientFlow.Application.Organizations;

public interface IOrganizationRepository
{
    Task<OrganizationPage> ListForMemberAsync(string subject, int page, int pageSize, CancellationToken cancellationToken);
    Task<OrganizationAccess?> GetForMemberAsync(Guid organizationId, string subject, CancellationToken cancellationToken);
    void Add(Organization organization);
}
