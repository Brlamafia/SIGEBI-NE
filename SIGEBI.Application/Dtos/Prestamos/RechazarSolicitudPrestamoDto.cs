namespace SIGEBI.Application.Dtos.Prestamos;

public sealed class RechazarSolicitudPrestamoDto
{
    public int SolicitudPrestamoId { get; set; }
    public int EmpleadoResponsableId { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
