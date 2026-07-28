namespace SIGEBI.Persistence.Models;

public sealed class PrestamoEjemplarRelacion
{
    public int PrestamoId { get; set; }
    public int EjemplarId { get; set; }
    public DateTime FechaAsignacion { get; set; }
}
