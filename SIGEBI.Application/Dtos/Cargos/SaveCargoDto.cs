namespace SIGEBI.Application.Dtos.Cargos
{
    public class SaveCargoDto
    {
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
        public required decimal SalarioBase { get; set; }
    }
}