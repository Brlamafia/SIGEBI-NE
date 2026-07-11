using System;

namespace SIGEBI.Application.Dtos.Prestamos
{
    // Separación de capas: evita que la interfaz reciba directamente entidades de dominio.
    public class RegistrarPrestamoDto
    {
        public int SolicitudPrestamoId { get; set; }
        public int EmpleadoPrestamoId { get; set; }
        public int LimitePrestamos { get; set; }
        public DateTime FechaPrestamo { get; set; }
        public DateTime FechaEsperadaDevolucion { get; set; }
    }
}
