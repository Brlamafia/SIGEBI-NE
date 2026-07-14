using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SIGEBI.Application.Interfaces;
using SIGEBI.Application.Interfaces.Administradores;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Cargos;
using SIGEBI.Application.Interfaces.Catalogo; // <-- Faltaba este using
using SIGEBI.Application.Interfaces.Empleados;
using SIGEBI.Application.Interfaces.Inventario;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Roles;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;
using SIGEBI.Application.Interfaces.Usuarios;
using SIGEBI.Application.Mappings;
using SIGEBI.Application.Services.Administradores;
using SIGEBI.Application.Services.Auditoria;
using SIGEBI.Application.Services.Cargos;
using SIGEBI.Application.Services.Catalogo;
using SIGEBI.Application.Services.Empleados;
using SIGEBI.Application.Services.Inventario;
using SIGEBI.Application.Services.Notificaciones;
using SIGEBI.Application.Services.Prestamos;
using SIGEBI.Application.Services.Roles;
using SIGEBI.Application.Services.Usuarios;
using SIGEBI.Application.Validations;
using SIGEBI.Domain.Policies;
using SIGEBI.Domain.Services;

namespace SIGEBI.IOC.Dependencies
{
    public static class ApplicationDependency
    {
        public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
        {
            services.AddAutoMapper(configuration =>
                configuration.AddMaps(typeof(MappingProfile).Assembly));
            services.AddValidatorsFromAssemblyContaining<SaveRolValidator>();

            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<ILibroService, LibroService>(); // <-- Sintaxis corregida aquí
            services.AddScoped<ISolicitudPrestamoService, SolicitudPrestamoService>();
            services.AddScoped<IPrestamoService, PrestamoService>();
            services.AddScoped<IMultaService, MultaService>();
            services.AddScoped<IInventarioService, InventarioService>();
            services.AddScoped<IAuditoriaService, AuditoriaService>();
            services.AddScoped<IAuditoriaWriter, AuditoriaWriter>();
            services.AddScoped<ICargoService, CargoService>();
            services.AddScoped<IEmpleadoService, EmpleadoService>();
            services.AddScoped<IAdministradorService, AdministradorService>();
            services.AddScoped<INotificacionService, NotificacionService>();
            services.AddScoped<IRolService, RolService>();

            services.AddScoped<PrestamoDomainService>();
            services.AddScoped<MultaDomainService>();
            services.AddSingleton(new PoliticaPrestamos());

            return services;
        }
    }
}
