using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;

namespace SIGEBI.Tests.Domain;

public class PrestamoTests
{
    [Fact]
    public void DebeDetectarUnaDevolucionTardia()
    {
        var inicio = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);
        var prestamo = new Prestamo(1, 1, 1, 1, 1, inicio, inicio.AddDays(7));

        var fueTardia = prestamo.RegistrarDevolucion(empleadoDevolucionId: 2, inicio.AddDays(8));

        Assert.True(fueTardia);
        Assert.Equal(EstadoPrestamo.Devuelto, prestamo.Estado);
    }
}
