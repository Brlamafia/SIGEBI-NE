using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Tests.Persistence;

public sealed class SigebiContextLegacyColumnsTests
{
    [Fact]
    public async Task GuardarEmpleado_SinCargoRastreado_CompletaLaColumnaLegada()
    {
        var options = new DbContextOptionsBuilder<SigebiContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        int cargoId;
        await using (var seedContext = new SigebiContext(options))
        {
            var cargo = new Cargo("Bibliotecario");
            seedContext.Cargos.Add(cargo);
            await seedContext.SaveChangesAsync();
            cargoId = cargo.Id;
        }

        await using var context = new SigebiContext(options);
        var empleado = new Empleado(usuarioId: 7, cargoId: cargoId);
        context.Empleados.Add(empleado);

        await context.SaveChangesAsync();

        Assert.Equal(
            "Bibliotecario",
            context.Entry(empleado).Property<string>("cargo").CurrentValue);
    }

    [Fact]
    public async Task ActualizarEmpleado_ConNuevoCargoNoRastreado_ActualizaLaColumnaLegada()
    {
        var options = new DbContextOptionsBuilder<SigebiContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        int empleadoId;
        int nuevoCargoId;
        await using (var seedContext = new SigebiContext(options))
        {
            var cargoActual = new Cargo("Auxiliar");
            var cargoNuevo = new Cargo("Bibliotecario");
            seedContext.Cargos.AddRange(cargoActual, cargoNuevo);
            await seedContext.SaveChangesAsync();

            var empleado = new Empleado(usuarioId: 7, cargoId: cargoActual.Id);
            seedContext.Empleados.Add(empleado);
            await seedContext.SaveChangesAsync();
            empleadoId = empleado.Id;
            nuevoCargoId = cargoNuevo.Id;
        }

        await using var context = new SigebiContext(options);
        var empleadoGuardado = await context.Empleados.SingleAsync(e => e.Id == empleadoId);
        empleadoGuardado.ActualizarCargo(nuevoCargoId);

        await context.SaveChangesAsync();

        Assert.Equal(
            "Bibliotecario",
            context.Entry(empleadoGuardado).Property<string>("cargo").CurrentValue);
    }
}
