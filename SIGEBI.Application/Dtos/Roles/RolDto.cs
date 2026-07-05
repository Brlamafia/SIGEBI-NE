namespace SIGEBI.Application.Dtos.Roles
{
    public class RolDto : DtoBase
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }
    }
}