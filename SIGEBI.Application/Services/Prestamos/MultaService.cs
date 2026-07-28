using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging;
using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Common;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Application.Services.Prestamos
{
    public class MultaService : IMultaService
    {
        private readonly IMultaRepository _multas;
        private readonly IEmpleadoRepository _empleados;
        private readonly IAuditoriaWriter _auditoria;
        private readonly INotificacionService _notificaciones;
        private readonly IUsuarioActual _usuarioActual;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MultaService> _logger;

        public MultaService(
            IMultaRepository multas,
            IEmpleadoRepository empleados,
            IAuditoriaWriter auditoria,
            INotificacionService notificaciones,
            IUsuarioActual usuarioActual,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<MultaService> logger)
        {
            _multas = multas;
            _empleados = empleados;
            _auditoria = auditoria;
            _notificaciones = notificaciones;
            _usuarioActual = usuarioActual;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<MultaDto> ObtenerPorIdAsync(int multaId, CancellationToken ct = default)
        {
            var multa = await _multas.ObtenerPorIdAsync(multaId, ct)
                ?? throw new NotFoundException(nameof(Multa), multaId);
            return _mapper.Map<MultaDto>(multa);
        }

        public async Task<IReadOnlyCollection<MultaDto>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            var multas = await _multas.ObtenerPorUsuarioAsync(usuarioId, ct);
            return _mapper.Map<IReadOnlyCollection<MultaDto>>(multas);
        }

        public async Task<IReadOnlyCollection<MultaDto>> ObtenerPorEstadoAsync(string estado, CancellationToken ct = default)
        {
            var estadoMulta = ConvertirEstadoMulta(estado);
            var multas = await _multas.ObtenerPorEstadoAsync(estadoMulta, ct);
            return _mapper.Map<IReadOnlyCollection<MultaDto>>(multas);
        }

        public Task<bool> TienePendientesPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
            => _multas.TienePendientesPorUsuarioAsync(usuarioId, ct);

        public Task<decimal> ObtenerMontoPendientePorUsuarioAsync(
            int usuarioId,
            CancellationToken ct = default) =>
            _multas.ObtenerMontoPendientePorUsuarioAsync(usuarioId, ct);

        public async Task MarcarComoPagadaAsync(PagarMultaDto dto, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            try
            {
                await _unitOfWork.EjecutarEnTransaccionAsync(async c =>
                {
                    var multa = await _multas.ObtenerPorIdAsync(dto.MultaId, c)
                        ?? throw new NotFoundException(nameof(Multa), dto.MultaId);

                    multa.MarcarComoPagada();
                    _multas.Actualizar(multa);
                    var responsableId = ResolverResponsable(dto.UsuarioResponsableId);
                    await _auditoria.RegistrarAsync(responsableId, ModuloAuditoria.Multas, AccionAuditoria.Pagar, $"Pago de la multa {multa.Id}.", cancellationToken: c);
                    await _notificaciones.EnviarNotificacionAsync(
                        new SaveNotificacionDto
                        {
                            UsuarioId = multa.UsuarioId,
                            TipoEvento = "Multa",
                            Mensaje = $"Se registró el pago de la multa #{multa.Id}."
                        },
                        c);
                }, IsolationLevel.Serializable, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar pago de multa {Id}", dto.MultaId);
                throw;
            }
        }

        public async Task ResolverAsync(ResolverMultaDto dto, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            try
            {
                await _unitOfWork.EjecutarEnTransaccionAsync(async c =>
                {
                    var multa = await _multas.ObtenerPorIdAsync(dto.MultaId, c)
                        ?? throw new NotFoundException(nameof(Multa), dto.MultaId);
                    var empleado = await ResolverEmpleadoAsync(dto.EmpleadoResolucionId, c);

                    multa.Resolver(
                        empleado.Id,
                        DateTimeNormalizer.ToUtc(dto.FechaResolucion),
                        dto.Observacion);
                    _multas.Actualizar(multa);
                    await _auditoria.RegistrarAsync(empleado.UsuarioId, ModuloAuditoria.Multas, AccionAuditoria.Resolver, $"Resolución de la multa {multa.Id}.", cancellationToken: c);
                    await _notificaciones.EnviarNotificacionAsync(
                        new SaveNotificacionDto
                        {
                            UsuarioId = multa.UsuarioId,
                            TipoEvento = "Multa",
                            Mensaje = $"La multa #{multa.Id} fue resuelta. El usuario queda habilitado si no mantiene otras multas pendientes."
                        },
                        c);
                }, IsolationLevel.Serializable, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resolver multa {Id}", dto.MultaId);
                throw;
            }
        }

        private static EstadoMulta ConvertirEstadoMulta(string estado) => EnumParser.ParseDefined<EstadoMulta>(estado, "estado de la multa");

        private int ResolverResponsable(int informado) =>
            _usuarioActual.EstaAutenticado ? _usuarioActual.UsuarioId : informado;

        private async Task<SIGEBI.Domain.Entities.Usuarios.Empleado> ResolverEmpleadoAsync(
            int informado,
            CancellationToken ct)
        {
            if (_usuarioActual.EstaAutenticado)
            {
                return await _empleados.ObtenerPorUsuarioIdAsync(_usuarioActual.UsuarioId, ct)
                    ?? throw new BusinessRuleException("El usuario autenticado no está registrado como empleado.");
            }

            return await _empleados.ObtenerPorIdAsync(informado, ct)
                ?? throw new NotFoundException("Empleado", informado);
        }
    }
}
