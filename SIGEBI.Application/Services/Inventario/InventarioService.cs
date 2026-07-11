using System.Data;
using AutoMapper;
using SIGEBI.Application.Dtos.Inventario;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Inventario;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;
using AuditoriaEntidad = SIGEBI.Domain.Entities.Auditoria.Auditoria;
using InventarioEntidad = SIGEBI.Domain.Entities.Catalogo.Inventario;

namespace SIGEBI.Application.Services.Inventario
{
    // Capa de Aplicación: coordina consultas y ajustes de inventario mediante DTOs.
    public class InventarioService : IInventarioService
    {
        private readonly IInventarioRepository _inventarios;
        private readonly IAuditoriaRepository _auditorias;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public InventarioService(
            IInventarioRepository inventarios,
            IAuditoriaRepository auditorias,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _inventarios = inventarios;
            _auditorias = auditorias;
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

                inventarioCreado = new InventarioEntidad(dto.LibroId, dto.CantidadTotal);
                await _inventarios.AgregarAsync(inventarioCreado, ct);
                await _auditorias.AgregarAsync(new AuditoriaEntidad(
                    dto.UsuarioResponsableId,
                    ModuloAuditoria.Inventario,
                    AccionAuditoria.Registrar,
                    $"Inventario inicial del libro {dto.LibroId}: {dto.CantidadTotal}. Motivo: {dto.Motivo.Trim()}",
                    ResultadoAuditoria.Exitoso), ct);
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

                inventario.AjustarCantidadTotal(dto.NuevaCantidadTotal);
                inventarioActualizado = inventario;

                var auditoria = new AuditoriaEntidad(
                    dto.UsuarioResponsableId,
                    ModuloAuditoria.Inventario,
                    AccionAuditoria.Ajustar,
                    $"Ajuste de inventario {inventario.Id} a cantidad total {dto.NuevaCantidadTotal}. Motivo: {dto.Motivo.Trim()}",
                    ResultadoAuditoria.Exitoso);

                _inventarios.Actualizar(inventario);
                await _auditorias.AgregarAsync(auditoria, ct);
            }, IsolationLevel.Serializable, cancellationToken);

            return _mapper.Map<InventarioDto>(
                inventarioActualizado ?? throw new InvalidOperationException("No fue posible ajustar el inventario."));
        }

        private static void ValidarResponsableYMotivo(int usuarioResponsableId, string motivo)
        {
            if (usuarioResponsableId <= 0)
                throw new BusinessRuleException("Debe indicar el usuario responsable.");
            if (string.IsNullOrWhiteSpace(motivo))
                throw new BusinessRuleException("Debe indicar el motivo del movimiento de inventario.");
        }
    }
}
