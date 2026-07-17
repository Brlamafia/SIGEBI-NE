namespace SIGEBI.Application.Dtos.Usuarios
{
    public class CrearUsuarioDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public int TipoUsuarioId { get; set; } // O el Enum directamente si lo manejan así
    }
}