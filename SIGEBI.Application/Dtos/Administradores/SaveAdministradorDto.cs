namespace SIGEBI.Application.Dtos.Administradores
{
    public class SaveAdministradorDto
    {
        public required int EmpleadoId { get; set; }
        public required string NivelAcceso { get; set; }
    }
}