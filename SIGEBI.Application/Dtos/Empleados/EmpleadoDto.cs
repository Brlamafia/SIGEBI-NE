namespace SIGEBI.Application.Dtos.Empleados
{
    public class EmpleadoDto : DtoBase
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int CargoId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
    }
}
