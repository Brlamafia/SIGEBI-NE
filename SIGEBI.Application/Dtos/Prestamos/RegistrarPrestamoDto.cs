using System;

namespace SIGEBI.Application.Dtos.Prestamos
{
    // Separación de capas: evita que la interfaz reciba directamente entidades de dominio.
    public class RegistrarPrestamoDto
    {
        public int SolicitudPrestamoId { get; set; }
        public int EmpleadoPrestamoId { get; set; }
        public DateTime FechaPrestamo { get; set; }
    }
}
