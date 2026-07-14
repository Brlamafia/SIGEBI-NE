using System.Data;
using AutoMapper;
using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Common;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Application.Services.Prestamos
{
    // Capa de Aplicación: coordina los casos de uso de multas mediante DTOs.
    public class MultaService : IMultaService
    {
        private readonly IMultaRepository _multas;
        private readonly IEmpleadoRepository _empleados;
        private readonly IAuditoriaWriter _auditoria;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public MultaService(
            IMultaRepository multas,
            IEmpleadoRepository empleados,
            IAuditoriaWriter auditoria,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _multas = multas;
            _empleados = empleados;
            _auditoria = auditoria;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<MultaDto> ObtenerPorIdAsync(
            int multaId,
            CancellationToken cancellationToken = default)
        {
            var multa = await _multas.ObtenerPorIdAsync(multaId, cancellationToken)
                ?? throw new NotFoundException(nameof(Multa), multaId);

            return _mapper.Map<MultaDto>(multa);
        }

        public async Task<IReadOnlyCollection<MultaDto>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            var multas = await _multas.ObtenerPorUsuarioAsync(usuarioId, cancellationToken);
            return _mapper.Map<IReadOnlyCollection<MultaDto>>(multas);
        }

        public async Task<IReadOnlyCollection<MultaDto>> ObtenerPorEstadoAsync(
            string estado,
            CancellationToken cancellationToken = default)
        {
            var estadoMulta = ConvertirEstadoMulta(estado);
            var multas = await _multas.ObtenerPorEstadoAsync(estadoMulta, cancellationToken);
            return _mapper.Map<IReadOnlyCollection<MultaDto>>(multas);
        }

        public Task<bool> TienePendientesPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
            => _multas.TienePendientesPorUsuarioAsync(usuarioId, cancellationToken);

        public async Task MarcarComoPagadaAsync(
            PagarMultaDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var multa = await _multas.ObtenerPorIdAsync(dto.MultaId, ct)
                    ?? throw new NotFoundException(nameof(Multa), dto.MultaId);

                multa.MarcarComoPagada();
                _multas.Actualizar(multa);
                await _auditoria.RegistrarAsync(
                    dto.UsuarioResponsableId,
                    ModuloAuditoria.Multas,
                    AccionAuditoria.Pagar,
                    $"Pago de la multa {multa.Id}.",
                    cancellationToken: ct);
            }, IsolationLevel.Serializable, cancellationToken);
        }

        public async Task ResolverAsync(
            ResolverMultaDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var multa = await _multas.ObtenerPorIdAsync(dto.MultaId, ct)
                    ?? throw new NotFoundException(nameof(Multa), dto.MultaId);
                var empleado = await _empleados.ObtenerPorIdAsync(dto.EmpleadoResolucionId, ct)
                    ?? throw new NotFoundException("Empleado", dto.EmpleadoResolucionId);

                multa.Resolver(empleado.Id, dto.FechaResolucion, dto.Observacion);
                _multas.Actualizar(multa);
                await _auditoria.RegistrarAsync(
                    empleado.UsuarioId,
                    ModuloAuditoria.Multas,
                    AccionAuditoria.Resolver,
                    $"Resolución de la multa {multa.Id}.",
                    cancellationToken: ct);
            }, IsolationLevel.Serializable, cancellationToken);
        }

        private static EstadoMulta ConvertirEstadoMulta(string estado)
        {
            return EnumParser.ParseDefined<EstadoMulta>(estado, "estado de la multa");
        }
    }
}
