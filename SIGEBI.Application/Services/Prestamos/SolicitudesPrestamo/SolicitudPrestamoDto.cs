using System;

namespace SIGEBI.Application.Dtos.SolicitudesPrestamo
{
    public class SolicitudPrestamoDto : DtoBase
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int LibroId { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string Estado { get; set; } = string.Empty; // Ej: Pendiente, Aprobada, Rechazada
        public string? MotivoRechazo { get; set; }
    }
}
