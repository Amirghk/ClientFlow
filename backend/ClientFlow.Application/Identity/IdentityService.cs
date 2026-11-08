using ClientFlow.Application.Abstractions;

namespace ClientFlow.Application.Identity;

public sealed class IdentityService(IUserRepository users, ICurrentUser currentUser)
{
    public async Task<IdentityResponse> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var subject = currentUser.Subject;
        var userId = await users.FindIdAsync(subject, cancellationToken);

        return new IdentityResponse(userId, subject);
    }
}

public sealed record IdentityResponse(Guid? UserId, string Subject);
