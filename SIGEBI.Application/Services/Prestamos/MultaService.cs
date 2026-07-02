using System.Data;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Domain.Entities.Auditoria;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Exceptions;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Prestamos
{
    // Application Service y SRP: coordina únicamente los casos de uso de multas.
    public class MultaService : IMultaService
    {
        private readonly IMultaRepository _multas;
        private readonly IEmpleadoRepository _empleados;
        private readonly IAuditoriaRepository _auditorias;
        private readonly IUnitOfWork _unitOfWork;

        public MultaService(
            IMultaRepository multas,
            IEmpleadoRepository empleados,
            IAuditoriaRepository auditorias,
            IUnitOfWork unitOfWork)
        {
            _multas = multas;
            _empleados = empleados;
            _auditorias = auditorias;
            _unitOfWork = unitOfWork;
        }

        public async Task MarcarComoPagadaAsync(
            int multaId,
            int usuarioResponsableId,
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var multa = await _multas.ObtenerPorIdAsync(multaId, ct)
                    ?? throw new DomainException("La multa no existe.");

                multa.MarcarComoPagada();
                var auditoria = new Auditoria(
                    usuarioResponsableId,
                    ModuloAuditoria.Multas,
                    AccionAuditoria.Pagar,
                    $"Pago de la multa {multa.Id}.",
                    ResultadoAuditoria.Exitoso);

                _multas.Actualizar(multa);
                await _auditorias.AgregarAsync(auditoria, ct);
            }, IsolationLevel.Serializable, cancellationToken);
        }

        public async Task ResolverAsync(
            int multaId,
            int empleadoResolucionId,
            DateTime fechaResolucion,
            string observacion,
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var multa = await _multas.ObtenerPorIdAsync(multaId, ct)
                    ?? throw new DomainException("La multa no existe.");
                var empleado = await _empleados.ObtenerPorIdAsync(empleadoResolucionId, ct)
                    ?? throw new DomainException("El empleado responsable no existe.");

                multa.Resolver(empleado.Id, fechaResolucion, observacion);
                var auditoria = new Auditoria(
                    empleado.UsuarioId,
                    ModuloAuditoria.Multas,
                    AccionAuditoria.Resolver,
                    $"Resolución de la multa {multa.Id}.",
                    ResultadoAuditoria.Exitoso);

                _multas.Actualizar(multa);
                await _auditorias.AgregarAsync(auditoria, ct);
            }, IsolationLevel.Serializable, cancellationToken);
        }
    }
}
