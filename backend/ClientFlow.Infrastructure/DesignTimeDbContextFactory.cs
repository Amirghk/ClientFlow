using ClientFlow.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClientFlow.Infrastructure;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ClientFlowDbContext>
{
    public ClientFlowDbContext CreateDbContext(string[] args)
    {
        var settings = new DatabaseSettings
        {
            ClientFlow = Environment.GetEnvironmentVariable($"{DatabaseSettings.SectionName}__{nameof(DatabaseSettings.ClientFlow)}")
                ?? "Host=localhost;Port=5432;Database=clientflow;Username=clientflow"
        };

        return new(new DbContextOptionsBuilder<ClientFlowDbContext>().UseNpgsql(settings.ClientFlow).Options);
    }
}
