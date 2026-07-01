namespace SIGEBI.Domain.Enums
{
    // B.R Estado explícito: controla el ciclo de vida de una solicitud de préstamo desde la web
    public enum EstadoSolicitud
    {
        Pendiente = 1,
        Aprobada = 2,
        Rechazada = 3,
        Cancelada = 4 // Por si el usuario decide cancelar su propia solicitud antes de que la revisen
    }
}