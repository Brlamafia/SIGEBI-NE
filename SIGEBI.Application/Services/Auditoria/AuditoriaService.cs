using AutoMapper;
using SIGEBI.Application.Common;
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

        public async Task<IReadOnlyCollection<AuditoriaDto>> ObtenerTodasAsync(
            CancellationToken cancellationToken = default)
            => _mapper.Map<IReadOnlyCollection<AuditoriaDto>>(
                await _auditorias.ObtenerTodasAsync(cancellationToken));

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
            var auditorias = await _auditorias.ObtenerPorRangoAsync(
                DateTimeNormalizer.ToUtc(fechaDesde),
                DateTimeNormalizer.ToUtc(fechaHasta),
                cancellationToken);
            return _mapper.Map<IReadOnlyCollection<AuditoriaDto>>(auditorias);
        }

        public async Task<IReadOnlyCollection<AuditoriaDto>> FiltrarAsync(
            FiltroAuditoriaDto filtro,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filtro);
            if (filtro.Pagina <= 0 || filtro.TamanoPagina is <= 0 or > 200)
                throw new BusinessRuleException("La paginación indicada no es válida.");
            if (filtro.FechaDesde.HasValue != filtro.FechaHasta.HasValue)
                throw new BusinessRuleException("Debe indicar la fecha inicial y la fecha final.");

            ModuloAuditoria? modulo = string.IsNullOrWhiteSpace(filtro.Modulo)
                ? null
                : ConvertirModulo(filtro.Modulo);
            DateTime? desde = filtro.FechaDesde.HasValue
                ? DateTimeNormalizer.ToUtc(filtro.FechaDesde.Value)
                : null;
            DateTime? hasta = filtro.FechaHasta.HasValue
                ? DateTimeNormalizer.ToUtc(filtro.FechaHasta.Value)
                : null;
            var resultados = await _auditorias.FiltrarPaginaAsync(
                (filtro.Pagina - 1) * filtro.TamanoPagina,
                filtro.TamanoPagina,
                filtro.UsuarioResponsableId,
                modulo,
                desde,
                hasta,
                cancellationToken);
            return _mapper.Map<IReadOnlyCollection<AuditoriaDto>>(resultados);
        }

        private static ModuloAuditoria ConvertirModulo(string modulo)
        {
            return EnumParser.ParseDefined<ModuloAuditoria>(modulo, "módulo de auditoría");
        }
    }
}
