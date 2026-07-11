using Microsoft.Extensions.DependencyInjection;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Inventario;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Services.Auditoria;
using SIGEBI.Application.Services.Inventario;
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
            // Separación entre capas: IOC conecta contratos de Application con Persistence.
            services.AddScoped<IPrestamoRepository, PrestamoRepository>();
            services.AddScoped<IPrestamoService, PrestamoService>();

            // Separación de responsabilidades: multas mantiene sus servicios y repositorios propios.
            services.AddScoped<IMultaRepository, MultaRepository>();
            services.AddScoped<IMultaService, MultaService>();

            services.AddScoped<IInventarioRepository, InventarioRepository>();
            services.AddScoped<IInventarioService, InventarioService>();
            services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
            services.AddScoped<IAuditoriaService, AuditoriaService>();
            services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
            services.AddScoped<IAdministradorRepository, AdministradorRepository>();
            services.AddScoped<ICargoRepository, CargoRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<PrestamoDomainService>();
            services.AddScoped<MultaDomainService>();
        }
    }
}
