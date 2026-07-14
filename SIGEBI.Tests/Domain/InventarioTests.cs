using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Exceptions;

namespace SIGEBI.Tests.Domain;

public class InventarioTests
{
    [Fact]
    public void DebeMantenerConsistentesLasCantidadesEnTodoElCiclo()
    {
        var inventario = new Inventario(libroId: 1, cantidadTotal: 3);

        inventario.RegistrarPrestamo();
        inventario.RegistrarDanio();
        inventario.CambiarEstadoEjemplar(EstadoEjemplar.Disponible, EstadoEjemplar.Reservado);

        Assert.Equal(3, inventario.CantidadTotal);
        Assert.Equal(1, inventario.CantidadDisponible);
        Assert.Equal(0, inventario.CantidadPrestada);
        Assert.Equal(1, inventario.CantidadReservada);
        Assert.Equal(1, inventario.CantidadDanada);
    }

    [Fact]
    public void NoDebeReducirTotalPorDebajoDeEjemplaresNoDisponibles()
    {
        var inventario = new Inventario(libroId: 1, cantidadTotal: 1);
        inventario.RegistrarPrestamo();

        Assert.Throws<DomainException>(() => inventario.AjustarCantidadTotal(0));
    }
}
