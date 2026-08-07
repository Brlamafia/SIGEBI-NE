using SIGEBI.Domain.Entities.Prestamos;

namespace SIGEBI.Persistence.Models;

public sealed class PrestamoEjemplarRelacion
{
    public int PrestamoId { get; set; }
    public int EjemplarId { get; set; }
    public DateTime FechaAsignacion { get; set; }
    public Prestamo Prestamo { get; set; } = null!;
}
