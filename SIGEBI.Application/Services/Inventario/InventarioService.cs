using System.Data;
using AutoMapper;
using SIGEBI.Application.Common;
using SIGEBI.Application.Dtos.Inventario;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Inventario;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using EjemplarEntidad = SIGEBI.Domain.Entities.Catalogo.Ejemplar;
using InventarioEntidad = SIGEBI.Domain.Entities.Catalogo.Inventario;

namespace SIGEBI.Application.Services.Inventario
{
    // Capa de Aplicación: coordina consultas y ajustes de inventario mediante DTOs.
    public class InventarioService : IInventarioService
    {
        private readonly IInventarioRepository _inventarios;
        private readonly IEjemplarRepository _ejemplares;
        private readonly ILibroRepository _libros;
        private readonly IAuditoriaWriter _auditoria;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public InventarioService(
            IInventarioRepository inventarios,
            IEjemplarRepository ejemplares,
            ILibroRepository libros,
            IAuditoriaWriter auditoria,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _inventarios = inventarios;
            _ejemplares = ejemplares;
            _libros = libros;
            _auditoria = auditoria;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<InventarioDto> ObtenerPorIdAsync(
            int inventarioId,
            CancellationToken cancellationToken = default)
        {
            var inventario = await _inventarios.ObtenerPorIdAsync(inventarioId, cancellationToken)
                ?? throw new NotFoundException(nameof(InventarioEntidad), inventarioId);

            return _mapper.Map<InventarioDto>(inventario);
        }

        public async Task<IReadOnlyCollection<InventarioDto>> ObtenerTodosAsync(
            CancellationToken cancellationToken = default)
            => _mapper.Map<IReadOnlyCollection<InventarioDto>>(
                await _inventarios.ObtenerTodosAsync(cancellationToken));

        public async Task<InventarioDto> CrearAsync(
            CrearInventarioDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ValidarResponsableYMotivo(dto.UsuarioResponsableId, dto.Motivo);
            InventarioEntidad? inventarioCreado = null;

            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                if (await _inventarios.ObtenerPorLibroIdAsync(dto.LibroId, ct) is not null)
                    throw new BusinessRuleException("El libro ya posee un inventario registrado.");
                if (await _libros.ObtenerPorIdAsync(dto.LibroId, ct) is null)
                    throw new NotFoundException("Libro", dto.LibroId);

                inventarioCreado = new InventarioEntidad(dto.LibroId, dto.CantidadTotal);
                await _inventarios.AgregarAsync(inventarioCreado, ct);
                await _ejemplares.AgregarRangoAsync(
                    CrearEjemplares(dto.LibroId, dto.CantidadTotal), ct);
                await _auditoria.RegistrarAsync(
                    dto.UsuarioResponsableId,
                    ModuloAuditoria.Inventario,
                    AccionAuditoria.Registrar,
                    $"Inventario inicial del libro {dto.LibroId}: {dto.CantidadTotal}. Motivo: {dto.Motivo.Trim()}",
                    cancellationToken: ct);
            }, IsolationLevel.Serializable, cancellationToken);

            return _mapper.Map<InventarioDto>(inventarioCreado!);
        }

        public async Task<InventarioDto> ObtenerPorLibroIdAsync(
            int libroId,
            CancellationToken cancellationToken = default)
        {
            var inventario = await _inventarios.ObtenerPorLibroIdAsync(libroId, cancellationToken)
                ?? throw new NotFoundException(nameof(InventarioEntidad), libroId);

            return _mapper.Map<InventarioDto>(inventario);
        }

