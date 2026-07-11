using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGEBI.IOC.Dependencies;

namespace SIGEBI.IOC.Injection;

public static class DependencyInjection
{
    public static IServiceCollection AddSigebiDependencies(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        services.AddApplicationDependencies();
        services.AddPersistenceDependencies(configureDbContext);
        services.AddInfrastructureDependencies();

        return services;
    }
}
