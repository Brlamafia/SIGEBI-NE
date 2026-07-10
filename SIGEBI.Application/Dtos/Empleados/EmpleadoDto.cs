namespace SIGEBI.Application.Dtos.Empleados
{
    public class EmpleadoDto : DtoBase
    {
        public required int Id { get; set; }
        public required string Nombre { get; set; }
        public required string Apellido { get; set; }
        public required string Cedula { get; set; }
        public required int CargoId { get; set; }
        public required bool Activo { get; set; }
    }
}