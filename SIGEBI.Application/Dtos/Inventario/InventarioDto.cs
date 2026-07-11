namespace SIGEBI.Application.Dtos.Inventario
{
    // DTO: presenta el estado consultable del inventario.
    public class InventarioDto : DtoBase
    {
        public int Id { get; set; }
        public int LibroId { get; set; }
        public int CantidadTotal { get; set; }
        public int CantidadDisponible { get; set; }
        public int CantidadPrestada { get; set; }
        public int CantidadPerdida { get; set; }
        public int CantidadDanada { get; set; }
        public bool TieneDisponibilidad { get; set; }
    }
}
