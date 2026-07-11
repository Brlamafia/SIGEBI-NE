using System;

namespace SIGEBI.Application.Dtos.Prestamos
{
    // DTO de entrada: agrupa los datos para registrar una pérdida.
    public class RegistrarPerdidaDto
    {
        public int PrestamoId { get; set; }
        public int EmpleadoResponsableId { get; set; }
        public DateTime FechaReporte { get; set; }
        public decimal MontoMulta { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
