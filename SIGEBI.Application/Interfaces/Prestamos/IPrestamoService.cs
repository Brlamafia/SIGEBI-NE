using SIGEBI.Domain.Entities.Prestamos;

namespace SIGEBI.Application.Interfaces.Prestamos
{
    public interface IPrestamoService
    {
        Task<Prestamo> RegistrarPrestamoAsync(
            int solicitudPrestamoId,
            int empleadoPrestamoId,
            int limitePrestamos,
            DateTime fechaPrestamo,
            DateTime fechaEsperadaDevolucion,
            CancellationToken cancellationToken = default);

        Task<Multa?> RegistrarDevolucionAsync(
            int prestamoId,
            int empleadoDevolucionId,
            DateTime fechaRealDevolucion,
            decimal montoMultaPorDia,
            CancellationToken cancellationToken = default);

        Task CancelarPrestamoAsync(
            int prestamoId,
            int empleadoResponsableId,
            CancellationToken cancellationToken = default);

        Task<Multa> RegistrarPerdidaAsync(
            int prestamoId,
            int empleadoResponsableId,
            DateTime fechaReporte,
            decimal montoMulta,
            string motivo,
            CancellationToken cancellationToken = default);

        Task<Multa> RegistrarDevolucionConDanioAsync(
            int prestamoId,
            int empleadoResponsableId,
            DateTime fechaDevolucion,
            decimal montoMulta,
            string motivo,
            CancellationToken cancellationToken = default);
    }
}