        public async Task<InventarioDto> AjustarCantidadTotalAsync(
            AjustarInventarioDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ValidarResponsableYMotivo(dto.UsuarioResponsableId, dto.Motivo);
            InventarioEntidad? inventarioActualizado = null;

            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var inventario = await _inventarios.ObtenerPorIdAsync(dto.InventarioId, ct)
                    ?? throw new NotFoundException(nameof(InventarioEntidad), dto.InventarioId);

                var diferencia = dto.NuevaCantidadTotal - inventario.CantidadTotal;

                inventario.AjustarCantidadTotal(dto.NuevaCantidadTotal);
                inventarioActualizado = inventario;

                if (diferencia > 0)
                {
                    await _ejemplares.AgregarRangoAsync(
                        CrearEjemplares(inventario.LibroId, diferencia), ct);
                }
                else if (diferencia < 0)
                {
                    var disponibles = await _ejemplares.ObtenerPorLibroYEstadoAsync(
                        inventario.LibroId, EstadoEjemplar.Disponible, ct);
                    _ejemplares.EliminarRango(disponibles.Take(-diferencia));
                }

                _inventarios.Actualizar(inventario);
                await _auditoria.RegistrarAsync(
                    dto.UsuarioResponsableId,
                    ModuloAuditoria.Inventario,
                    AccionAuditoria.Ajustar,
                    $"Ajuste de inventario {inventario.Id} a cantidad total {dto.NuevaCantidadTotal}. Motivo: {dto.Motivo.Trim()}",
                    cancellationToken: ct);
            }, IsolationLevel.Serializable, cancellationToken);

            return _mapper.Map<InventarioDto>(
                inventarioActualizado ?? throw new InvalidOperationException("No fue posible ajustar el inventario."));
        }

        public async Task<IReadOnlyCollection<EjemplarDto>> ObtenerEjemplaresPorLibroAsync(
            int libroId,
            CancellationToken cancellationToken = default)
        {
            var ejemplares = await _ejemplares.ObtenerPorLibroAsync(libroId, cancellationToken);
            return _mapper.Map<IReadOnlyCollection<EjemplarDto>>(ejemplares);
        }

        public async Task<EjemplarDto> CambiarEstadoEjemplarAsync(
            CambiarEstadoEjemplarDto dto,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ValidarResponsableYMotivo(dto.UsuarioResponsableId, dto.Motivo);
            EjemplarEntidad? actualizado = null;

            await _unitOfWork.EjecutarEnTransaccionAsync(async ct =>
            {
                var ejemplar = await _ejemplares.ObtenerPorIdAsync(dto.EjemplarId, ct)
                    ?? throw new NotFoundException(nameof(EjemplarEntidad), dto.EjemplarId);
                var inventario = await _inventarios.ObtenerPorLibroIdAsync(ejemplar.LibroId, ct)
                    ?? throw new NotFoundException(nameof(InventarioEntidad), ejemplar.LibroId);
                var nuevoEstado = EnumParser.ParseDefined<EstadoEjemplar>(dto.NuevoEstado, "estado del ejemplar");
                var estadoAnterior = ejemplar.Estado;

                ejemplar.CambiarEstadoOperativo(nuevoEstado);
                inventario.CambiarEstadoEjemplar(estadoAnterior, nuevoEstado);
                actualizado = ejemplar;

                _ejemplares.Actualizar(ejemplar);
                _inventarios.Actualizar(inventario);
                await _auditoria.RegistrarAsync(
                    dto.UsuarioResponsableId,
                    ModuloAuditoria.Inventario,
                    AccionAuditoria.ActualizarEstado,
                    $"Ejemplar {ejemplar.Codigo}: {estadoAnterior} -> {nuevoEstado}. Motivo: {dto.Motivo.Trim()}",
                    cancellationToken: ct);
            }, IsolationLevel.Serializable, cancellationToken);

            return _mapper.Map<EjemplarDto>(actualizado!);
        }

        private static IEnumerable<EjemplarEntidad> CrearEjemplares(int libroId, int cantidad)
            => Enumerable.Range(0, cantidad)
                .Select(_ => new EjemplarEntidad(libroId, $"LIB-{libroId}-{Guid.NewGuid():N}"));

        private static void ValidarResponsableYMotivo(int usuarioResponsableId, string motivo)
        {
            if (usuarioResponsableId <= 0)
                throw new BusinessRuleException("Debe indicar el usuario responsable.");
            if (string.IsNullOrWhiteSpace(motivo))
                throw new BusinessRuleException("Debe indicar el motivo del movimiento de inventario.");
        }
    }
}
