namespace SIGEBI.Application.Dtos.Administradores
{
    public class AdministradorDto : DtoBase
    {
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public string NivelAcceso { get; set; }
        public bool Activo { get; set; }
    }
}