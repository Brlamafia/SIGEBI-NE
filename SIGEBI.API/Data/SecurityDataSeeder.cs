using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Persistence.Context;

namespace SIGEBI.API.Data;

public static class SecurityDataSeeder
{
    public const string AdministratorRole = "Administrador";
    public const string FullAdministrationPermission = "SIGEBI.ADMIN";

    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SigebiContext>();

        var permission = await context.Permisos
            .SingleOrDefaultAsync(
                item => item.Codigo == FullAdministrationPermission,
                cancellationToken);
        if (permission is null)
        {
            permission = new Permiso(
                "Administración completa",
                FullAdministrationPermission);
            context.Permisos.Add(permission);
        }

        var role = await context.Roles
            .Include(item => item.Permisos)
            .SingleOrDefaultAsync(
                item => item.Nombre == AdministratorRole,
                cancellationToken);
        if (role is null)
        {
            role = new Rol(
                AdministratorRole,
                "Administración integral de seguridad y configuración.");
            context.Roles.Add(role);
        }

        role.AsignarPermiso(permission);

        var administratorUserIds = await context.Administradores
            .Select(item => item.UsuarioId)
            .ToArrayAsync(cancellationToken);
        if (administratorUserIds.Length > 0)
        {
            var administrators = await context.Usuarios
                .Include(item => item.Roles)
                .Where(item => administratorUserIds.Contains(item.Id))
                .ToArrayAsync(cancellationToken);
            foreach (var administrator in administrators)
                administrator.AsignarRol(role);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
