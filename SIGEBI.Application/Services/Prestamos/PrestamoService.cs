using System.Data;
using AutoMapper;
using SIGEBI.Application.Common;
using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Exceptions;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Services;
using SIGEBI.Domain.Policies;
using InventarioCatalogo = SIGEBI.Domain.Entities.Catalogo.Inventario;

namespace SIGEBI.Application.Services.Prestamos
{
    // Capa de Aplicación: coordina repositorios, dominio y DTOs sin exponer entidades a capas externas.
    public class PrestamoService : IPrestamoService
    {
        private readonly ISolicitudPrestamoRepository _solicitudes;
        private readonly IUsuarioRepository _usuarios;
        private readonly IEmpleadoRepository _empleados;
        private readonly IPrestamoRepository _prestamos;
        private readonly IMultaRepository _multas;
        private readonly IInventarioRepository _inventarios;
        private readonly IEjemplarRepository _ejemplares;
        private readonly IAuditoriaWriter _auditoria;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PrestamoDomainService _prestamoDomainService;
        private readonly MultaDomainService _multaDomainService;
        private readonly IMapper _mapper;
        private readonly PoliticaPrestamos _politicaPrestamos;

        public PrestamoService(
            ISolicitudPrestamoRepository solicitudes,
            IUsuarioRepository usuarios,
            IEmpleadoRepository empleados,
            IPrestamoRepository prestamos,
            IMultaRepository multas,
            IInventarioRepository inventarios,
            IEjemplarRepository ejemplares,
            IAuditoriaWriter auditoria,
            IUnitOfWork unitOfWork,
            PrestamoDomainService prestamoDomainService,
            MultaDomainService multaDomainService,
            IMapper mapper,
            PoliticaPrestamos politicaPrestamos)
        {
            _solicitudes = solicitudes;
            _usuarios = usuarios;
            _empleados = empleados;
            _prestamos = prestamos;
            _multas = multas;
            _inventarios = inventarios;
            _ejemplares = ejemplares;
            _auditoria = auditoria;
            _unitOfWork = unitOfWork;
            _prestamoDomainService = prestamoDomainService;
            _multaDomainService = multaDomainService;
            _mapper = mapper;
            _politicaPrestamos = politicaPrestamos;
        }

        public async Task<PrestamoDto> ObtenerPorIdAsync(
            int prestamoId,
            CancellationToken cancellationToken = default)
        {
            var prestamo = await _prestamos.ObtenerPorIdAsync(prestamoId, cancellationToken)
                ?? throw new NotFoundException(nameof(Prestamo), prestamoId);

            return _mapper.Map<PrestamoDto>(prestamo);
        }

        public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            var prestamos = await _prestamos.ObtenerPorUsuarioAsync(usuarioId, cancellationToken);
            return _mapper.Map<IReadOnlyCollection<PrestamoDto>>(prestamos);
        }

        public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorEstadoAsync(
            string estado,
            CancellationToken cancellationToken = default)
        {
            var estadoPrestamo = ConvertirEstadoPrestamo(estado);
            var prestamos = await _prestamos.ObtenerPorEstadoAsync(estadoPrestamo, cancellationToken);
            return _mapper.Map<IReadOnlyCollection<PrestamoDto>>(prestamos);
        }

        public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorRangoAsync(
            DateTime fechaDesde,
            DateTime fechaHasta,
            CancellationToken cancellationToken = default)
            => _mapper.Map<IReadOnlyCollection<PrestamoDto>>(
                await _prestamos.ObtenerPorRangoAsync(fechaDesde, fechaHasta, cancellationToken));

        public Task<IReadOnlyCollection<PrestamoDto>> ObtenerActivosAsync(
            CancellationToken cancellationToken = default)
            => ObtenerPorEstadoAsync(nameof(EstadoPrestamo.Activo), cancellationToken);

        public Task<IReadOnlyCollection<PrestamoDto>> ObtenerVencidosAsync(
            CancellationToken cancellationToken = default)
            => ObtenerPorEstadoAsync(nameof(EstadoPrestamo.Vencido), cancellationToken);

