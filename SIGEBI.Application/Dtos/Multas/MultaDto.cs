using System;

namespace SIGEBI.Application.Dtos.Multas
{
    // DTO: expone la multa sin filtrar la entidad de dominio.
    public class MultaDto : DtoBase
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int? PrestamoId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; }
        public int? EmpleadoResolucionId { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public string? ObservacionResolucion { get; set; }
    }
}
