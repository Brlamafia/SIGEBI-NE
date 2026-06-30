using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Domain.Exceptions
{
    // Excepción específica: comunica una violación de la regla de disponibilidad.
    public sealed class EjemplarNoDisponibleException : DomainException
    {
        public EjemplarNoDisponibleException()
            : base("No hay ejemplares disponibles para prestar.")
        {
        }

        public EjemplarNoDisponibleException(int libroId)
            : base($"No hay ejemplares disponibles para el libro con identificador {libroId}.")
        {
        }
    }
}
