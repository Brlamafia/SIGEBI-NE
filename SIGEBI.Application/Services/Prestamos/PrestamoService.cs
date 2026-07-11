using System.Data;
using AutoMapper;
using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Exceptions;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Services;
using SIGEBI.Domain.Policies;
using AuditoriaEntidad = SIGEBI.Domain.Entities.Auditoria.Auditoria;

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
        private readonly IAuditoriaRepository _auditorias;
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
            IAuditoriaRepository auditorias,
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
            _auditorias = auditorias;
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
                    await _auditorias.AgregarAsync(new AuditoriaEntidad(
                        dto.UsuarioResponsableId,
                        ModuloAuditoria.Prestamos,
                        AccionAuditoria.ActualizarEstado,
                        $"Se marcaron {cantidadActualizada} préstamos como vencidos.",
                        ResultadoAuditoria.Exitoso), ct);
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
                var multasUsuario = await _multas.ObtenerPorUsuarioAsync(usuario.Id, ct);
                var cantidadActivos = await _prestamos.ContarActivosPorUsuarioAsync(usuario.Id, ct);
                var tieneVencidos = await _prestamos.TieneVencidosPorUsuarioAsync(usuario.Id, ct);

                prestamoRegistrado = _prestamoDomainService.RegistrarPrestamo(
                    usuario.Id,
                    usuario.Estado == EstadoUsuario.Activo,
                    _multaDomainService.TieneMultasPendientes(multasUsuario),
                    tieneVencidos,
                    cantidadActivos,
                    _politicaPrestamos.LimitePrestamosPorUsuario,
                    solicitud,
                    empleado.Id,
                    dto.FechaPrestamo,
                    dto.FechaEsperadaDevolucion,
                    inventario);

                var auditoria = new AuditoriaEntidad(
                    empleado.UsuarioId,
                    ModuloAuditoria.Prestamos,
                    AccionAuditoria.Registrar,
                    $"Registro del préstamo asociado a la solicitud {solicitud.Id}.",
                    ResultadoAuditoria.Exitoso);

                await _prestamos.AgregarAsync(prestamoRegistrado, ct);
                _inventarios.Actualizar(inventario);
                await _auditorias.AgregarAsync(auditoria, ct);
            }, IsolationLevel.Serializable, cancellationToken);

            return _mapper.Map<PrestamoDto>(
                prestamoRegistrado ?? throw new InvalidOperationException("No fue posible completar el registro del préstamo."));
        }

        public async Task<MultaDto?> RegistrarDevolucionAsync(
            RegistrarDevolucionDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            Multa? multaGenerada = null;

            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var prestamo = await _prestamos.ObtenerPorIdAsync(dto.PrestamoId, ct)
                    ?? throw new NotFoundException(nameof(Prestamo), dto.PrestamoId);
                var empleado = await _empleados.ObtenerPorIdAsync(dto.EmpleadoDevolucionId, ct)
                    ?? throw new NotFoundException("Empleado", dto.EmpleadoDevolucionId);
                var inventario = await _inventarios.ObtenerPorLibroIdAsync(prestamo.LibroId, ct)
                    ?? throw new BusinessRuleException("El libro no posee un inventario registrado.");
                var multasUsuario = await _multas.ObtenerPorUsuarioAsync(prestamo.UsuarioId, ct);

                var fueTardia = _prestamoDomainService.RegistrarDevolucion(
                    prestamo,
                    inventario,
                    empleado.Id,
                    dto.FechaRealDevolucion);

                if (fueTardia)
                {
                    multaGenerada = _multaDomainService.GenerarMultaPorRetraso(
                        prestamo,
                        dto.MontoMultaPorDia,
                        multasUsuario);
                }

                var auditoria = new AuditoriaEntidad(
                    empleado.UsuarioId,
                    ModuloAuditoria.Prestamos,
                    AccionAuditoria.Devolver,
                    $"Devolución del préstamo {prestamo.Id}.",
                    ResultadoAuditoria.Exitoso);

                _prestamos.Actualizar(prestamo);
                _inventarios.Actualizar(inventario);
                if (multaGenerada is not null)
                    await _multas.AgregarAsync(multaGenerada, ct);
                await _auditorias.AgregarAsync(auditoria, ct);
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
                var prestamo = await _prestamos.ObtenerPorIdAsync(dto.PrestamoId, ct)
                    ?? throw new NotFoundException(nameof(Prestamo), dto.PrestamoId);
                var empleado = await _empleados.ObtenerPorIdAsync(dto.EmpleadoResponsableId, ct)
                    ?? throw new NotFoundException("Empleado", dto.EmpleadoResponsableId);
                var inventario = await _inventarios.ObtenerPorLibroIdAsync(prestamo.LibroId, ct)
                    ?? throw new BusinessRuleException("El libro no posee un inventario registrado.");

                _prestamoDomainService.CancelarPrestamo(prestamo, inventario);
                var auditoria = new AuditoriaEntidad(
                    empleado.UsuarioId,
                    ModuloAuditoria.Prestamos,
                    AccionAuditoria.Cancelar,
                    $"Cancelación del préstamo {prestamo.Id}.",
                    ResultadoAuditoria.Exitoso);

                _prestamos.Actualizar(prestamo);
                _inventarios.Actualizar(inventario);
                await _auditorias.AgregarAsync(auditoria, ct);
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
                var prestamo = await _prestamos.ObtenerPorIdAsync(prestamoId, ct)
                    ?? throw new NotFoundException(nameof(Prestamo), prestamoId);
                var empleado = await _empleados.ObtenerPorIdAsync(empleadoResponsableId, ct)
                    ?? throw new NotFoundException("Empleado", empleadoResponsableId);
                var inventario = await _inventarios.ObtenerPorLibroIdAsync(prestamo.LibroId, ct)
                    ?? throw new BusinessRuleException("El libro no posee un inventario registrado.");
                var multasUsuario = await _multas.ObtenerPorUsuarioAsync(prestamo.UsuarioId, ct);

                if (esPerdida)
                {
                    _prestamoDomainService.RegistrarPerdida(
                        prestamo,
                        inventario,
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
                        empleado.Id,
                        fechaIncidencia);
                    multaGenerada = _multaDomainService.GenerarMultaPorDanio(
                        prestamo,
                        montoMulta,
                        motivo,
                        multasUsuario);
                }

                var auditoria = new AuditoriaEntidad(
                    empleado.UsuarioId,
                    ModuloAuditoria.Inventario,
                    esPerdida ? AccionAuditoria.RegistrarPerdida : AccionAuditoria.RegistrarDanio,
                    $"Incidencia registrada para el préstamo {prestamo.Id}.",
                    ResultadoAuditoria.Exitoso);

                _prestamos.Actualizar(prestamo);
                _inventarios.Actualizar(inventario);
                await _multas.AgregarAsync(multaGenerada, ct);
                await _auditorias.AgregarAsync(auditoria, ct);
            }, IsolationLevel.Serializable, cancellationToken);

            return _mapper.Map<MultaDto>(
                multaGenerada ?? throw new InvalidOperationException("No fue posible registrar la incidencia."));
        }

        private static EstadoPrestamo ConvertirEstadoPrestamo(string estado)
        {
            if (!Enum.TryParse<EstadoPrestamo>(estado, ignoreCase: true, out var estadoPrestamo)
                || !Enum.IsDefined(estadoPrestamo))
            {
                throw new BusinessRuleException("El estado del préstamo no es válido.");
            }

            return estadoPrestamo;
        }
    }
}
