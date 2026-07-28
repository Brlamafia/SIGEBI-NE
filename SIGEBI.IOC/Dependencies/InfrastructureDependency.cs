using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SIGEBI.Infrastructure.Health;

namespace SIGEBI.IOC.Dependencies;

public static class InfrastructureDependency
{
    public static IServiceCollection AddInfrastructureDependencies(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddHealthChecks()
            .AddCheck<PostgreSqlHealthCheck>("postgresql", tags: ["ready"]);
        return services;
    }
}
