namespace ClientFlow.Common.Constants;

public static class DatabaseIndexNames
{
    public const string CompaniesTenant = "IX_Companies_OrganizationId_IsArchived";
    public const string ContactsTenant = "IX_Contacts_OrganizationId_IsArchived";
    public const string OpportunitiesTenant = "IX_Opportunities_OrganizationId_IsArchived";
    public const string ProjectsTenant = "IX_Projects_OrganizationId_IsArchived";
    public const string TasksTenant = "IX_ProjectTasks_OrganizationId_IsArchived";
    public const string ProjectSourceOpportunity = "IX_Projects_OrganizationId_SourceOpportunityId";
    public const string InvitationToken = "IX_Invitations_TokenHash";
    public const string ActivityTenantDate = "IX_ActivityEntries_OrganizationId_CreatedAt";
    public const string InvitationOrganization = "IX_Invitations_OrganizationId";
    public const string OpportunityCompanyContact = "IX_Opportunities_OrganizationId_CompanyId_ContactId";
    public const string ProjectCompany = "IX_Projects_OrganizationId_CompanyId";
    public const string TaskProject = "IX_ProjectTasks_OrganizationId_ProjectId";
    public const string TaskAssignee = "IX_ProjectTasks_OrganizationId_AssignedToUserId";
    public static readonly IReadOnlyDictionary<string, string> Relationships = new Dictionary<string, string>
    {
        ["Invitation:OrganizationId"] = InvitationOrganization,
        ["Opportunity:OrganizationId,CompanyId,ContactId"] = OpportunityCompanyContact,
        ["Project:OrganizationId,CompanyId"] = ProjectCompany,
        ["ProjectTask:OrganizationId,ProjectId"] = TaskProject,
        ["ProjectTask:OrganizationId,AssignedToUserId"] = TaskAssignee
    };

    // Historical migrations use these names. Add new constants for a rename;
    // never change the value of a constant referenced by an existing migration.
    public const string UsersExternalIdentityId = "IX_Users_ExternalIdentityId";
    public const string OrganizationMembershipsUserId = "IX_OrganizationMemberships_UserId";
}
