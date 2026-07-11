namespace SIGEBI.Application.Dtos.Prestamos
{
    // DTO de entrada: separa los datos del caso de uso de cancelación.
    public class CancelarPrestamoDto
    {
        public int PrestamoId { get; set; }
        public int EmpleadoResponsableId { get; set; }
    }
}
