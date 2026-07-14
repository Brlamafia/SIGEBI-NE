using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Persistence.Context;

namespace SIGEBI.API.Data;

internal static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SigebiContext>();

        await context.Database.EnsureCreatedAsync();
        if (await context.Usuarios.AnyAsync())
            return;

        var permiso = new Permiso("Administración completa", "SIGEBI.ADMIN");
        var rol = new Rol("Administrador", "Acceso de demostración para Swagger.");
        rol.AsignarPermiso(permiso);

        var administrador = new Usuario(
            "Administrador",
            "Swagger",
            "000-0000000-0",
            "admin@sigebi.local",
            TipoUsuario.Administrativo);
        administrador.AsignarRol(rol);

        var estudiante = new Usuario(
            "Usuario",
            "Demostración",
            "001-0000000-0",
            "usuario@sigebi.local",
            TipoUsuario.Estudiante);

        var cargo = new Cargo("Bibliotecario");
        context.AddRange(rol, administrador, estudiante, cargo);
        await context.SaveChangesAsync();

        var empleado = new Empleado(administrador.Id, cargo.Id);
        var perfilAdministrador = new Administrador(administrador.Id, cargo.Id);
        var libro = new Libro(
            "Cien años de soledad",
            "Gabriel García Márquez",
            "9780307474728",
            "Novela",
            "Nueva Era");

        context.AddRange(empleado, perfilAdministrador, libro);
        await context.SaveChangesAsync();

        var inventario = new Inventario(libro.Id, 3);
        context.AddRange(
            inventario,
            new Ejemplar(libro.Id, $"LIB-{libro.Id:D4}-001"),
            new Ejemplar(libro.Id, $"LIB-{libro.Id:D4}-002"),
            new Ejemplar(libro.Id, $"LIB-{libro.Id:D4}-003"),
            new SolicitudPrestamo(estudiante.Id, libro.Id),
            new Notificacion(estudiante.Id, "Datos de demostración listos para probar Swagger."));

        await context.SaveChangesAsync();
    }
}
