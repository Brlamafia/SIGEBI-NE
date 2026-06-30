using System;
using System.Collections.Generic;
using System.Text;
using SIGEBI.Domain.Entities.Catalogo;

namespace SIGEBI.Domain.Interfaces.Repositories
{
    // Inversión de dependencias: Domain define el contrato y Persistence lo implementa.
    public interface IInventarioRepository
    {
        Task<Inventario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Inventario?> ObtenerPorLibroIdAsync(int libroId, CancellationToken cancellationToken = default);
        Task AgregarAsync(Inventario inventario, CancellationToken cancellationToken = default);
        void Actualizar(Inventario inventario);
    }
}
