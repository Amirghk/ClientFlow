using ClientFlow.Application.Identity;
using ClientFlow.Application.Organizations;
using Microsoft.Extensions.DependencyInjection;

namespace ClientFlow.Application;

public static class RegisterDependencies
{
    public static IServiceCollection RegisterApplication(this IServiceCollection services)
    {
        services.AddScoped<ClientFlow.Application.Workspace.WorkspaceService>();
        services.AddScoped<IdentityService>();
        services.AddScoped<OrganizationService>();

        return services;
    }
}
