using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging;
using SIGEBI.Application.Common;
using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Notificaciones;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Dtos.Notificaciones;
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
        private readonly INotificacionService _notificaciones;
        private readonly IUsuarioActual _usuarioActual;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PrestamoDomainService _prestamoDomainService;
        private readonly MultaDomainService _multaDomainService;
        private readonly IMapper _mapper;
        private readonly PoliticaPrestamos _politicaPrestamos;
        private readonly ILogger<PrestamoService> _logger;

        public PrestamoService(
            ISolicitudPrestamoRepository solicitudes,
            IUsuarioRepository usuarios,
            IEmpleadoRepository empleados,
            IPrestamoRepository prestamos,
            IMultaRepository multas,
            IInventarioRepository inventarios,
            IEjemplarRepository ejemplares,
            IAuditoriaWriter auditoria,
            INotificacionService notificaciones,
            IUsuarioActual usuarioActual,
            IUnitOfWork unitOfWork,
            PrestamoDomainService prestamoDomainService,
            MultaDomainService multaDomainService,
            IMapper mapper,
            PoliticaPrestamos politicaPrestamos,
            ILogger<PrestamoService> logger)
        {
            _solicitudes = solicitudes; _usuarios = usuarios; _empleados = empleados; _prestamos = prestamos;
            _multas = multas; _inventarios = inventarios; _ejemplares = ejemplares; _auditoria = auditoria;
            _notificaciones = notificaciones; _usuarioActual = usuarioActual;
            _unitOfWork = unitOfWork; _prestamoDomainService = prestamoDomainService;
            _multaDomainService = multaDomainService; _mapper = mapper; _politicaPrestamos = politicaPrestamos;
            _logger = logger;
        }

        public async Task<PrestamoDto> ObtenerPorIdAsync(int id, CancellationToken ct = default)
            => _mapper.Map<PrestamoDto>(await _prestamos.ObtenerPorIdAsync(id, ct) ?? throw new NotFoundException(nameof(Prestamo), id));

        public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorUsuarioAsync(int uId, CancellationToken ct = default)
            => _mapper.Map<IReadOnlyCollection<PrestamoDto>>(await _prestamos.ObtenerPorUsuarioAsync(uId, ct));

        public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorLibroAsync(int libroId, CancellationToken ct = default)
            => _mapper.Map<IReadOnlyCollection<PrestamoDto>>(await _prestamos.ObtenerPorLibroAsync(libroId, ct));

        public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorEjemplarAsync(int ejemplarId, CancellationToken ct = default)
            => _mapper.Map<IReadOnlyCollection<PrestamoDto>>(await _prestamos.ObtenerPorEjemplarAsync(ejemplarId, ct));

        public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerDevolucionesPorUsuarioAsync(int usuarioId, CancellationToken ct = default)
            => _mapper.Map<IReadOnlyCollection<PrestamoDto>>(await _prestamos.ObtenerDevolucionesPorUsuarioAsync(usuarioId, ct));

        public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerDevolucionesPorLibroAsync(int libroId, CancellationToken ct = default)
            => _mapper.Map<IReadOnlyCollection<PrestamoDto>>(await _prestamos.ObtenerDevolucionesPorLibroAsync(libroId, ct));

        public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorEstadoAsync(string est, CancellationToken ct = default)
            => _mapper.Map<IReadOnlyCollection<PrestamoDto>>(await _prestamos.ObtenerPorEstadoAsync(ConvertirEstadoPrestamo(est), ct));

        public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorRangoAsync(DateTime d, DateTime h, CancellationToken ct = default)
            => _mapper.Map<IReadOnlyCollection<PrestamoDto>>(await _prestamos.ObtenerPorRangoAsync(d, h, ct));

        public Task<IReadOnlyCollection<PrestamoDto>> ObtenerActivosAsync(CancellationToken ct = default) => ObtenerPorEstadoAsync(nameof(EstadoPrestamo.Activo), ct);

        public Task<IReadOnlyCollection<PrestamoDto>> ObtenerVencidosAsync(CancellationToken ct = default) => ObtenerPorEstadoAsync(nameof(EstadoPrestamo.Vencido), ct);

        public async Task<int> ActualizarPrestamosVencidosAsync(ActualizarPrestamosVencidosDto dto, CancellationToken ct = default)
        {
            try
            {
                var cantidad = 0;
                await _unitOfWork.EjecutarEnTransaccionAsync(async c => {
                    var responsable = ResolverUsuarioResponsable(dto.UsuarioResponsableId);
                    foreach (var p in await _prestamos.ObtenerActivosVencidosAsync(dto.FechaReferencia, c))
                    {
                        p.MarcarComoVencido(dto.FechaReferencia);
                        _prestamos.Actualizar(p);
                        await _auditoria.RegistrarAsync(
                            responsable,
                            ModuloAuditoria.Prestamos,
                            AccionAuditoria.ActualizarEstado,
                            $"Préstamo {p.Id} marcado como vencido.",
                            cancellationToken: c);
                        await NotificarAsync(
                            p.UsuarioId,
                            $"El préstamo #{p.Id} está vencido desde {p.FechaEsperadaDevolucion:dd/MM/yyyy}.",
                            "Vencimiento",
                            c);
                        cantidad++;
                    }
                }, IsolationLevel.Serializable, ct);
                return cantidad;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error al vencer préstamos"); throw; }
        }

        public async Task<int> GenerarRecordatoriosVencimientoAsync(
            DateTime fechaReferencia,
            CancellationToken ct = default)
        {
            var desde = fechaReferencia.Date;
            var hasta = desde.AddDays(_politicaPrestamos.DiasAnticipacionRecordatorio + 1).AddTicks(-1);
            var enviados = 0;

            foreach (var prestamo in await _prestamos.ObtenerActivosProximosAVencerAsync(desde, hasta, ct))
            {
                var identificador = $"préstamo #{prestamo.Id}";
                var enviado = await _notificaciones.EnviarSiNoExisteAsync(
                    new SaveNotificacionDto
                    {
                        UsuarioId = prestamo.UsuarioId,
                        TipoEvento = "Vencimiento",
                        Mensaje = $"Recordatorio: el {identificador} vence el {prestamo.FechaEsperadaDevolucion:dd/MM/yyyy}."
                    },
                    identificador,
                    desde,
                    ct);
                if (enviado)
                    enviados++;
            }

            return enviados;
        }

        public async Task<PrestamoDto> RegistrarPrestamoAsync(RegistrarPrestamoDto dto, CancellationToken ct = default)
        {
            try
            {
                Prestamo? prestamoRegistrado = null;
                await _unitOfWork.EjecutarEnTransaccionAsync(async c => {
                    var solicitud = await _solicitudes.ObtenerPorIdAsync(dto.SolicitudPrestamoId, c) ?? throw new NotFoundException("SolicitudPrestamo", dto.SolicitudPrestamoId);
                    var usuario = await _usuarios.ObtenerPorIdAsync(solicitud.UsuarioId, c) ?? throw new NotFoundException("Usuario", solicitud.UsuarioId);
                    var empleado = await ResolverEmpleadoAsync(dto.EmpleadoPrestamoId, c);
                    var inventario = await _inventarios.ObtenerPorLibroIdAsync(solicitud.LibroId, c) ?? throw new BusinessRuleException("El libro no posee un inventario registrado.");
                    var ejemplar = await _ejemplares.ObtenerDisponiblePorLibroAsync(solicitud.LibroId, c) ?? throw new BusinessRuleException("No hay ejemplares disponibles.");
                    solicitud.Aprobar();
                    _solicitudes.Actualizar(solicitud);
                    prestamoRegistrado = _prestamoDomainService.RegistrarPrestamo(usuario.Id, usuario.Estado == EstadoUsuario.Activo, _multaDomainService.TieneMultasPendientes(await _multas.ObtenerPorUsuarioAsync(usuario.Id, c)), await _prestamos.TieneVencidosPorUsuarioAsync(usuario.Id, c), await _prestamos.ContarActivosPorUsuarioAsync(usuario.Id, c), _politicaPrestamos.ObtenerCondiciones(usuario.TipoUsuario).LimitePrestamos, solicitud, empleado.Id, dto.FechaPrestamo, _politicaPrestamos.CalcularFechaLimite(usuario.TipoUsuario, dto.FechaPrestamo), inventario, ejemplar);
                    await _prestamos.AgregarAsync(prestamoRegistrado, c);
                    _inventarios.Actualizar(inventario); _ejemplares.Actualizar(ejemplar);
                    await _auditoria.RegistrarAsync(
                        empleado.UsuarioId,
                        ModuloAuditoria.Prestamos,
                        AccionAuditoria.Aprobar,
                        $"Préstamo {prestamoRegistrado.Id} formalizado desde solicitud {solicitud.Id}; ejemplar {ejemplar.Codigo}.",
                        cancellationToken: c);
                    await NotificarAsync(
                        usuario.Id,
                        $"Su préstamo #{prestamoRegistrado.Id} fue formalizado. Fecha límite: {prestamoRegistrado.FechaEsperadaDevolucion:dd/MM/yyyy}.",
                        "Informacion",
                        c);
                }, IsolationLevel.Serializable, ct);
                return _mapper.Map<PrestamoDto>(prestamoRegistrado ?? throw new InvalidOperationException("Fallo en registro de préstamo."));
            }
            catch (Exception ex) { _logger.LogError(ex, "Error crítico al registrar préstamo para solicitud {Id}", dto.SolicitudPrestamoId); throw; }
        }

        public async Task RechazarSolicitudAsync(RechazarSolicitudPrestamoDto dto, CancellationToken ct = default)
        {
            try
            {
                await _unitOfWork.EjecutarEnTransaccionAsync(async c => {
                    var s = await _solicitudes.ObtenerPorIdAsync(dto.SolicitudPrestamoId, c) ?? throw new NotFoundException("Solicitud", dto.SolicitudPrestamoId);
                    var empleado = await ResolverEmpleadoAsync(dto.EmpleadoResponsableId, c);
                    s.Rechazar(dto.Motivo);
                    _solicitudes.Actualizar(s);
                    await _auditoria.RegistrarAsync(
                        empleado.UsuarioId,
                        ModuloAuditoria.Solicitudes,
                        AccionAuditoria.Rechazar,
                        $"Solicitud {s.Id} rechazada. Motivo: {dto.Motivo}",
                        cancellationToken: c);
                    await NotificarAsync(
                        s.UsuarioId,
                        $"Su solicitud #{s.Id} fue rechazada. Motivo: {dto.Motivo}",
                        "Alerta",
                        c);
                }, IsolationLevel.Serializable, ct);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error al rechazar solicitud {Id}", dto.SolicitudPrestamoId); throw; }
        }

        public async Task<MultaDto?> RegistrarDevolucionAsync(RegistrarDevolucionDto dto, CancellationToken ct = default)
        {
            try
            {
                Multa? multa = null;
                await _unitOfWork.EjecutarEnTransaccionAsync(async c => {
                    var p = await _prestamos.ObtenerPorIdAsync(dto.PrestamoId, c) ?? throw new NotFoundException("Prestamo", dto.PrestamoId);
                    var em = await ResolverEmpleadoAsync(dto.EmpleadoDevolucionId, c);
                    var i = await _inventarios.ObtenerPorLibroIdAsync(p.LibroId, c) ?? throw new BusinessRuleException("Inventario no encontrado.");
                    var e = await _ejemplares.ObtenerPorIdAsync(p.EjemplarId, c) ?? throw new NotFoundException("Ejemplar", p.EjemplarId);
                    if (_prestamoDomainService.RegistrarDevolucion(p, i, e, em.Id, dto.FechaRealDevolucion))
                        multa = _multaDomainService.GenerarMultaPorRetraso(p, _politicaPrestamos.MontoMultaPorDia, await _multas.ObtenerPorUsuarioAsync(p.UsuarioId, c));
                    _prestamos.Actualizar(p); _inventarios.Actualizar(i); _ejemplares.Actualizar(e);
                    if (multa != null) await _multas.AgregarAsync(multa, c);
                    await _auditoria.RegistrarAsync(
                        em.UsuarioId,
                        ModuloAuditoria.Prestamos,
                        AccionAuditoria.Devolver,
                        $"Devolución del préstamo {p.Id} registrada; ejemplar {e.Codigo}; penalización: {(multa is null ? "no" : $"sí, multa {multa.Id}")}.",
                        cancellationToken: c);
                    await NotificarAsync(
                        p.UsuarioId,
                        multa is null
                            ? $"Se confirmó la devolución del préstamo #{p.Id}."
                            : $"Se confirmó la devolución del préstamo #{p.Id} y se generó una multa de {multa.Monto:C}.",
                        multa is null ? "Informacion" : "Multa",
                        c);
                }, IsolationLevel.Serializable, ct);
                return multa == null ? null : _mapper.Map<MultaDto>(multa);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error en devolución {Id}", dto.PrestamoId); throw; }
        }

        public async Task CancelarPrestamoAsync(CancelarPrestamoDto dto, CancellationToken ct = default)
        {
            try
            {
                await _unitOfWork.EjecutarEnTransaccionAsync(async c => {
                    var p = await _prestamos.ObtenerPorIdAsync(dto.PrestamoId, c) ?? throw new NotFoundException("Prestamo", dto.PrestamoId);
                    var empleado = await ResolverEmpleadoAsync(dto.EmpleadoResponsableId, c);
                    var i = await _inventarios.ObtenerPorLibroIdAsync(p.LibroId, c) ?? throw new BusinessRuleException("Inventario no encontrado.");
                    var e = await _ejemplares.ObtenerPorIdAsync(p.EjemplarId, c) ?? throw new NotFoundException("Ejemplar", p.EjemplarId);
                    _prestamoDomainService.CancelarPrestamo(p, i, e);
                    _prestamos.Actualizar(p); _inventarios.Actualizar(i); _ejemplares.Actualizar(e);
                    await _auditoria.RegistrarAsync(
                        empleado.UsuarioId,
                        ModuloAuditoria.Prestamos,
                        AccionAuditoria.Cancelar,
                        $"Préstamo {p.Id} cancelado. Motivo: {dto.Motivo}",
                        cancellationToken: c);
                    await NotificarAsync(p.UsuarioId, $"El préstamo #{p.Id} fue cancelado. Motivo: {dto.Motivo}", "Alerta", c);
                }, IsolationLevel.Serializable, ct);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error al cancelar préstamo {Id}", dto.PrestamoId); throw; }
        }

        public Task<MultaDto> RegistrarPerdidaAsync(RegistrarPerdidaDto dto, CancellationToken ct = default) => RegistrarIncidenciaAsync(dto.PrestamoId, dto.EmpleadoResponsableId, dto.FechaReporte, _politicaPrestamos.MontoMultaPorPerdida, dto.Motivo, true, ct);

        public Task<MultaDto> RegistrarDevolucionConDanioAsync(RegistrarDanioDto dto, CancellationToken ct = default) => RegistrarIncidenciaAsync(dto.PrestamoId, dto.EmpleadoResponsableId, dto.FechaDevolucion, _politicaPrestamos.MontoMultaPorDanio, dto.Motivo, false, ct);

        private async Task<MultaDto> RegistrarIncidenciaAsync(int pId, int empId, DateTime fecha, decimal monto, string motivo, bool esPerdida, CancellationToken ct)
        {
            try
            {
                Multa? multa = null;
                await _unitOfWork.EjecutarEnTransaccionAsync(async c => {
                    var p = await _prestamos.ObtenerPorIdAsync(pId, c) ?? throw new NotFoundException("Prestamo", pId);
                    var em = await ResolverEmpleadoAsync(empId, c);
                    var i = await _inventarios.ObtenerPorLibroIdAsync(p.LibroId, c) ?? throw new BusinessRuleException("Inventario no encontrado.");
                    var e = await _ejemplares.ObtenerPorIdAsync(p.EjemplarId, c) ?? throw new NotFoundException("Ejemplar", p.EjemplarId);
                    if (esPerdida) { _prestamoDomainService.RegistrarPerdida(p, i, e, em.Id, fecha); multa = _multaDomainService.GenerarMultaPorPerdida(p, monto, motivo, await _multas.ObtenerPorUsuarioAsync(p.UsuarioId, c)); }
                    else { _prestamoDomainService.RegistrarDevolucionConDanio(p, i, e, em.Id, fecha); multa = _multaDomainService.GenerarMultaPorDanio(p, monto, motivo, await _multas.ObtenerPorUsuarioAsync(p.UsuarioId, c)); }
                    _prestamos.Actualizar(p); _inventarios.Actualizar(i); _ejemplares.Actualizar(e);
                    if (multa != null) await _multas.AgregarAsync(multa, c);
                    await _auditoria.RegistrarAsync(
                        em.UsuarioId,
                        ModuloAuditoria.Prestamos,
                        esPerdida ? AccionAuditoria.RegistrarPerdida : AccionAuditoria.RegistrarDanio,
                        $"{(esPerdida ? "Pérdida" : "Daño")} del préstamo {p.Id}; ejemplar {e.Codigo}; multa {multa?.Id}. Motivo: {motivo}",
                        cancellationToken: c);
                    await NotificarAsync(
                        p.UsuarioId,
                        $"Se registró {(esPerdida ? "la pérdida" : "un daño")} del préstamo #{p.Id}. Multa generada: {multa?.Monto:C}.",
                        "Multa",
                        c);
                }, IsolationLevel.Serializable, ct);
                return _mapper.Map<MultaDto>(multa ?? throw new InvalidOperationException("No se pudo generar multa."));
            }
            catch (Exception ex) { _logger.LogError(ex, "Error al registrar incidencia préstamo {Id}", pId); throw; }
        }

        private async Task<Empleado> ResolverEmpleadoAsync(int empleadoInformadoId, CancellationToken ct)
        {
            if (_usuarioActual.EstaAutenticado)
            {
                return await _empleados.ObtenerPorUsuarioIdAsync(_usuarioActual.UsuarioId, ct)
                    ?? throw new BusinessRuleException("El usuario autenticado no está registrado como empleado.");
            }

            return await _empleados.ObtenerPorIdAsync(empleadoInformadoId, ct)
                ?? throw new NotFoundException("Empleado", empleadoInformadoId);
        }

        private int ResolverUsuarioResponsable(int usuarioInformadoId)
        {
            var usuarioId = _usuarioActual.EstaAutenticado
                ? _usuarioActual.UsuarioId
                : usuarioInformadoId;
            if (usuarioId <= 0)
                throw new BusinessRuleException("No se pudo determinar el usuario responsable.");
            return usuarioId;
        }

        private Task NotificarAsync(
            int usuarioId,
            string mensaje,
            string tipo,
            CancellationToken ct) =>
            _notificaciones.EnviarNotificacionAsync(
                new SaveNotificacionDto
                {
                    UsuarioId = usuarioId,
                    Mensaje = mensaje,
                    TipoEvento = tipo
                },
                ct);

        private static EstadoPrestamo ConvertirEstadoPrestamo(string e) => EnumParser.ParseDefined<EstadoPrestamo>(e, "estado");
    }
}
