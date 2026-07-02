using System.Data;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Domain.Entities.Auditoria;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Exceptions;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Services;

namespace SIGEBI.Application.Services.Prestamos
{
    // Application Service: coordina repositorios y dominio sin contener reglas de negocio internas.
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
            MultaDomainService multaDomainService)
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
        }

        public async Task<Prestamo> RegistrarPrestamoAsync(
            int solicitudPrestamoId,
            int empleadoPrestamoId,
            int limitePrestamos,
            DateTime fechaPrestamo,
            DateTime fechaEsperadaDevolucion,
            CancellationToken cancellationToken = default)
        {
            Prestamo? prestamoRegistrado = null;

            // Serializable mantiene juntas las lecturas de elegibilidad y la escritura final.
            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var solicitud = await _solicitudes.ObtenerPorIdAsync(solicitudPrestamoId, ct)
                    ?? throw new DomainException("La solicitud de préstamo no existe.");
                var usuario = await _usuarios.ObtenerPorIdAsync(solicitud.UsuarioId, ct)
                    ?? throw new DomainException("El usuario de la solicitud no existe.");
                var empleado = await _empleados.ObtenerPorIdAsync(empleadoPrestamoId, ct)
                    ?? throw new DomainException("El empleado responsable no existe.");
                var inventario = await _inventarios.ObtenerPorLibroIdAsync(solicitud.LibroId, ct)
                    ?? throw new DomainException("El libro no posee un inventario registrado.");
                var multasUsuario = await _multas.ObtenerPorUsuarioAsync(usuario.Id, ct);
                var cantidadActivos = await _prestamos.ContarActivosPorUsuarioAsync(usuario.Id, ct);
                var tieneVencidos = await _prestamos.TieneVencidosPorUsuarioAsync(usuario.Id, ct);

                prestamoRegistrado = _prestamoDomainService.RegistrarPrestamo(
                    usuario.Id,
                    usuario.Estado == EstadoUsuario.Activo,
                    _multaDomainService.TieneMultasPendientes(multasUsuario),
                    tieneVencidos,
                    cantidadActivos,
                    limitePrestamos,
                    solicitud,
                    empleado.Id,
                    fechaPrestamo,
                    fechaEsperadaDevolucion,
                    inventario);

                var auditoria = new Auditoria(
                    empleado.UsuarioId,
                    ModuloAuditoria.Prestamos,
                    AccionAuditoria.Registrar,
                    $"Registro del préstamo asociado a la solicitud {solicitud.Id}.",
                    ResultadoAuditoria.Exitoso);

                await _prestamos.AgregarAsync(prestamoRegistrado, ct);
                _inventarios.Actualizar(inventario);
                await _auditorias.AgregarAsync(auditoria, ct);
            }, IsolationLevel.Serializable, cancellationToken);

            return prestamoRegistrado
                ?? throw new InvalidOperationException("No fue posible completar el registro del préstamo.");
        }

        public async Task<Multa?> RegistrarDevolucionAsync(
            int prestamoId,
            int empleadoDevolucionId,
            DateTime fechaRealDevolucion,
            decimal montoMultaPorDia,
            CancellationToken cancellationToken = default)
        {
            Multa? multaGenerada = null;

            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var prestamo = await _prestamos.ObtenerPorIdAsync(prestamoId, ct)
                    ?? throw new DomainException("El préstamo no existe.");
                var empleado = await _empleados.ObtenerPorIdAsync(empleadoDevolucionId, ct)
                    ?? throw new DomainException("El empleado responsable no existe.");
                var inventario = await _inventarios.ObtenerPorLibroIdAsync(prestamo.LibroId, ct)
                    ?? throw new DomainException("El libro no posee un inventario registrado.");
                var multasUsuario = await _multas.ObtenerPorUsuarioAsync(prestamo.UsuarioId, ct);

                var fueTardia = _prestamoDomainService.RegistrarDevolucion(
                    prestamo,
                    inventario,
                    empleado.Id,
                    fechaRealDevolucion);

                if (fueTardia)
                {
                    multaGenerada = _multaDomainService.GenerarMultaPorRetraso(
                        prestamo,
                        montoMultaPorDia,
                        multasUsuario);
                }

                var auditoria = new Auditoria(
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

            return multaGenerada;
        }

        public async Task CancelarPrestamoAsync(
            int prestamoId,
            int empleadoResponsableId,
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var prestamo = await _prestamos.ObtenerPorIdAsync(prestamoId, ct)
                    ?? throw new DomainException("El préstamo no existe.");
                var empleado = await _empleados.ObtenerPorIdAsync(empleadoResponsableId, ct)
                    ?? throw new DomainException("El empleado responsable no existe.");
                var inventario = await _inventarios.ObtenerPorLibroIdAsync(prestamo.LibroId, ct)
                    ?? throw new DomainException("El libro no posee un inventario registrado.");

                _prestamoDomainService.CancelarPrestamo(prestamo, inventario);
                var auditoria = new Auditoria(
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

        public Task<Multa> RegistrarPerdidaAsync(
            int prestamoId,
            int empleadoResponsableId,
            DateTime fechaReporte,
            decimal montoMulta,
            string motivo,
            CancellationToken cancellationToken = default)
            => RegistrarIncidenciaAsync(
                prestamoId,
                empleadoResponsableId,
                fechaReporte,
                montoMulta,
                motivo,
                esPerdida: true,
                cancellationToken);

        public Task<Multa> RegistrarDevolucionConDanioAsync(
            int prestamoId,
            int empleadoResponsableId,
            DateTime fechaDevolucion,
            decimal montoMulta,
            string motivo,
            CancellationToken cancellationToken = default)
            => RegistrarIncidenciaAsync(
                prestamoId,
                empleadoResponsableId,
                fechaDevolucion,
                montoMulta,
                motivo,
                esPerdida: false,
                cancellationToken);

        private async Task<Multa> RegistrarIncidenciaAsync(
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
                    ?? throw new DomainException("El préstamo no existe.");
                var empleado = await _empleados.ObtenerPorIdAsync(empleadoResponsableId, ct)
                    ?? throw new DomainException("El empleado responsable no existe.");
                var inventario = await _inventarios.ObtenerPorLibroIdAsync(prestamo.LibroId, ct)
                    ?? throw new DomainException("El libro no posee un inventario registrado.");
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

                var auditoria = new Auditoria(
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

            return multaGenerada
                ?? throw new InvalidOperationException("No fue posible registrar la incidencia.");
        }
    }
}
