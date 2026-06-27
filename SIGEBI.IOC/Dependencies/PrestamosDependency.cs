using Microsoft.Extensions.DependencyInjection;

// Rutas de Aplicación
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Services.Prestamos;

// Rutas de Persistencia (Basado en tu estructura)
using SIGEBI.Persistence.Interfaces.Prestamos;
using SIGEBI.Persistence.Repositories.Prestamos;
using SIGEBI.Persistence.Repositories.PrestamosRepository;

namespace SIGEBI.IOC.Dependencies
{
    public static class PrestamosDependency
    {
        public static void AddPrestamosDependencies(this IServiceCollection services)
        {
            // --- Módulo de Préstamos ---
            // Inyección del Repositorio (Persistencia)
            services.AddScoped<IPrestamoRepository, PrestamoRepository>();
            // Inyección del Servicio (Aplicación)
            services.AddScoped<IPrestamoService, PrestamoService>();

            // --- Módulo de Multas ---
            // Inyección del Repositorio (Persistencia)
            services.AddScoped<IMultaRepository, MultaRepository>();
            // Inyección del Servicio (Aplicación)
            services.AddScoped<IMultaService, MultaService>();
        }
    }
}