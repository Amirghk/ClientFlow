using ClientFlow.Domain.Organizations;

namespace ClientFlow.Application.Organizations;

public sealed record OrganizationResponse(Guid Id, string Name, DateTimeOffset CreatedAt, string Role, Guid Version);
public sealed record OrganizationPage(IReadOnlyList<OrganizationResponse> Items, int Page, int PageSize, int Total);
public sealed record OrganizationAccess(Organization Organization, Guid UserId);

public enum OrganizationFailure
{
    NotFound, Forbidden, InvalidName, InvalidPagination
}

public sealed record OrganizationResult<T>(T? Value, OrganizationFailure? Failure = null) where T : class;
