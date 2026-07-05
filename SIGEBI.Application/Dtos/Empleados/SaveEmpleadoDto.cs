namespace SIGEBI.Application.Dtos.Empleados
{
    public class SaveEmpleadoDto
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Cedula { get; set; }
        public int CargoId { get; set; }
    }
}