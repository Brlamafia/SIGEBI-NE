using System;

namespace SIGEBI.Application.Dtos.Prestamos
{
    // DTO: expone datos del préstamo sin filtrar la entidad de dominio.
    public class PrestamoDto : DtoBase
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int LibroId { get; set; }
        public int SolicitudPrestamoId { get; set; }
        public int EmpleadoPrestamoId { get; set; }
        public int? EmpleadoDevolucionId { get; set; }
        public DateTime FechaPrestamo { get; set; }
        public DateTime FechaEsperadaDevolucion { get; set; }
        public DateTime? FechaRealDevolucion { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}
