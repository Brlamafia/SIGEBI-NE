using Microsoft.EntityFrameworkCore;
using System.Data;
using SIGEBI.Domain.Exceptions;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence
{
    // Patrón Unit of Work: garantiza atomicidad al guardar varios cambios relacionados.
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly SigebiContext _context;

        public UnitOfWork(SigebiContext context)
        {
            _context = context;
        }

        public async Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                _context.ChangeTracker.Clear();
                throw new ConflictoConcurrenciaException(exception);
            }
        }

        public async Task EjecutarEnTransaccionAsync(
            Func<CancellationToken, Task> operacion,
            CancellationToken cancellationToken = default)
            => await EjecutarEnTransaccionAsync(
                operacion,
                IsolationLevel.ReadCommitted,
                cancellationToken);

        public async Task EjecutarEnTransaccionAsync(
            Func<CancellationToken, Task> operacion,
            IsolationLevel nivelAislamiento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operacion);

            // Propiedad ACID: ante cualquier fallo se revierte toda la operación.
            await using var transaccion =
                await _context.Database.BeginTransactionAsync(nivelAislamiento, cancellationToken);

            try
            {
                await operacion(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaccion.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                await transaccion.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                throw new ConflictoConcurrenciaException(exception);
            }
            catch
            {
                await transaccion.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                throw;
            }
        }
    }
}
