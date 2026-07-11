using System;

namespace SIGEBI.Application.Dtos.Auditoria
{
    // DTO: permite consultar el registro histórico de auditoría.
    public class AuditoriaDto : DtoBase
    {
        public int Id { get; set; }
        public int UsuarioResponsableId { get; set; }
        public string Modulo { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}
