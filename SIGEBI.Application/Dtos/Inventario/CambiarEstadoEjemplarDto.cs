namespace SIGEBI.Application.Dtos.Inventario;

public sealed class CambiarEstadoEjemplarDto
{
    public int EjemplarId { get; set; }
    public string NuevoEstado { get; set; } = string.Empty;
    public int UsuarioResponsableId { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
