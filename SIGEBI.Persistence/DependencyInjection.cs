using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddSqlServerPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "La cadena de conexion de NELibrary es obligatoria.");
        }

        services.AddDbContext<SigebiContext>(options =>
        {
            options.UseSqlServer(
                    connectionString,
                    sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null))
                .ConfigureWarnings(warnings => warnings.Ignore(
                    RelationalEventId.ForeignKeyPropertiesMappedToUnrelatedTables));
        });

        return services;
    }
}
