using System;

namespace SIGEBI.Application.Dtos.Auditoria
{
    // DTO de consulta: agrupa criterios para leer registros de auditoría.
    public class FiltroAuditoriaDto
    {
        public int? UsuarioResponsableId { get; set; }
        public string? Modulo { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
    }
}
