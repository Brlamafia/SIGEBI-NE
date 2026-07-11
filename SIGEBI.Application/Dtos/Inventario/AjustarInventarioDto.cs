namespace SIGEBI.Application.Dtos.Inventario
{
    // DTO de entrada: separa la solicitud de ajuste del modelo de dominio.
    public class AjustarInventarioDto
    {
        public int InventarioId { get; set; }
        public int NuevaCantidadTotal { get; set; }
        public int UsuarioResponsableId { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
