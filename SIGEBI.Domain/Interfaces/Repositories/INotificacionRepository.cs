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
        Task<IEnumerable<Notificacion>> ObtenerPorUsuarioAsync(int usuarioId);
        Task<IEnumerable<Notificacion>> ObtenerNoLeidasPorUsuarioAsync(int usuarioId);
        void Actualizar(Notificacion notificacion);

        // ELIMINAMOS la línea de AgregarAsync porque ya viene heredada de IRepository
    }
}