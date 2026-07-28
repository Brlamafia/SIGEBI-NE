using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<UnitOfWork> _logger;

        public UnitOfWork(SigebiContext context, ILogger<UnitOfWork> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var cambios = await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Unidad de trabajo guardó {Cantidad} cambios.", cambios);
                return cambios;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Guardado de la unidad de trabajo cancelado.");
                throw;
            }
            catch (DbUpdateConcurrencyException exception)
            {
                _logger.LogWarning(exception, "Conflicto de concurrencia al guardar la unidad de trabajo.");
                _context.ChangeTracker.Clear();
                throw new ConflictoConcurrenciaException(exception);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al guardar la unidad de trabajo.");
                throw;
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

            // Reutiliza la transacción activa cuando una operación compone servicios.
            if (_context.Database.CurrentTransaction is not null)
            {
                await operacion(cancellationToken);
                return;
            }

            // Propiedad ACID: ante cualquier fallo se revierte toda la operación.
            var estrategia = _context.Database.CreateExecutionStrategy();

            await estrategia.ExecuteAsync(async () =>
            {
                await using var transaccion =
                    await _context.Database.BeginTransactionAsync(
                        nivelAislamiento,
                        cancellationToken);

                try
                {
                    await operacion(cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaccion.CommitAsync(cancellationToken);
                    _logger.LogInformation(
                        "Transacción confirmada con aislamiento {NivelAislamiento}.",
                        nivelAislamiento);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    await transaccion.RollbackAsync(CancellationToken.None);
                    _context.ChangeTracker.Clear();
                    _logger.LogInformation("Transacción cancelada y revertida.");
                    throw;
                }
                catch (DbUpdateConcurrencyException exception)
                {
                    await transaccion.RollbackAsync(cancellationToken);
                    _context.ChangeTracker.Clear();
                    _logger.LogWarning(exception, "Transacción revertida por conflicto de concurrencia.");
                    throw new ConflictoConcurrenciaException(exception);
                }
                catch (Exception exception)
                {
                    await transaccion.RollbackAsync(cancellationToken);
                    _context.ChangeTracker.Clear();
                    _logger.LogError(exception, "Transacción revertida por un error no controlado.");
                    throw;
                }
            });
        }
    }
}
