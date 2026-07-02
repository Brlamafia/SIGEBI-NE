using Microsoft.Extensions.DependencyInjection;

// Separación por capas: IOC conoce las abstracciones y conecta sus implementaciones.
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Services.Prestamos;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Services;
using SIGEBI.Persistence;
using SIGEBI.Persistence.Repositories.Auditoria;
using SIGEBI.Persistence.Repositories.Catalogo;
using SIGEBI.Persistence.Repositories.Prestamos;
using SIGEBI.Persistence.Repositories.Usuarios;

namespace SIGEBI.IOC.Dependencies
{
    public static class PrestamosDependency
    {
        public static void AddPrestamosDependencies(this IServiceCollection services)
        {
            // Inyección de Dependencias y DIP: se registra la abstracción, no una dependencia concreta.
            services.AddScoped<IPrestamoRepository, PrestamoRepository>();
            services.AddScoped<IPrestamoService, PrestamoService>();

            // Open/Closed: la implementación puede sustituirse sin cambiar sus consumidores.
            services.AddScoped<IMultaRepository, MultaRepository>();
            services.AddScoped<IMultaService, MultaService>();

            services.AddScoped<IInventarioRepository, InventarioRepository>();
            services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
            services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
            services.AddScoped<IAdministradorRepository, AdministradorRepository>();
            services.AddScoped<ICargoRepository, CargoRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<PrestamoDomainService>();
            services.AddScoped<MultaDomainService>();
        }
    }
}
