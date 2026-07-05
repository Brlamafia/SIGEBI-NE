// B.R
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    // B.R: Ahora hereda de IRepository para estandarizar el patrón
    public interface ISolicitudPrestamoRepository : IRepository<SolicitudPrestamo>
    {
        Task<SolicitudPrestamo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

        // Ajustado a IEnumerable para coincidir con el Servicio de Aplicación
        Task<IEnumerable<SolicitudPrestamo>> ObtenerPorUsuarioAsync(int usuarioId);

        // Ajustado a IEnumerable para coincidir con el Servicio de Aplicación
        Task<IEnumerable<SolicitudPrestamo>> ObtenerPorEstadoAsync(EstadoSolicitud estado);

        void Actualizar(SolicitudPrestamo solicitud);
    }
}