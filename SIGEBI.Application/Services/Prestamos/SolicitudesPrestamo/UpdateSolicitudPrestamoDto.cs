namespace SIGEBI.Application.Dtos.SolicitudesPrestamo
{
    public class UpdateSolicitudPrestamoDto
    {
        public int Id { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? MotivoRechazo { get; set; }
    }
}
