using SIGEBI.Domain.Base;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Exceptions;

namespace SIGEBI.Domain.Entities.Catalogo;

public class Inventario : EntidadAuditable
{
    public int LibroId { get; private set; }
    public int CantidadTotal { get; private set; }
    public int CantidadDisponible { get; private set; }
    public int CantidadPrestada { get; private set; }
    public int CantidadReservada { get; private set; }
    public int CantidadFueraServicio { get; private set; }
    public int CantidadPerdida { get; private set; }
    public int CantidadDanada { get; private set; }
    public bool TieneDisponibilidad => CantidadDisponible > 0;

    private Inventario() { }

    public Inventario(int libroId, int cantidadTotal)
    {
        if (libroId <= 0)
            throw new ArgumentOutOfRangeException(nameof(libroId));
        if (cantidadTotal < 0)
            throw new ArgumentOutOfRangeException(nameof(cantidadTotal));

        LibroId = libroId;
        CantidadTotal = cantidadTotal;
        CantidadDisponible = cantidadTotal;
    }

    public void RegistrarPrestamo() => Mover(EstadoEjemplar.Disponible, EstadoEjemplar.Prestado);
    public void RegistrarDevolucion() => Mover(EstadoEjemplar.Prestado, EstadoEjemplar.Disponible);
    public void CancelarPrestamo() => Mover(EstadoEjemplar.Prestado, EstadoEjemplar.Disponible);
    public void RegistrarPerdida() => Mover(EstadoEjemplar.Prestado, EstadoEjemplar.Perdido);
    public void RegistrarDanio() => Mover(EstadoEjemplar.Prestado, EstadoEjemplar.Danado);

    public void CambiarEstadoEjemplar(EstadoEjemplar estadoActual, EstadoEjemplar nuevoEstado)
        => Mover(estadoActual, nuevoEstado);

    public void AjustarCantidadTotal(int nuevaCantidadTotal)
    {
        var cantidadNoDisponible = CantidadTotal - CantidadDisponible;
        if (nuevaCantidadTotal < cantidadNoDisponible)
            throw new DomainException("La cantidad total no puede ser menor que los ejemplares no disponibles.");

        CantidadTotal = nuevaCantidadTotal;
        CantidadDisponible = CantidadTotal - cantidadNoDisponible;
        MarcarComoModificada();
    }

    private void Mover(EstadoEjemplar origen, EstadoEjemplar destino)
    {
        if (origen == destino)
            throw new DomainException("El estado de origen y destino no pueden ser iguales.");

        Decrementar(origen);
        Incrementar(destino);
        MarcarComoModificada();
    }

    private void Decrementar(EstadoEjemplar estado)
    {
        var cantidad = ObtenerCantidad(estado);
        if (cantidad <= 0)
            throw new DomainException($"No existen ejemplares en estado {estado} para completar la operación.");

        EstablecerCantidad(estado, cantidad - 1);
    }

    private void Incrementar(EstadoEjemplar estado)
        => EstablecerCantidad(estado, ObtenerCantidad(estado) + 1);

    private int ObtenerCantidad(EstadoEjemplar estado)
        => estado switch
        {
            EstadoEjemplar.Disponible => CantidadDisponible,
            EstadoEjemplar.Prestado => CantidadPrestada,
            EstadoEjemplar.Reservado => CantidadReservada,
            EstadoEjemplar.FueraDeServicio => CantidadFueraServicio,
            EstadoEjemplar.Perdido => CantidadPerdida,
            EstadoEjemplar.Danado => CantidadDanada,
            _ => throw new ArgumentOutOfRangeException(nameof(estado))
        };

    private void EstablecerCantidad(EstadoEjemplar estado, int cantidad)
    {
        switch (estado)
        {
            case EstadoEjemplar.Disponible: CantidadDisponible = cantidad; break;
            case EstadoEjemplar.Prestado: CantidadPrestada = cantidad; break;
            case EstadoEjemplar.Reservado: CantidadReservada = cantidad; break;
            case EstadoEjemplar.FueraDeServicio: CantidadFueraServicio = cantidad; break;
            case EstadoEjemplar.Perdido: CantidadPerdida = cantidad; break;
            case EstadoEjemplar.Danado: CantidadDanada = cantidad; break;
            default: throw new ArgumentOutOfRangeException(nameof(estado));
        }
    }
}
