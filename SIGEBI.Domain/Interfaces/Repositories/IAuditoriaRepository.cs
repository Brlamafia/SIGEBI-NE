using System;
using System.Collections.Generic;
using System.Text;
using SIGEBI.Domain.Entities.Auditoria;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    // Inmutabilidad: el contrato permite crear y consultar, pero nunca actualizar o eliminar.
    public interface IAuditoriaRepository
    {
        Task<Auditoria?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Auditoria>> ObtenerPorUsuarioAsync(
            int usuarioResponsableId,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Auditoria>> ObtenerPorModuloAsync(
            string modulo,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Auditoria>> ObtenerPorRangoAsync(
            DateTime fechaDesde,
            DateTime fechaHasta,
            CancellationToken cancellationToken = default);
        Task AgregarAsync(Auditoria auditoria, CancellationToken cancellationToken = default);
    }
}
