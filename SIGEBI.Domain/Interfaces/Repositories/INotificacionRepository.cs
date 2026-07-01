// B.R
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SIGEBI.Domain.Entities.Notificaciones;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    public interface INotificacionRepository
    {
        Task<Notificacion?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Notificacion>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);
        Task AgregarAsync(Notificacion notificacion, CancellationToken cancellationToken = default);
        void Actualizar(Notificacion notificacion);
    }
}