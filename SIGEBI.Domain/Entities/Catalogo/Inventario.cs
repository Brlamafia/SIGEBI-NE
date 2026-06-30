using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Domain.Entities.Catalogo
{
    // Responsabilidad única: administra exclusivamente las existencias de un libro.
    public class Inventario : SIGEBI.Domain.Base.EntidadAuditable
    {
        // Encapsulación: el estado solo cambia mediante las operaciones definidas por la entidad.
        public int LibroId { get; private set; }
        public int CantidadTotal { get; private set; }
        public int CantidadDisponible { get; private set; }
        public int CantidadPrestada { get; private set; }

        // DRY: la regla de disponibilidad se expresa una sola vez.
        public bool TieneDisponibilidad => CantidadDisponible > 0;

        // Constructor reservado para la materialización de la entidad desde persistencia.
        private Inventario()
        {
        }

        public Inventario(int libroId, int cantidadTotal)
        {
            // Fail Fast: impide crear un inventario con datos inválidos.
            if (libroId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(libroId), "El identificador del libro debe ser mayor que cero.");
            }

            if (cantidadTotal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cantidadTotal), "La cantidad total no puede ser negativa.");
            }

            LibroId = libroId;
            CantidadTotal = cantidadTotal;
            CantidadDisponible = cantidadTotal;
        }

        // Entidad con comportamiento: protege la regla de negocio al registrar un préstamo.
        public void RegistrarPrestamo()
        {
            if (!TieneDisponibilidad)
            {
                throw new SIGEBI.Domain.Exceptions.EjemplarNoDisponibleException(LibroId);
            }

            CantidadDisponible--;
            CantidadPrestada++;
            MarcarComoModificada();
        }

        // Entidad con comportamiento: la devolución mantiene sincronizadas las cantidades.
        public void RegistrarDevolucion()
        {
            if (CantidadPrestada <= 0)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "No existen ejemplares prestados para devolver.");
            }

            CantidadDisponible++;
            CantidadPrestada--;
            MarcarComoModificada();
        }

        // Invariante: total siempre debe ser igual a disponible más prestada.
        public void AjustarCantidadTotal(int nuevaCantidadTotal)
        {
            if (nuevaCantidadTotal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nuevaCantidadTotal), "La cantidad total no puede ser negativa.");
            }

            if (nuevaCantidadTotal < CantidadPrestada)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "La cantidad total no puede ser menor que la cantidad prestada.");
            }

            CantidadTotal = nuevaCantidadTotal;
            CantidadDisponible = CantidadTotal - CantidadPrestada;
            MarcarComoModificada();
        }
    }
}
