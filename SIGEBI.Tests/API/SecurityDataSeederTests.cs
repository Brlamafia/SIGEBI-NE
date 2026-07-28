using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGEBI.API.Data;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Tests.API;

public sealed class SecurityDataSeederTests
{
    [Fact]
    public async Task Seed_CreaPermisoYLoAsignaAAdministradoresDeFormaIdempotente()
    {
        var services = new ServiceCollection();
        var options = new DbContextOptionsBuilder<SigebiContext>()
            .UseInMemoryDatabase($"security-{Guid.NewGuid():N}")
            .Options;
        services.AddSingleton(new SigebiContext(options));
        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<SigebiContext>();
            var user = new Usuario(
                "Ana",
                "Administradora",
                "001-TEST",
                "ana@sigebi.test",
                TipoUsuario.Administrativo);
            user.EstablecerContrasenaHash("hash-de-prueba");
            var position = new Cargo("Administración");
            context.AddRange(user, position);
            await context.SaveChangesAsync();
            context.Administradores.Add(new Administrador(user.Id, position.Id));
            await context.SaveChangesAsync();
        }

        await SecurityDataSeeder.SeedAsync(provider);
        await SecurityDataSeeder.SeedAsync(provider);

        await using var verificationScope = provider.CreateAsyncScope();
        var verification =
            verificationScope.ServiceProvider.GetRequiredService<SigebiContext>();
        var administrator = await verification.Usuarios
            .Include(user => user.Roles)
            .ThenInclude(role => role.Permisos)
            .SingleAsync();

        var role = Assert.Single(administrator.Roles);
        Assert.Equal(SecurityDataSeeder.AdministratorRole, role.Nombre);
        var permission = Assert.Single(role.Permisos);
        Assert.Equal(
            SecurityDataSeeder.FullAdministrationPermission,
            permission.Codigo);
        Assert.Single(await verification.Roles.ToArrayAsync());
        Assert.Single(await verification.Permisos.ToArrayAsync());
    }
}
