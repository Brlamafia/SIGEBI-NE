namespace SIGEBI.Application.Dtos.Administradores
{
    public class AdministradorDto : DtoBase
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int CargoId { get; set; }
    }
}
