namespace SIGEBI.Application.Dtos.Inventario;

public sealed class EjemplarDto
{
    public int Id { get; set; }
    public int LibroId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}
