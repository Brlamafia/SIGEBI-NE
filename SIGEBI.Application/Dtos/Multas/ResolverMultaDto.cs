using System;

namespace SIGEBI.Application.Dtos.Multas
{
    // DTO de entrada: agrupa los datos necesarios para resolver una multa.
    public class ResolverMultaDto
    {
        public int MultaId { get; set; }
        public int EmpleadoResolucionId { get; set; }
        public DateTime FechaResolucion { get; set; }
        public string Observacion { get; set; } = string.Empty;
    }
}
