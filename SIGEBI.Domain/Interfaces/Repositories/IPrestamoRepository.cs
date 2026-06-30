using System;
using System.Collections.Generic;
using System.Text;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    // Repository: abstrae la persistencia de préstamos sin filtrar detalles de Entity Framework.
    public interface IPrestamoRepository
    {
        Task<Prestamo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Prestamo>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Prestamo>> ObtenerPorEstadoAsync(
            EstadoPrestamo estado,
            CancellationToken cancellationToken = default);
        Task<int> ContarActivosPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default);
        Task<bool> TieneVencidosPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default);
        Task AgregarAsync(Prestamo prestamo, CancellationToken cancellationToken = default);
        void Actualizar(Prestamo prestamo);
    }
}
