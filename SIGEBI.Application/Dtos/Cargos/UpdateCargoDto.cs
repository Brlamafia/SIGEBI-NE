namespace SIGEBI.Application.Dtos.Cargos
{
    public class UpdateCargoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal SalarioBase { get; set; }
        public bool Activo { get; set; }
    }
}