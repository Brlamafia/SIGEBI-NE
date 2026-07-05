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
    public class SolicitudPrestamoRepository : MutableRepository<SolicitudPrestamo>, ISolicitudPrestamoRepository
    {
        public SolicitudPrestamoRepository(SigebiContext context) : base(context) { }

        // SECCIÓN 1: Consultas por Identificador
        public async Task<SolicitudPrestamo?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, ct);
        }

        // SECCIÓN 2: Consultas por Usuario (Ajustado a IEnumerable y sin CancellationToken)
        public async Task<IEnumerable<SolicitudPrestamo>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            return await _dbSet.Where(s => s.UsuarioId == usuarioId).ToListAsync();
        }

        // SECCIÓN 3: Consultas por Estado (Ajustado a IEnumerable y sin CancellationToken)
        public async Task<IEnumerable<SolicitudPrestamo>> ObtenerPorEstadoAsync(EstadoSolicitud estado)
        {
            return await _dbSet.Where(s => s.Estado == estado).ToListAsync();
        }
    }
}