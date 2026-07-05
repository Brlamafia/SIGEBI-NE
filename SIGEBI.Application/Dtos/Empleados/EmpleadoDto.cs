namespace SIGEBI.Application.Dtos.Empleados
{
    public class EmpleadoDto : DtoBase
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Cedula { get; set; }
        public int CargoId { get; set; }
        public bool Activo { get; set; }
    }
}