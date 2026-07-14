using SIGEBI.Domain.Base;
using SIGEBI.Domain.Common;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Exceptions;

namespace SIGEBI.Domain.Entities.Catalogo;

public sealed class Ejemplar : EntidadAuditable
{
    public int LibroId { get; private set; }
    public string Codigo { get; private set; } = string.Empty;
    public EstadoEjemplar Estado { get; private set; }

    private Ejemplar() { }

    public Ejemplar(int libroId, string codigo)
    {
        LibroId = Guard.AgainstNonPositive(libroId, nameof(libroId));
        Codigo = Guard.AgainstNullOrWhiteSpace(codigo, nameof(codigo));
        Estado = EstadoEjemplar.Disponible;
    }

    public void Prestar() => CambiarDesde(EstadoEjemplar.Disponible, EstadoEjemplar.Prestado);
    public void Devolver() => CambiarDesde(EstadoEjemplar.Prestado, EstadoEjemplar.Disponible);
    public void RegistrarPerdida() => CambiarDesde(EstadoEjemplar.Prestado, EstadoEjemplar.Perdido);
    public void RegistrarDanio() => CambiarDesde(EstadoEjemplar.Prestado, EstadoEjemplar.Danado);

    public void CambiarEstadoOperativo(EstadoEjemplar nuevoEstado)
    {
        var transicionValida = (Estado, nuevoEstado) switch
        {
            (EstadoEjemplar.Disponible, EstadoEjemplar.Reservado) => true,
            (EstadoEjemplar.Disponible, EstadoEjemplar.FueraDeServicio) => true,
            (EstadoEjemplar.Reservado, EstadoEjemplar.Disponible) => true,
            (EstadoEjemplar.Reservado, EstadoEjemplar.FueraDeServicio) => true,
            (EstadoEjemplar.FueraDeServicio, EstadoEjemplar.Disponible) => true,
            (EstadoEjemplar.Danado, EstadoEjemplar.FueraDeServicio) => true,
            (EstadoEjemplar.Danado, EstadoEjemplar.Disponible) => true,
            _ => false
        };

        if (!transicionValida)
            throw new DomainException($"No se permite cambiar el ejemplar de {Estado} a {nuevoEstado}.");

        Estado = nuevoEstado;
        MarcarComoModificada();
    }

    private void CambiarDesde(EstadoEjemplar estadoEsperado, EstadoEjemplar nuevoEstado)
    {
        if (Estado != estadoEsperado)
            throw new DomainException($"El ejemplar debe estar en estado {estadoEsperado}.");

        Estado = nuevoEstado;
        MarcarComoModificada();
    }
}
