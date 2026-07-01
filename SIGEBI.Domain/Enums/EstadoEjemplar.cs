// B.R
namespace SIGEBI.Domain.Enums
{
    // Controla el estado físico y disponibilidad del ejemplar en inventario.
    public enum EstadoEjemplar
    {
        Disponible = 1,
        Prestado = 2,
        EnReparacion = 3,
        Perdido = 4
    }
}