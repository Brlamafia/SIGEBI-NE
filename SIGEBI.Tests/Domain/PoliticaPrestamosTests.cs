using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Policies;

namespace SIGEBI.Tests.Domain;

public class PoliticaPrestamosTests
{
    private readonly PoliticaPrestamos _politica = new();

    [Theory]
    [InlineData(TipoUsuario.Estudiante, 3, 7)]
    [InlineData(TipoUsuario.Docente, 5, 14)]
    [InlineData(TipoUsuario.Administrativo, 3, 7)]
    [InlineData(TipoUsuario.Externo, 1, 3)]
    public void DebeAplicarCondicionesPorTipo(
        TipoUsuario tipo,
        int limiteEsperado,
        int diasEsperados)
    {
        var condiciones = _politica.ObtenerCondiciones(tipo);

        Assert.Equal(limiteEsperado, condiciones.LimitePrestamos);
        Assert.Equal(diasEsperados, condiciones.DiasPrestamo);
    }

    [Fact]
    public void DebeCalcularFechaLimiteSinRecibirlaDelCliente()
    {
        var inicio = new DateTime(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc);

        var limite = _politica.CalcularFechaLimite(TipoUsuario.Docente, inicio);

        Assert.Equal(inicio.AddDays(14), limite);
    }
}
