using System.Data;

namespace SIGEBI.Domain.Interfaces
{
    // Patrón Unit of Work: coordina persistencia y transacciones como una sola unidad lógica.
    public interface IUnitOfWork
    {
        Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
        Task EjecutarEnTransaccionAsync(
            Func<CancellationToken, Task> operacion,
            CancellationToken cancellationToken = default);
        Task EjecutarEnTransaccionAsync(
            Func<CancellationToken, Task> operacion,
            IsolationLevel nivelAislamiento,
            CancellationToken cancellationToken = default);
    }
}
