using Microsoft.Extensions.DependencyInjection;

namespace ClientFlow.Common;

public static class RegisterDependencies
{
    public static IServiceCollection RegisterCommon(this IServiceCollection services)
    {
        // Shared constants currently require no service registrations.

        return services;
    }
}
