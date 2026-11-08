using ClientFlow.Application.Abstractions;
using ClientFlow.Domain.Identity;
using ClientFlow.Domain.Organizations;
using ClientFlow.Domain.Crm;
using ClientFlow.Domain.Projects;
using ClientFlow.Domain.Sales;
using ClientFlow.Domain.Shared;
using ClientFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClientFlow.Tests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CLIENTFLOW_TEST_POSTGRES")))
        {
            Skip = "Requires CLIENTFLOW_TEST_POSTGRES and a PostgreSQL engine.";
        }
    }
}

public sealed class PostgresConcurrencyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [PostgresFact]
    public async Task Two_admin_demotions_from_the_same_snapshot_cannot_remove_all_administrators()
    {
        using var seed = factory.Services.CreateScope();
        var db = seed.ServiceProvider.GetRequiredService<ClientFlowDbContext>();
        var first = new AppUser("auth0|" + Guid.NewGuid());
        var second = new AppUser("auth0|" + Guid.NewGuid());
        var org = new Organization("Concurrent", first.Id);
        org.AddMember(second.Id, first.Id);
        org.ChangeRole(second.Id, OrganizationRole.Admin, first.Id, org.Version);
        db.AddRange(first, second, org);
        await db.SaveChangesAsync();
        using var scopeA = factory.Services.CreateScope();
        using var scopeB = factory.Services.CreateScope();
        var a = scopeA.ServiceProvider.GetRequiredService<ClientFlowDbContext>();
        var b = scopeB.ServiceProvider.GetRequiredService<ClientFlowDbContext>();
        var orgA = await a.Organizations.Include(x => x.Memberships).SingleAsync(x => x.Id == org.Id);
        var orgB = await b.Organizations.Include(x => x.Memberships).SingleAsync(x => x.Id == org.Id);
        orgA.ChangeRole(first.Id, OrganizationRole.Member, first.Id, orgA.Version);
        orgB.ChangeRole(second.Id, OrganizationRole.Member, second.Id, orgB.Version);

        await ((IUnitOfWork)a).SaveChangesAsync(default);
        await Assert.ThrowsAsync<StaleRecordException>(() => ((IUnitOfWork)b).SaveChangesAsync(default));
        Assert.Equal(1, await db.Memberships.CountAsync(x => x.OrganizationId == org.Id && x.Role == OrganizationRole.Admin));
    }

    [PostgresFact]
    public async Task Conversion_uniqueness_and_invitation_consumption_are_enforced_by_postgres()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClientFlowDbContext>();
        var owner = new AppUser("auth0|" + Guid.NewGuid());
        var org = new Organization("Transactions", owner.Id);
        var company = new Company(org.Id, "Company");
        var opportunity = new Opportunity(org.Id, company.Id);
        opportunity.Update(company.Id, null, "Deal", 100, "USD", null, opportunity.Version);
        opportunity.ChangeStage("Won", opportunity.Version);
        var invitation = new Invitation(org.Id, Guid.NewGuid().ToString("N"));
        db.AddRange(owner, org, company, opportunity, invitation, Project.FromOpportunity(opportunity));
        await db.SaveChangesAsync();
        using var duplicateScope = factory.Services.CreateScope();
        var duplicateDb = duplicateScope.ServiceProvider.GetRequiredService<ClientFlowDbContext>();
        duplicateDb.Add(Project.FromOpportunity(opportunity));
        await Assert.ThrowsAsync<StaleRecordException>(() => ((IUnitOfWork)duplicateDb).SaveChangesAsync(default));

        using var consumeA = factory.Services.CreateScope();
        using var consumeB = factory.Services.CreateScope();
        var a = consumeA.ServiceProvider.GetRequiredService<ClientFlowDbContext>();
        var b = consumeB.ServiceProvider.GetRequiredService<ClientFlowDbContext>();
        var inviteA = await a.Set<Invitation>().SingleAsync(x => x.Id == invitation.Id);
        var inviteB = await b.Set<Invitation>().SingleAsync(x => x.Id == invitation.Id);
        inviteA.Accept(DateTimeOffset.UtcNow);
        inviteB.Accept(DateTimeOffset.UtcNow);
        await ((IUnitOfWork)a).SaveChangesAsync(default);
        await Assert.ThrowsAsync<StaleRecordException>(() => ((IUnitOfWork)b).SaveChangesAsync(default));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }
}
