using System.Security.Cryptography;
using System.Text;
using ClientFlow.Domain.Identity;
using ClientFlow.Domain.Organizations;
using ClientFlow.Domain.Shared;
using ClientFlow.Application.Organizations;
namespace ClientFlow.Application.Workspace;

public sealed partial class WorkspaceService
{
    public async Task<AppUser> OnboardAsync(CancellationToken ct)
    {

        return await users.GetOrCreateAsync(currentUser.Subject, ct);
    }
    public async Task<AppUser> UpdateProfileAsync(ProfileInput input, CancellationToken ct)
    {
        var user = await users.GetOrCreateAsync(currentUser.Subject, ct);
        user.UpdateProfile(input.DisplayName, input.Version);
        await unitOfWork.SaveChangesAsync(ct);

        return user;
    }
    public async Task<IReadOnlyList<MemberView>> MembersAsync(Guid org, CancellationToken ct)
    {
        await Access(org, ct);

        return await store.MembersAsync(org, ct);
    }
    public async Task ChangeRoleAsync(Guid org, Guid userId, RoleInput input, CancellationToken ct)
    {
        var access = await Access(org, ct, true);
        access.Organization.ChangeRole(userId, Rules.EnumValue<OrganizationRole>(input.Role), access.UserId, input.Version);
        await Commit(access, "role changed", "members", userId, $"Member role changed to {input.Role}", ct);
    }
    public async Task RemoveMemberAsync(Guid org, Guid userId, Guid version, CancellationToken ct)
    {
        var access = await Access(org, ct, true);
        access.Organization.RemoveMember(userId, access.UserId, version);
        await store.UnassignTasksAsync(org, userId, ct);
        await Commit(access, "removed", "members", userId, "Member removed; task assignments cleared", ct);
    }
    public async Task<IReadOnlyList<InvitationView>> InvitationsAsync(Guid org, CancellationToken ct)
    {
        await Access(org, ct, true);

        return await store.InvitationsAsync(org, ct);
    }
    public async Task<InvitationCreated> InviteAsync(Guid org, CancellationToken ct)
    {
        var access = await Access(org, ct, true);
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var invitation = new Invitation(org, Hash(token));
        store.AddInvitation(invitation);
        await Commit(access, "created", "invitations", invitation.Id, "Invitation created", ct);

        return new(invitation.Id, token, invitation.ExpiresAt);
    }
    public async Task RevokeAsync(Guid org, Guid id, Guid version, CancellationToken ct)
    {
        var access = await Access(org, ct, true);
        var invitation = await store.InvitationAsync(org, id, ct) ?? throw new ResourceNotFoundException();
        invitation.Revoke(version);
        await Commit(access, "revoked", "invitations", id, "Invitation revoked", ct);
    }
    public async Task<Guid> AcceptInviteAsync(string token, CancellationToken ct)
    {
        if (token.Length != 64 || !token.All(Uri.IsHexDigit))
        {
            throw new BusinessRuleException("Invalid invitation.");
        }

        var invitation = await store.InvitationByHashAsync(Hash(token), ct) ?? throw new ResourceNotFoundException();
        var organization = await store.OrganizationAsync(invitation.OrganizationId, ct) ?? throw new ResourceNotFoundException();
        var user = await users.GetOrCreateAsync(currentUser.Subject, ct);
        organization.JoinByInvitation(user.Id, invitation, DateTimeOffset.UtcNow);
        await Commit(new OrganizationAccess(organization, user.Id), "joined", "members", user.Id, $"{user.DisplayName} joined the organization", ct);

        return organization.Id;
    }
    private static string Hash(string token)
    {

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
