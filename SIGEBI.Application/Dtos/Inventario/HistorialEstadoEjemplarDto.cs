namespace SIGEBI.Application.Dtos.Inventario;

public sealed class HistorialEstadoEjemplarDto
{
    public int AuditoriaId { get; init; }
    public int EjemplarId { get; init; }
    public string CodigoEjemplar { get; init; } = string.Empty;
    public string EstadoAnterior { get; init; } = string.Empty;
    public string EstadoNuevo { get; init; } = string.Empty;
    public string Motivo { get; init; } = string.Empty;
    public int UsuarioResponsableId { get; init; }
    public DateTime Fecha { get; init; }
}
