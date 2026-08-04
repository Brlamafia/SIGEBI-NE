using System;
using System.ComponentModel.DataAnnotations;

namespace SIGEBI.Application.Dtos.Auditoria
{
    // DTO de consulta: agrupa criterios para leer registros de auditoría.
    public class FiltroAuditoriaDto
    {
        public int? UsuarioResponsableId { get; set; }
        public string? Modulo { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        [Range(1, 1_000_000)]
        public int Pagina { get; set; } = 1;
        [Range(1, 200)]
        public int TamanoPagina { get; set; } = 100;
    }
}
