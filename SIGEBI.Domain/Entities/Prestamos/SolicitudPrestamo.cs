using SIGEBI.Domain.Base;
using SIGEBI.Domain.Enums;
// B.R
namespace SIGEBI.Domain.Entities.Prestamos
{
    public class SolicitudPrestamo : EntidadAuditable
    {
        public int UsuarioId { get; private set; }
        public int LibroId { get; private set; }
        public DateTime FechaSolicitud { get; private set; }
        public EstadoSolicitud Estado { get; private set; }
        public string? MotivoRechazo { get; private set; }

        private SolicitudPrestamo() { }

        // Constructor para cuando el estudiante solicita desde la Web
        public SolicitudPrestamo(int usuarioId, int libroId)
        {
            if (usuarioId <= 0) throw new ArgumentOutOfRangeException(nameof(usuarioId), "ID de usuario inválido.");
            if (libroId <= 0) throw new ArgumentOutOfRangeException(nameof(libroId), "ID de libro inválido.");

            UsuarioId = usuarioId;
            LibroId = libroId;
            FechaSolicitud = DateTime.UtcNow;
            Estado = EstadoSolicitud.Pendiente; // (Asegúrate de tener este Enum)
        }

        // Comportamiento: Cuando se aprueba desde Desktop
        public void Aprobar()
        {
            if (Estado != EstadoSolicitud.Pendiente)
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "Solo se pueden aprobar solicitudes pendientes.");

            Estado = EstadoSolicitud.Aprobada;
            MarcarComoModificada();
        }

        // Comportamiento: Cuando se rechaza desde Desktop
        public void Rechazar(string motivo)
        {
            if (Estado != EstadoSolicitud.Pendiente)
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "Solo se pueden rechazar solicitudes pendientes.");
            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException("Debe especificar un motivo de rechazo.");

            Estado = EstadoSolicitud.Rechazada;
            MotivoRechazo = motivo.Trim();
            MarcarComoModificada();
        }
    }
}
