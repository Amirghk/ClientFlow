using Microsoft.Extensions.DependencyInjection;

namespace ClientFlow.Domain;

public static class RegisterDependencies
{
    public static IServiceCollection RegisterDomain(this IServiceCollection services)
    {
        // Entities are created by use cases, not by the container. No domain services yet.

        return services;
    }
}
