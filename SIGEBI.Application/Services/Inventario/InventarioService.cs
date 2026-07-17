using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging;
using SIGEBI.Application.Common;
using SIGEBI.Application.Dtos.Inventario;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Inventario;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EjemplarEntidad = SIGEBI.Domain.Entities.Catalogo.Ejemplar;
using InventarioEntidad = SIGEBI.Domain.Entities.Catalogo.Inventario;

namespace SIGEBI.Application.Services.Inventario
{
    public class InventarioService : IInventarioService
    {
        private readonly IInventarioRepository _inventarios;
        private readonly IEjemplarRepository _ejemplares;
        private readonly ILibroRepository _libros;
        private readonly IAuditoriaWriter _auditoria;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<InventarioService> _logger;

        public InventarioService(
            IInventarioRepository inventarios,
            IEjemplarRepository ejemplares,
            ILibroRepository libros,
            IAuditoriaWriter auditoria,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<InventarioService> logger)
        {
            _inventarios = inventarios;
            _ejemplares = ejemplares;
            _libros = libros;
            _auditoria = auditoria;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<InventarioDto> ObtenerPorIdAsync(int inventarioId, CancellationToken ct = default)
        {
            var inv = await _inventarios.ObtenerPorIdAsync(inventarioId, ct) ?? throw new NotFoundException(nameof(InventarioEntidad), inventarioId);
            return _mapper.Map<InventarioDto>(inv);
        }

        public async Task<IReadOnlyCollection<InventarioDto>> ObtenerTodosAsync(CancellationToken ct = default)
            => _mapper.Map<IReadOnlyCollection<InventarioDto>>(await _inventarios.ObtenerTodosAsync(ct));

        public async Task<InventarioDto> CrearAsync(CrearInventarioDto dto, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ValidarResponsableYMotivo(dto.UsuarioResponsableId, dto.Motivo);
            try
            {
                InventarioEntidad? inventarioCreado = null;
                await _unitOfWork.EjecutarEnTransaccionAsync(async c => {
                    if (await _inventarios.ObtenerPorLibroIdAsync(dto.LibroId, c) is not null) throw new BusinessRuleException("El libro ya posee un inventario.");
                    if (await _libros.ObtenerPorIdAsync(dto.LibroId, c) is null) throw new NotFoundException("Libro", dto.LibroId);

                    inventarioCreado = new InventarioEntidad(dto.LibroId, dto.CantidadTotal);
                    await _inventarios.AgregarAsync(inventarioCreado, c);
                    await _ejemplares.AgregarRangoAsync(CrearEjemplares(dto.LibroId, dto.CantidadTotal), c);
                    await _auditoria.RegistrarAsync(dto.UsuarioResponsableId, ModuloAuditoria.Inventario, AccionAuditoria.Registrar, $"Inventario inicial libro {dto.LibroId}", cancellationToken: c);
                }, IsolationLevel.Serializable, ct);
                return _mapper.Map<InventarioDto>(inventarioCreado!);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error al crear inventario para libro {Id}", dto.LibroId); throw; }
        }

        public async Task<InventarioDto> ObtenerPorLibroIdAsync(int libroId, CancellationToken ct = default)
        {
            var inv = await _inventarios.ObtenerPorLibroIdAsync(libroId, ct) ?? throw new NotFoundException(nameof(InventarioEntidad), libroId);
            return _mapper.Map<InventarioDto>(inv);
        }

        public async Task<InventarioDto> AjustarCantidadTotalAsync(AjustarInventarioDto dto, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ValidarResponsableYMotivo(dto.UsuarioResponsableId, dto.Motivo);
            try
            {
                InventarioEntidad? invActualizado = null;
                await _unitOfWork.EjecutarEnTransaccionAsync(async c => {
                    var inv = await _inventarios.ObtenerPorIdAsync(dto.InventarioId, c) ?? throw new NotFoundException(nameof(InventarioEntidad), dto.InventarioId);
                    var dif = dto.NuevaCantidadTotal - inv.CantidadTotal;
                    inv.AjustarCantidadTotal(dto.NuevaCantidadTotal);
                    invActualizado = inv;
                    if (dif > 0) await _ejemplares.AgregarRangoAsync(CrearEjemplares(inv.LibroId, dif), c);
                    else if (dif < 0) _ejemplares.EliminarRango((await _ejemplares.ObtenerPorLibroYEstadoAsync(inv.LibroId, EstadoEjemplar.Disponible, c)).Take(-dif));
                    _inventarios.Actualizar(inv);
                    await _auditoria.RegistrarAsync(dto.UsuarioResponsableId, ModuloAuditoria.Inventario, AccionAuditoria.Ajustar, $"Ajuste inventario {inv.Id}", cancellationToken: c);
                }, IsolationLevel.Serializable, ct);
                return _mapper.Map<InventarioDto>(invActualizado!);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error ajustando inventario {Id}", dto.InventarioId); throw; }
        }

        public async Task<EjemplarDto> CambiarEstadoEjemplarAsync(CambiarEstadoEjemplarDto dto, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ValidarResponsableYMotivo(dto.UsuarioResponsableId, dto.Motivo);
            try
            {
                EjemplarEntidad? actualizado = null;
                await _unitOfWork.EjecutarEnTransaccionAsync(async c => {
                    var e = await _ejemplares.ObtenerPorIdAsync(dto.EjemplarId, c) ?? throw new NotFoundException(nameof(EjemplarEntidad), dto.EjemplarId);
                    var i = await _inventarios.ObtenerPorLibroIdAsync(e.LibroId, c) ?? throw new NotFoundException(nameof(InventarioEntidad), e.LibroId);
                    var nuevo = EnumParser.ParseDefined<EstadoEjemplar>(dto.NuevoEstado, "estado ejemplar");
                    e.CambiarEstadoOperativo(nuevo);
                    i.CambiarEstadoEjemplar(e.Estado, nuevo);
                    actualizado = e;
                    _ejemplares.Actualizar(e); _inventarios.Actualizar(i);
                    await _auditoria.RegistrarAsync(dto.UsuarioResponsableId, ModuloAuditoria.Inventario, AccionAuditoria.ActualizarEstado, $"Ejemplar {e.Codigo}", cancellationToken: c);
                }, IsolationLevel.Serializable, ct);
                return _mapper.Map<EjemplarDto>(actualizado!);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error al cambiar estado de ejemplar {Id}", dto.EjemplarId); throw; }
        }

        private static IEnumerable<EjemplarEntidad> CrearEjemplares(int libroId, int cantidad) => Enumerable.Range(0, cantidad).Select(_ => new EjemplarEntidad(libroId, $"LIB-{libroId}-{Guid.NewGuid():N}"));
        private static void ValidarResponsableYMotivo(int u, string m) { if (u <= 0 || string.IsNullOrWhiteSpace(m)) throw new BusinessRuleException("Responsable y motivo son obligatorios."); }
        public async Task<IReadOnlyCollection<EjemplarDto>> ObtenerEjemplaresPorLibroAsync(int libroId, CancellationToken ct = default) => _mapper.Map<IReadOnlyCollection<EjemplarDto>>(await _ejemplares.ObtenerPorLibroAsync(libroId, ct));
    }
}