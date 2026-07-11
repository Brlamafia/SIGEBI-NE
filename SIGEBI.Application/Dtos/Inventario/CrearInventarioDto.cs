namespace SIGEBI.Application.Dtos.Inventario;

public sealed class CrearInventarioDto
{
    public int LibroId { get; set; }
    public int CantidadTotal { get; set; }
    public int UsuarioResponsableId { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
