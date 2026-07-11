using System;

namespace SIGEBI.Application.Dtos.Prestamos
{
    // Separación de Responsabilidades: agrupa solo los datos necesarios para devolver.
    public class RegistrarDevolucionDto
    {
        public int PrestamoId { get; set; }
        public int EmpleadoDevolucionId { get; set; }
        public DateTime FechaRealDevolucion { get; set; }
        public decimal MontoMultaPorDia { get; set; }
    }
}
