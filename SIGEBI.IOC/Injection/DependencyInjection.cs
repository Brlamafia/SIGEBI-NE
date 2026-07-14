using Microsoft.Extensions.DependencyInjection;
using SIGEBI.IOC.Dependencies;

namespace SIGEBI.IOC.Injection;

public static class DependencyInjection
{
    public static IServiceCollection AddSigebiDependencies(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddApplicationDependencies();
        services.AddPersistenceDependencies(connectionString);
        services.AddInfrastructureDependencies();

        return services;
    }
}
