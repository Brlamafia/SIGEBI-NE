using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgreSqlPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "La cadena de conexion de Supabase es obligatoria.");
        }

        services.AddDbContext<SigebiContext>(options =>
        {
            options.UseNpgsql(
                    connectionString,
                    postgreSqlOptions => postgreSqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null))
                .ConfigureWarnings(warnings => warnings.Ignore(
                    RelationalEventId.ForeignKeyPropertiesMappedToUnrelatedTables));
        });

        return services;
    }
}
