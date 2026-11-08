namespace ClientFlow.Domain.Organizations;

public sealed class InvalidOrganizationNameException()
    : Exception("Enter an organization name of 1–160 characters.");

public sealed class OrganizationAccessDeniedException()
    : Exception("Only an organization administrator can perform this operation.");
