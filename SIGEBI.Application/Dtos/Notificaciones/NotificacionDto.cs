using System;

namespace SIGEBI.Application.Dtos.Notificaciones
{
    public class NotificacionDto : DtoBase
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Mensaje { get; set; }
        public DateTime FechaEnvio { get; set; }
        public bool Leida { get; set; }
    }
}