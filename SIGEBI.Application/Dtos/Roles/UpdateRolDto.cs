namespace SIGEBI.Application.Dtos.Roles
{
    public class UpdateRolDto
    {
        public required int Id { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
        public required bool Activo { get; set; }
    }
}