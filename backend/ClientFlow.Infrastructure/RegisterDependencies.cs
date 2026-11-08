using ClientFlow.Application.Abstractions;
using ClientFlow.Application.Identity;
using ClientFlow.Application.Organizations;
using ClientFlow.Infrastructure.Configuration;
using ClientFlow.Infrastructure.Health;
using ClientFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClientFlow.Infrastructure;

public static class RegisterDependencies
{
    public static IServiceCollection RegisterInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseSettings>()
            .Bind(configuration.GetSection(DatabaseSettings.SectionName))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.ClientFlow),
                "Set ConnectionStrings:ClientFlow to your PostgreSQL connection string.")
            .ValidateOnStart();

        services.AddDbContext<ClientFlowDbContext>((provider, options) =>
        {
            var settings = provider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            options.UseNpgsql(settings.ClientFlow);
        });

        services.AddScoped<ClientFlow.Application.Workspace.IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ClientFlowDbContext>());
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

        return services;
    }
}
