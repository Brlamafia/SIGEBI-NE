using Microsoft.Extensions.DependencyInjection;
using SIGEBI.Application.Interfaces;
using SIGEBI.Application.Services.Prestamos;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Repositories.Catalogo;

namespace SIGEBI.IOC.Dependencies
{
    public static class CatalogoDependency
    {
        public static void AddCatalogoDependencies(this IServiceCollection services)
        {
            // Inyección del Repositorio (Persistencia)
            services.AddScoped<ILibroRepository, LibroRepository>();

            // Inyección del Servicio (Aplicación)
            services.AddScoped<ILibroService, LibroService>();
        }
    }
}