using AutoMapper;
using SIGEBI.Application.Dtos.Auditoria;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces.Repositories;
using AuditoriaEntidad = SIGEBI.Domain.Entities.Auditoria.Auditoria;

namespace SIGEBI.Application.Services.Auditoria
{
    // Capa de Aplicación: habilita consultas de auditoría sin modificar el registro histórico.
    public class AuditoriaService : IAuditoriaService
    {
        private readonly IAuditoriaRepository _auditorias;
        private readonly IMapper _mapper;

        public AuditoriaService(
            IAuditoriaRepository auditorias,
            IMapper mapper)
        {
            _auditorias = auditorias;
            _mapper = mapper;
        }

        public async Task<AuditoriaDto> ObtenerPorIdAsync(
            int auditoriaId,
            CancellationToken cancellationToken = default)
        {
            var auditoria = await _auditorias.ObtenerPorIdAsync(auditoriaId, cancellationToken)
                ?? throw new NotFoundException(nameof(AuditoriaEntidad), auditoriaId);

            return _mapper.Map<AuditoriaDto>(auditoria);
        }

        public async Task<IReadOnlyCollection<AuditoriaDto>> ObtenerPorUsuarioAsync(
            int usuarioResponsableId,
            CancellationToken cancellationToken = default)
        {
            var auditorias = await _auditorias.ObtenerPorUsuarioAsync(usuarioResponsableId, cancellationToken);
            return _mapper.Map<IReadOnlyCollection<AuditoriaDto>>(auditorias);
        }

        public async Task<IReadOnlyCollection<AuditoriaDto>> ObtenerPorModuloAsync(
            string modulo,
            CancellationToken cancellationToken = default)
        {
            var moduloAuditoria = ConvertirModulo(modulo);
            var auditorias = await _auditorias.ObtenerPorModuloAsync(moduloAuditoria, cancellationToken);
            return _mapper.Map<IReadOnlyCollection<AuditoriaDto>>(auditorias);
        }

        public async Task<IReadOnlyCollection<AuditoriaDto>> ObtenerPorRangoAsync(
            DateTime fechaDesde,
            DateTime fechaHasta,
            CancellationToken cancellationToken = default)
        {
            var auditorias = await _auditorias.ObtenerPorRangoAsync(fechaDesde, fechaHasta, cancellationToken);
            return _mapper.Map<IReadOnlyCollection<AuditoriaDto>>(auditorias);
        }

        public async Task<IReadOnlyCollection<AuditoriaDto>> FiltrarAsync(
            FiltroAuditoriaDto filtro,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filtro);

            if (filtro.FechaDesde.HasValue || filtro.FechaHasta.HasValue)
            {
                if (!filtro.FechaDesde.HasValue || !filtro.FechaHasta.HasValue)
                    throw new BusinessRuleException("Debe indicar la fecha inicial y la fecha final.");

                return await ObtenerPorRangoAsync(
                    filtro.FechaDesde.Value,
                    filtro.FechaHasta.Value,
                    cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(filtro.Modulo))
                return await ObtenerPorModuloAsync(filtro.Modulo, cancellationToken);

            if (filtro.UsuarioResponsableId.HasValue)
                return await ObtenerPorUsuarioAsync(filtro.UsuarioResponsableId.Value, cancellationToken);

            throw new BusinessRuleException("Debe indicar al menos un filtro de auditoría.");
        }

        private static ModuloAuditoria ConvertirModulo(string modulo)
        {
            if (!Enum.TryParse<ModuloAuditoria>(modulo, ignoreCase: true, out var moduloAuditoria)
                || !Enum.IsDefined(moduloAuditoria))
            {
                throw new BusinessRuleException("El módulo de auditoría no es válido.");
            }

            return moduloAuditoria;
        }
    }
}
