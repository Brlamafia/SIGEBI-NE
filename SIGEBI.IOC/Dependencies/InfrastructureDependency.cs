using Microsoft.Extensions.DependencyInjection;

namespace SIGEBI.IOC.Dependencies;

public static class InfrastructureDependency
{
    public static IServiceCollection AddInfrastructureDependencies(this IServiceCollection services)
    {
        // Los servicios externos se registrarán aquí cuando existan sus implementaciones.
        return services;
    }
}
