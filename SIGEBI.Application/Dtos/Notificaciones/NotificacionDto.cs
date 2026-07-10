using System;

namespace SIGEBI.Application.Dtos.Notificaciones
{
    public class NotificacionDto : DtoBase
    {
        public required int Id { get; set; }
        public required int UsuarioId { get; set; }
        public required string Mensaje { get; set; }
        public required DateTime FechaEnvio { get; set; }
        public required bool Leida { get; set; }
    }
}