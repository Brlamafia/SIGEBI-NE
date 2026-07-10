namespace SIGEBI.Application.Dtos.Administradores
{
    public class AdministradorDto : DtoBase
    {
        public required int Id { get; set; }
        public required int EmpleadoId { get; set; }
        public required string NivelAcceso { get; set; }
        public required bool Activo { get; set; }
    }
}