using ClientFlow.Domain.Identity;

namespace ClientFlow.Application.Identity;

public interface IUserRepository
{
    Task<Guid?> FindIdAsync(string subject, CancellationToken cancellationToken);
    Task<AppUser> GetOrCreateAsync(string subject, CancellationToken cancellationToken);
}
