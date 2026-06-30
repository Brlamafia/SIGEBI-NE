using System;
using System.Collections.Generic;
using System.Text;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    // Repository: permite consultar penalizaciones sin acoplar Domain a la base de datos.
    public interface IMultaRepository
    {
        Task<Multa?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Multa>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Multa>> ObtenerPorEstadoAsync(
            EstadoMulta estado,
            CancellationToken cancellationToken = default);
        Task<bool> TienePendientesPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default);
        Task AgregarAsync(Multa multa, CancellationToken cancellationToken = default);
        void Actualizar(Multa multa);
    }
}
