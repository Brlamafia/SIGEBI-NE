namespace SIGEBI.Application.Dtos.Cargos
{
    public class UpdateCargoDto
    {
        public required int Id { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
        public required decimal SalarioBase { get; set; }
        public required bool Activo { get; set; }
    }
}