using Microsoft.Extensions.DependencyInjection;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence;
using SIGEBI.Persistence.Context;
using SIGEBI.Persistence.Repositories.Auditoria;
using SIGEBI.Persistence.Repositories.Catalogo;
using SIGEBI.Persistence.Repositories.Notificaciones;
using SIGEBI.Persistence.Repositories.Prestamos;
using SIGEBI.Persistence.Repositories.Usuarios;

namespace SIGEBI.IOC.Dependencies;

public static class PersistenceDependency
{
    public static IServiceCollection AddPersistenceDependencies(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSqlServerPersistence(connectionString);
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<UsuarioRepository>();
        services.AddScoped<IUsuarioRepository>(provider =>
            provider.GetRequiredService<UsuarioRepository>());
        services.AddScoped<IRepository<Usuario>>(provider =>
            provider.GetRequiredService<UsuarioRepository>());

        services.AddScoped<LibroRepository>();
        services.AddScoped<ILibroRepository>(provider =>
            provider.GetRequiredService<LibroRepository>());
        services.AddScoped<IRepository<Libro>>(provider =>
            provider.GetRequiredService<LibroRepository>());

        services.AddScoped<IInventarioRepository, InventarioRepository>();
        services.AddScoped<IEjemplarRepository, EjemplarRepository>();
        services.AddScoped<SolicitudPrestamoRepository>();
        services.AddScoped<ISolicitudPrestamoRepository>(provider =>
            provider.GetRequiredService<SolicitudPrestamoRepository>());
        services.AddScoped<IRepository<SolicitudPrestamo>>(provider =>
            provider.GetRequiredService<SolicitudPrestamoRepository>());
        services.AddScoped<IPrestamoRepository, PrestamoRepository>();
        services.AddScoped<IMultaRepository, MultaRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();

        services.AddScoped<NotificacionRepository>();
        services.AddScoped<INotificacionRepository>(provider =>
            provider.GetRequiredService<NotificacionRepository>());
        services.AddScoped<IRepository<Notificacion>>(provider =>
            provider.GetRequiredService<NotificacionRepository>());

        services.AddScoped<RolRepository>();
        services.AddScoped<IRepository<Rol>>(provider => provider.GetRequiredService<RolRepository>());

        services.AddScoped<CargoRepository>();
        services.AddScoped<ICargoRepository>(provider => provider.GetRequiredService<CargoRepository>());
        services.AddScoped<IRepository<Cargo>>(provider => provider.GetRequiredService<CargoRepository>());

        services.AddScoped<EmpleadoRepository>();
        services.AddScoped<IEmpleadoRepository>(provider => provider.GetRequiredService<EmpleadoRepository>());
        services.AddScoped<IRepository<Empleado>>(provider => provider.GetRequiredService<EmpleadoRepository>());

        services.AddScoped<AdministradorRepository>();
        services.AddScoped<IAdministradorRepository>(provider =>
            provider.GetRequiredService<AdministradorRepository>());
        services.AddScoped<IRepository<Administrador>>(provider =>
            provider.GetRequiredService<AdministradorRepository>());

        return services;
    }
}
