using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Exceptions;

namespace SIGEBI.Tests.Domain;

public class EjemplarTests
{
    [Fact]
    public void DebeRecorrerCicloPrestamoYDevolucion()
    {
        var ejemplar = new Ejemplar(libroId: 1, codigo: "EJ-001");

        ejemplar.Prestar();
        Assert.Equal(EstadoEjemplar.Prestado, ejemplar.Estado);

        ejemplar.Devolver();
        Assert.Equal(EstadoEjemplar.Disponible, ejemplar.Estado);
    }

    [Fact]
    public void NoDebePrestarUnEjemplarReservado()
    {
        var ejemplar = new Ejemplar(libroId: 1, codigo: "EJ-001");
        ejemplar.CambiarEstadoOperativo(EstadoEjemplar.Reservado);

        Assert.Throws<DomainException>(ejemplar.Prestar);
    }
}
