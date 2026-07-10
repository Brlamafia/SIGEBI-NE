namespace SIGEBI.Application.Dtos.Notificaciones
{
    public class SaveNotificacionDto
    {
        public required int UsuarioId { get; set; }
        public required string Mensaje { get; set; }
    }
}