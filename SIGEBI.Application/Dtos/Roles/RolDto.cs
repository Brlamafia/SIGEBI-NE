namespace SIGEBI.Application.Dtos.Roles
{
    public class RolDto : DtoBase
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
}
