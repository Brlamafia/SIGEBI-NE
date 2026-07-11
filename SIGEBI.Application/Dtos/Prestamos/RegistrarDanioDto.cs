using System;

namespace SIGEBI.Application.Dtos.Prestamos
{
    // DTO de entrada: agrupa los datos para registrar una devolución con daño.
    public class RegistrarDanioDto
    {
        public int PrestamoId { get; set; }
        public int EmpleadoResponsableId { get; set; }
        public DateTime FechaDevolucion { get; set; }
        public decimal MontoMulta { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
