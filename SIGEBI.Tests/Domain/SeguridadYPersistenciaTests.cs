using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Tests.Domain;

public sealed class SeguridadYPersistenciaTests
{
    [Fact]
    public void Usuario_NormalizaEmailAlCrearYActualizar()
    {
        var usuario = new Usuario(
            "Ana",
            "Pérez",
            "001",
            "  ANA@SIGEBI.TEST  ",
            TipoUsuario.Estudiante);

        Assert.Equal("ana@sigebi.test", usuario.Email);

        usuario.ActualizarContacto("809-000-0000", "  NUEVO@SIGEBI.TEST ");

        Assert.Equal("nuevo@sigebi.test", usuario.Email);
    }

    [Fact]
    public void Usuario_SeBloqueaYReiniciaIntentos()
    {
        var usuario = new Usuario(
            "Ana",
            "Pérez",
            "001",
            "ana@sigebi.test",
            TipoUsuario.Estudiante);

        usuario.RegistrarIntentoFallido(2, TimeSpan.FromMinutes(15));
        Assert.False(usuario.EstaBloqueado(DateTime.UtcNow));

        usuario.RegistrarIntentoFallido(2, TimeSpan.FromMinutes(15));
        Assert.True(usuario.EstaBloqueado(DateTime.UtcNow));
        Assert.Equal(2, usuario.IntentosAccesoFallidos);

        usuario.RegistrarAccesoExitoso();
        Assert.False(usuario.EstaBloqueado(DateTime.UtcNow));
        Assert.Equal(0, usuario.IntentosAccesoFallidos);
        Assert.Null(usuario.BloqueadoHasta);
    }

    [Fact]
    public void ModeloEf_PersisteTipoMultaYTodosLosEstadosDeInventario()
    {
        var options = new DbContextOptionsBuilder<SigebiContext>()
            .UseNpgsql("Host=localhost;Database=sigebi_tests;Username=postgres;Password=postgres")
            .Options;
        using var context = new SigebiContext(options);

        var multa = context.Model.FindEntityType(typeof(Multa))!;
        Assert.NotNull(multa.FindProperty(nameof(Multa.Tipo)));

        var inventario = context.Model.FindEntityType(typeof(Inventario))!;
        Assert.NotNull(inventario.FindProperty(nameof(Inventario.CantidadReservada)));
        Assert.NotNull(inventario.FindProperty(nameof(Inventario.CantidadFueraServicio)));
        Assert.NotNull(inventario.FindProperty(nameof(Inventario.CantidadPerdida)));
        Assert.NotNull(inventario.FindProperty(nameof(Inventario.CantidadDanada)));
    }
}