        public async Task<int> ActualizarPrestamosVencidosAsync(
            ActualizarPrestamosVencidosDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            if (dto.UsuarioResponsableId <= 0)
                throw new BusinessRuleException("Debe indicar el usuario responsable.");

            var fechaReferencia = dto.FechaReferencia == default
                ? DateTime.UtcNow
                : dto.FechaReferencia;
            var cantidadActualizada = 0;

            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var prestamos = await _prestamos.ObtenerActivosVencidosAsync(fechaReferencia, ct);
                foreach (var prestamo in prestamos)
                {
                    prestamo.MarcarComoVencido(fechaReferencia);
                    _prestamos.Actualizar(prestamo);
                    cantidadActualizada++;
                }

                if (cantidadActualizada > 0)
                {
                    await _auditoria.RegistrarAsync(
                        dto.UsuarioResponsableId,
                        ModuloAuditoria.Prestamos,
                        AccionAuditoria.ActualizarEstado,
                        $"Se marcaron {cantidadActualizada} préstamos como vencidos.",
                        cancellationToken: ct);
                }
            }, cancellationToken);

            return cantidadActualizada;
        }

        public async Task<PrestamoDto> RegistrarPrestamoAsync(
            RegistrarPrestamoDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            Prestamo? prestamoRegistrado = null;

            // Separación de responsabilidades: Application orquesta y Domain valida las reglas del préstamo.
            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var solicitud = await _solicitudes.ObtenerPorIdAsync(dto.SolicitudPrestamoId, ct)
                    ?? throw new NotFoundException("SolicitudPrestamo", dto.SolicitudPrestamoId);
                var usuario = await _usuarios.ObtenerPorIdAsync(solicitud.UsuarioId, ct)
                    ?? throw new NotFoundException("Usuario", solicitud.UsuarioId);
                var empleado = await _empleados.ObtenerPorIdAsync(dto.EmpleadoPrestamoId, ct)
                    ?? throw new NotFoundException("Empleado", dto.EmpleadoPrestamoId);
                var inventario = await _inventarios.ObtenerPorLibroIdAsync(solicitud.LibroId, ct)
                    ?? throw new BusinessRuleException("El libro no posee un inventario registrado.");
                var ejemplar = await _ejemplares.ObtenerDisponiblePorLibroAsync(solicitud.LibroId, ct)
                    ?? throw new BusinessRuleException("No existe un ejemplar disponible para el libro solicitado.");
                var multasUsuario = await _multas.ObtenerPorUsuarioAsync(usuario.Id, ct);
                var cantidadActivos = await _prestamos.ContarActivosPorUsuarioAsync(usuario.Id, ct);
                var tieneVencidos = await _prestamos.TieneVencidosPorUsuarioAsync(usuario.Id, ct);
                var condiciones = _politicaPrestamos.ObtenerCondiciones(usuario.TipoUsuario);
                var fechaLimite = _politicaPrestamos.CalcularFechaLimite(usuario.TipoUsuario, dto.FechaPrestamo);

                if (solicitud.Estado == EstadoSolicitud.Pendiente)
                {
                    solicitud.Aprobar();
                    _solicitudes.Actualizar(solicitud);
                    await _auditoria.RegistrarAsync(
                        empleado.UsuarioId,
                        ModuloAuditoria.Solicitudes,
                        AccionAuditoria.Aprobar,
                        $"Aprobación de la solicitud {solicitud.Id}.",
                        cancellationToken: ct);
                }

                prestamoRegistrado = _prestamoDomainService.RegistrarPrestamo(
                    usuario.Id,
                    usuario.Estado == EstadoUsuario.Activo,
                    _multaDomainService.TieneMultasPendientes(multasUsuario),
                    tieneVencidos,
                    cantidadActivos,
                    condiciones.LimitePrestamos,
                    solicitud,
                    empleado.Id,
                    dto.FechaPrestamo,
                    fechaLimite,
                    inventario,
                    ejemplar);

                await _prestamos.AgregarAsync(prestamoRegistrado, ct);
                _inventarios.Actualizar(inventario);
                _ejemplares.Actualizar(ejemplar);
                await _auditoria.RegistrarAsync(
                    empleado.UsuarioId,
                    ModuloAuditoria.Prestamos,
                    AccionAuditoria.Registrar,
                    $"Registro del préstamo asociado a la solicitud {solicitud.Id}.",
                    cancellationToken: ct);
            }, IsolationLevel.Serializable, cancellationToken);

            return _mapper.Map<PrestamoDto>(
                prestamoRegistrado ?? throw new InvalidOperationException("No fue posible completar el registro del préstamo."));
        }

        public async Task RechazarSolicitudAsync(
            RechazarSolicitudPrestamoDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var solicitud = await _solicitudes.ObtenerPorIdAsync(dto.SolicitudPrestamoId, ct)
                    ?? throw new NotFoundException("SolicitudPrestamo", dto.SolicitudPrestamoId);
                var empleado = await _empleados.ObtenerPorIdAsync(dto.EmpleadoResponsableId, ct)
                    ?? throw new NotFoundException("Empleado", dto.EmpleadoResponsableId);

                solicitud.Rechazar(dto.Motivo);
                _solicitudes.Actualizar(solicitud);
                await _auditoria.RegistrarAsync(
                    empleado.UsuarioId,
                    ModuloAuditoria.Solicitudes,
                    AccionAuditoria.Rechazar,
                    $"Rechazo de la solicitud {solicitud.Id}. Motivo: {dto.Motivo.Trim()}",
                    cancellationToken: ct);
            }, IsolationLevel.Serializable, cancellationToken);
        }

        public async Task<MultaDto?> RegistrarDevolucionAsync(
            RegistrarDevolucionDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            Multa? multaGenerada = null;

            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var contexto = await CargarContextoAsync(dto.PrestamoId, dto.EmpleadoDevolucionId, ct);
                var (prestamo, empleado, inventario, ejemplar) = contexto;
                var multasUsuario = await _multas.ObtenerPorUsuarioAsync(prestamo.UsuarioId, ct);

                var fueTardia = _prestamoDomainService.RegistrarDevolucion(
                    prestamo,
                    inventario,
                    ejemplar,
                    empleado.Id,
                    dto.FechaRealDevolucion);

                if (fueTardia)
                {
                    multaGenerada = _multaDomainService.GenerarMultaPorRetraso(
                        prestamo,
                        dto.MontoMultaPorDia,
                        multasUsuario);
                }

                ActualizarContexto(contexto);
                if (multaGenerada is not null)
                    await _multas.AgregarAsync(multaGenerada, ct);
                await _auditoria.RegistrarAsync(
                    empleado.UsuarioId,
                    ModuloAuditoria.Prestamos,
                    AccionAuditoria.Devolver,
                    $"Devolución del préstamo {prestamo.Id}.",
                    cancellationToken: ct);
            }, IsolationLevel.Serializable, cancellationToken);

            return multaGenerada is null ? null : _mapper.Map<MultaDto>(multaGenerada);
        }

        public async Task CancelarPrestamoAsync(
            CancelarPrestamoDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var contexto = await CargarContextoAsync(dto.PrestamoId, dto.EmpleadoResponsableId, ct);
                var (prestamo, empleado, inventario, ejemplar) = contexto;

                _prestamoDomainService.CancelarPrestamo(prestamo, inventario, ejemplar);
                ActualizarContexto(contexto);
                await _auditoria.RegistrarAsync(
                    empleado.UsuarioId,
                    ModuloAuditoria.Prestamos,
                    AccionAuditoria.Cancelar,
                    $"Cancelación del préstamo {prestamo.Id}.",
                    cancellationToken: ct);
            }, IsolationLevel.Serializable, cancellationToken);
        }

        public Task<MultaDto> RegistrarPerdidaAsync(
            RegistrarPerdidaDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            return RegistrarIncidenciaAsync(
                dto.PrestamoId,
                dto.EmpleadoResponsableId,
                dto.FechaReporte,
                dto.MontoMulta,
                dto.Motivo,
                esPerdida: true,
                cancellationToken);
        }

        public Task<MultaDto> RegistrarDevolucionConDanioAsync(
            RegistrarDanioDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            return RegistrarIncidenciaAsync(
                dto.PrestamoId,
                dto.EmpleadoResponsableId,
                dto.FechaDevolucion,
                dto.MontoMulta,
                dto.Motivo,
                esPerdida: false,
                cancellationToken);
        }

        private async Task<MultaDto> RegistrarIncidenciaAsync(
            int prestamoId,
            int empleadoResponsableId,
            DateTime fechaIncidencia,
            decimal montoMulta,
            string motivo,
            bool esPerdida,
            CancellationToken cancellationToken)
        {
            Multa? multaGenerada = null;

            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var contexto = await CargarContextoAsync(prestamoId, empleadoResponsableId, ct);
                var (prestamo, empleado, inventario, ejemplar) = contexto;
                var multasUsuario = await _multas.ObtenerPorUsuarioAsync(prestamo.UsuarioId, ct);

                if (esPerdida)
                {
                    _prestamoDomainService.RegistrarPerdida(
                        prestamo,
                        inventario,
                        ejemplar,
                        empleado.Id,
                        fechaIncidencia);
                    multaGenerada = _multaDomainService.GenerarMultaPorPerdida(
                        prestamo,
                        montoMulta,
                        motivo,
                        multasUsuario);
                }
                else
                {
                    _prestamoDomainService.RegistrarDevolucionConDanio(
                        prestamo,
                        inventario,
                        ejemplar,
                        empleado.Id,
                        fechaIncidencia);
                    multaGenerada = _multaDomainService.GenerarMultaPorDanio(
                        prestamo,
                        montoMulta,
                        motivo,
                        multasUsuario);
                }

                ActualizarContexto(contexto);
                await _multas.AgregarAsync(multaGenerada, ct);
                await _auditoria.RegistrarAsync(
                    empleado.UsuarioId,
                    ModuloAuditoria.Inventario,
                    esPerdida ? AccionAuditoria.RegistrarPerdida : AccionAuditoria.RegistrarDanio,
                    $"Incidencia registrada para el préstamo {prestamo.Id}.",
                    cancellationToken: ct);
            }, IsolationLevel.Serializable, cancellationToken);

            return _mapper.Map<MultaDto>(
                multaGenerada ?? throw new InvalidOperationException("No fue posible registrar la incidencia."));
        }

        private async Task<ContextoPrestamo> CargarContextoAsync(
            int prestamoId,
            int empleadoId,
            CancellationToken cancellationToken)
        {
            var prestamo = await _prestamos.ObtenerPorIdAsync(prestamoId, cancellationToken)
                ?? throw new NotFoundException(nameof(Prestamo), prestamoId);
            var empleado = await _empleados.ObtenerPorIdAsync(empleadoId, cancellationToken)
                ?? throw new NotFoundException(nameof(Empleado), empleadoId);
            var inventario = await _inventarios.ObtenerPorLibroIdAsync(prestamo.LibroId, cancellationToken)
                ?? throw new BusinessRuleException("El libro no posee un inventario registrado.");
            var ejemplar = await _ejemplares.ObtenerPorIdAsync(prestamo.EjemplarId, cancellationToken)
                ?? throw new NotFoundException(nameof(Ejemplar), prestamo.EjemplarId);

            return new ContextoPrestamo(prestamo, empleado, inventario, ejemplar);
        }

        private void ActualizarContexto(ContextoPrestamo contexto)
        {
            _prestamos.Actualizar(contexto.Prestamo);
            _inventarios.Actualizar(contexto.Inventario);
            _ejemplares.Actualizar(contexto.Ejemplar);
        }

        private sealed record ContextoPrestamo(
            Prestamo Prestamo,
            Empleado Empleado,
            InventarioCatalogo Inventario,
            Ejemplar Ejemplar);

        private static EstadoPrestamo ConvertirEstadoPrestamo(string estado)
        {
            return EnumParser.ParseDefined<EstadoPrestamo>(estado, "estado del préstamo");
        }
    }
}
