using SIGEBI.Domain.Base;
using SIGEBI.Domain.Enums;
// B.R
namespace SIGEBI.Domain.Entities.Notificaciones
{
    public class Notificacion : EntidadBase
    {
        public int UsuarioId { get; private set; }
        public string Mensaje { get; private set; } = string.Empty;
        public DateTime FechaEnvio { get; private set; }
        public bool Leida { get; private set; }
        public TipoNotificacion TipoEvento { get; private set; }

        private Notificacion() { }

        public Notificacion(
            int usuarioId,
            string mensaje,
            TipoNotificacion tipoEvento = TipoNotificacion.Informacion)
        {
            if (usuarioId <= 0) throw new ArgumentOutOfRangeException(nameof(usuarioId));
            if (string.IsNullOrWhiteSpace(mensaje)) throw new ArgumentException("El mensaje es obligatorio.");

            UsuarioId = usuarioId;
            Mensaje = mensaje;
            FechaEnvio = DateTime.UtcNow;
            Leida = false;
            TipoEvento = Enum.IsDefined(tipoEvento)
                ? tipoEvento
                : throw new ArgumentOutOfRangeException(nameof(tipoEvento));
        }

        // El usuario la lee en la app Web React
        public void MarcarComoLeida()
        {
            Leida = true;
        }
    }
}
