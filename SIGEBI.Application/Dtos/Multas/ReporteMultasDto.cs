namespace SIGEBI.Application.Dtos.Multas
{
    public class ReporteMultasDto
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }

        public int TotalMultasGeneradas { get; set; }
        public int CantidadPagadas { get; set; }
        public int CantidadPendientes { get; set; }

        public decimal MontoTotalGenerado { get; set; }
        public decimal MontoTotalRecaudado { get; set; }
        public decimal MontoTotalPendiente { get; set; }
    }
}