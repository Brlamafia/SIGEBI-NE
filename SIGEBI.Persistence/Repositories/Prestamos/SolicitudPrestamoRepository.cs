// B.R
using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Enums;
using SIGEBI.Persistence.Base;
using SIGEBI.Persistence.Context;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Repositories.Prestamos
{
    // B.R Implementación completa del repositorio para asegurar que cumpla la interfaz
    public class SolicitudPrestamoRepository : BaseRepository<SolicitudPrestamo>, ISolicitudPrestamoRepository
    {
        public SolicitudPrestamoRepository(SigebiContext context) : base(context) { }

        // SECCIÓN 1: Consultas por Identificador
        public async Task<SolicitudPrestamo?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, ct);
        }

        // SECCIÓN 2: Consultas por Usuario
        public async Task<IReadOnlyCollection<SolicitudPrestamo>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            return await _dbSet.Where(s => s.UsuarioId == usuarioId).ToListAsync(ct);
        }

        // SECCIÓN 3: Consultas por Estado
        public async Task<IReadOnlyCollection<SolicitudPrestamo>> ObtenerPorEstadoAsync(EstadoSolicitud estado, CancellationToken ct = default)
        {
            return await _dbSet.Where(s => s.Estado == estado).ToListAsync(ct);
        }

    }
}