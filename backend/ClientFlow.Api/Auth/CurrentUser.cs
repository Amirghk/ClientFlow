using ClientFlow.Application.Abstractions;

namespace ClientFlow.Api.Auth;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string Subject
    {
        get
        {
            var subject = accessor.HttpContext?.User.FindFirst("sub")?.Value;

            return subject ?? throw new InvalidOperationException("An authenticated user is required.");
        }
    }
}
