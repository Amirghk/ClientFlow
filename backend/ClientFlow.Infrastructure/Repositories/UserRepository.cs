using ClientFlow.Application.Identity;
using ClientFlow.Common.Constants;
using ClientFlow.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClientFlow.Infrastructure.Repositories;

internal sealed class UserRepository(ClientFlowDbContext db) : IUserRepository
{
    public async Task<Guid?> FindIdAsync(string subject, CancellationToken cancellationToken)
    {

        return await db.Users.Where(x => x.ExternalIdentityId == subject)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AppUser> GetOrCreateAsync(string subject, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.ExternalIdentityId == subject, cancellationToken);

        if (user is not null)
        {

            return user;
        }

        user = new AppUser(subject);
        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: DatabaseIndexNames.UsersExternalIdentityId })
        {
            db.Entry(user).State = EntityState.Detached;
            user = await db.Users.SingleAsync(x => x.ExternalIdentityId == subject, cancellationToken);
        }

        return user;
    }
}
