// B.R
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    public interface ISolicitudPrestamoRepository
    {
        Task<SolicitudPrestamo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<SolicitudPrestamo>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<SolicitudPrestamo>> ObtenerPorEstadoAsync(EstadoSolicitud estado, CancellationToken cancellationToken = default);
        Task AgregarAsync(SolicitudPrestamo solicitud, CancellationToken cancellationToken = default);
        void Actualizar(SolicitudPrestamo solicitud);
    }
}