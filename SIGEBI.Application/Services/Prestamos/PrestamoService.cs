using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging;
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
            IUnitOfWork unitOfWork,
            PrestamoDomainService prestamoDomainService,
            MultaDomainService multaDomainService,
            IMapper mapper,
            PoliticaPrestamos politicaPrestamos,
            ILogger<PrestamoService> logger)
        {
            _solicitudes = solicitudes; _usuarios = usuarios; _empleados = empleados; _prestamos = prestamos;
            _multas = multas; _inventarios = inventarios; _ejemplares = ejemplares; _auditoria = auditoria;
            _unitOfWork = unitOfWork; _prestamoDomainService = prestamoDomainService;
            _multaDomainService = multaDomainService; _mapper = mapper; _politicaPrestamos = politicaPrestamos;
            _logger = logger;
        }

        public async Task<PrestamoDto> ObtenerPorIdAsync(int id, CancellationToken ct = default)
            => _mapper.Map<PrestamoDto>(await _prestamos.ObtenerPorIdAsync(id, ct) ?? throw new NotFoundException(nameof(Prestamo), id));

        public async Task<IReadOnlyCollection<PrestamoDto>> ObtenerPorUsuarioAsync(int uId, CancellationToken ct = default)
            => _mapper.Map<IReadOnlyCollection<PrestamoDto>>(await _prestamos.ObtenerPorUsuarioAsync(uId, ct));

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
                    foreach (var p in await _prestamos.ObtenerActivosVencidosAsync(dto.FechaReferencia, c)) { p.MarcarComoVencido(dto.FechaReferencia); _prestamos.Actualizar(p); cantidad++; }
                }, IsolationLevel.Serializable, ct);
                return cantidad;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error al vencer préstamos"); throw; }
        }

        public async Task<PrestamoDto> RegistrarPrestamoAsync(RegistrarPrestamoDto dto, CancellationToken ct = default)
        {
            try
            {
                Prestamo? prestamoRegistrado = null;
                await _unitOfWork.EjecutarEnTransaccionAsync(async c => {
                    var solicitud = await _solicitudes.ObtenerPorIdAsync(dto.SolicitudPrestamoId, c) ?? throw new NotFoundException("SolicitudPrestamo", dto.SolicitudPrestamoId);
                    var usuario = await _usuarios.ObtenerPorIdAsync(solicitud.UsuarioId, c) ?? throw new NotFoundException("Usuario", solicitud.UsuarioId);
                    var empleado = await _empleados.ObtenerPorIdAsync(dto.EmpleadoPrestamoId, c) ?? throw new NotFoundException("Empleado", dto.EmpleadoPrestamoId);
                    var inventario = await _inventarios.ObtenerPorLibroIdAsync(solicitud.LibroId, c) ?? throw new BusinessRuleException("El libro no posee un inventario registrado.");
                    var ejemplar = await _ejemplares.ObtenerDisponiblePorLibroAsync(solicitud.LibroId, c) ?? throw new BusinessRuleException("No hay ejemplares disponibles.");
                    solicitud.Aprobar();
                    _solicitudes.Actualizar(solicitud);
                    prestamoRegistrado = _prestamoDomainService.RegistrarPrestamo(usuario.Id, usuario.Estado == EstadoUsuario.Activo, _multaDomainService.TieneMultasPendientes(await _multas.ObtenerPorUsuarioAsync(usuario.Id, c)), await _prestamos.TieneVencidosPorUsuarioAsync(usuario.Id, c), await _prestamos.ContarActivosPorUsuarioAsync(usuario.Id, c), _politicaPrestamos.ObtenerCondiciones(usuario.TipoUsuario).LimitePrestamos, solicitud, empleado.Id, dto.FechaPrestamo, _politicaPrestamos.CalcularFechaLimite(usuario.TipoUsuario, dto.FechaPrestamo), inventario, ejemplar);
                    await _prestamos.AgregarAsync(prestamoRegistrado, c);
                    _inventarios.Actualizar(inventario); _ejemplares.Actualizar(ejemplar);
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
                    s.Rechazar(dto.Motivo);
                    _solicitudes.Actualizar(s);
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
                    var em = await _empleados.ObtenerPorIdAsync(dto.EmpleadoDevolucionId, c) ?? throw new NotFoundException("Empleado", dto.EmpleadoDevolucionId);
                    var i = await _inventarios.ObtenerPorLibroIdAsync(p.LibroId, c) ?? throw new BusinessRuleException("Inventario no encontrado.");
                    var e = await _ejemplares.ObtenerPorIdAsync(p.EjemplarId, c) ?? throw new NotFoundException("Ejemplar", p.EjemplarId);
                    if (_prestamoDomainService.RegistrarDevolucion(p, i, e, em.Id, dto.FechaRealDevolucion))
                        multa = _multaDomainService.GenerarMultaPorRetraso(p, dto.MontoMultaPorDia, await _multas.ObtenerPorUsuarioAsync(p.UsuarioId, c));
                    _prestamos.Actualizar(p); _inventarios.Actualizar(i); _ejemplares.Actualizar(e);
                    if (multa != null) await _multas.AgregarAsync(multa, c);
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
                    var i = await _inventarios.ObtenerPorLibroIdAsync(p.LibroId, c) ?? throw new BusinessRuleException("Inventario no encontrado.");
                    var e = await _ejemplares.ObtenerPorIdAsync(p.EjemplarId, c) ?? throw new NotFoundException("Ejemplar", p.EjemplarId);
                    _prestamoDomainService.CancelarPrestamo(p, i, e);
                    _prestamos.Actualizar(p); _inventarios.Actualizar(i); _ejemplares.Actualizar(e);
                }, IsolationLevel.Serializable, ct);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error al cancelar préstamo {Id}", dto.PrestamoId); throw; }
        }

        public Task<MultaDto> RegistrarPerdidaAsync(RegistrarPerdidaDto dto, CancellationToken ct = default) => RegistrarIncidenciaAsync(dto.PrestamoId, dto.EmpleadoResponsableId, dto.FechaReporte, dto.MontoMulta, dto.Motivo, true, ct);

        public Task<MultaDto> RegistrarDevolucionConDanioAsync(RegistrarDanioDto dto, CancellationToken ct = default) => RegistrarIncidenciaAsync(dto.PrestamoId, dto.EmpleadoResponsableId, dto.FechaDevolucion, dto.MontoMulta, dto.Motivo, false, ct);

        private async Task<MultaDto> RegistrarIncidenciaAsync(int pId, int empId, DateTime fecha, decimal monto, string motivo, bool esPerdida, CancellationToken ct)
        {
            try
            {
                Multa? multa = null;
                await _unitOfWork.EjecutarEnTransaccionAsync(async c => {
                    var p = await _prestamos.ObtenerPorIdAsync(pId, c) ?? throw new NotFoundException("Prestamo", pId);
                    var em = await _empleados.ObtenerPorIdAsync(empId, c) ?? throw new NotFoundException("Empleado", empId);
                    var i = await _inventarios.ObtenerPorLibroIdAsync(p.LibroId, c) ?? throw new BusinessRuleException("Inventario no encontrado.");
                    var e = await _ejemplares.ObtenerPorIdAsync(p.EjemplarId, c) ?? throw new NotFoundException("Ejemplar", p.EjemplarId);
                    if (esPerdida) { _prestamoDomainService.RegistrarPerdida(p, i, e, em.Id, fecha); multa = _multaDomainService.GenerarMultaPorPerdida(p, monto, motivo, await _multas.ObtenerPorUsuarioAsync(p.UsuarioId, c)); }
                    else { _prestamoDomainService.RegistrarDevolucionConDanio(p, i, e, em.Id, fecha); multa = _multaDomainService.GenerarMultaPorDanio(p, monto, motivo, await _multas.ObtenerPorUsuarioAsync(p.UsuarioId, c)); }
                    _prestamos.Actualizar(p); _inventarios.Actualizar(i); _ejemplares.Actualizar(e);
                    if (multa != null) await _multas.AgregarAsync(multa, c);
                }, IsolationLevel.Serializable, ct);
                return _mapper.Map<MultaDto>(multa ?? throw new InvalidOperationException("No se pudo generar multa."));
            }
            catch (Exception ex) { _logger.LogError(ex, "Error al registrar incidencia préstamo {Id}", pId); throw; }
        }

        private static EstadoPrestamo ConvertirEstadoPrestamo(string e) => EnumParser.ParseDefined<EstadoPrestamo>(e, "estado");
    }
}