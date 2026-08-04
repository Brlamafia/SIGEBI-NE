using System;
using System.Collections.Generic;
using System.Text;
using SIGEBI.Domain.Entities.Auditoria;
using SIGEBI.Domain.Enums;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    // Inmutabilidad: el contrato permite crear y consultar, pero nunca actualizar o eliminar.
    public interface IAuditoriaRepository
    {
        Task<IReadOnlyCollection<Auditoria>> ObtenerTodasAsync(
            CancellationToken cancellationToken = default);
        Task<Auditoria?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Auditoria>> ObtenerPorUsuarioAsync(
            int usuarioResponsableId,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Auditoria>> ObtenerPorModuloAsync(
            ModuloAuditoria modulo,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Auditoria>> ObtenerPorRangoAsync(
            DateTime fechaDesde,
            DateTime fechaHasta,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Auditoria>> FiltrarPaginaAsync(
            int skip,
            int take,
            int? usuarioResponsableId = null,
            ModuloAuditoria? modulo = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            CancellationToken cancellationToken = default);
        Task AgregarAsync(Auditoria auditoria, CancellationToken cancellationToken = default);
        Task AgregarRangoAsync(
            IEnumerable<Auditoria> auditorias,
            CancellationToken cancellationToken = default);
    }
}
