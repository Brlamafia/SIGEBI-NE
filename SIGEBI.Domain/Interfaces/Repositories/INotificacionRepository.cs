// B.R
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SIGEBI.Domain.Entities.Notificaciones;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    public interface INotificacionRepository : IRepository<Notificacion>
    {
        Task<Notificacion?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Notificacion>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Notificacion>> ObtenerPorUsuarioAsync(
            int usuarioId,
            int skip,
            int take,
            CancellationToken cancellationToken = default);
        Task<IEnumerable<Notificacion>> ObtenerNoLeidasPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default);
        Task<bool> ExisteEventoAsync(
            int usuarioId,
            string textoIdentificador,
            DateTime desde,
            CancellationToken cancellationToken = default);
        void Actualizar(Notificacion notificacion);

        // ELIMINAMOS la línea de AgregarAsync porque ya viene heredada de IRepository
    }
}
