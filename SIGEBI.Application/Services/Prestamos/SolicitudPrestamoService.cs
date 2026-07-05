using AutoMapper;
using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Interfaces.SolicitudesPrestamo;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIGEBI.Application.Services.Prestamos
{
    public class SolicitudPrestamoService : BaseService<SolicitudPrestamo, SolicitudPrestamoDto>, ISolicitudPrestamoService
    {
        private readonly ISolicitudPrestamoRepository _solicitudRepository;

        // 1. Se agregó IMapper y se pasó a la base
        public SolicitudPrestamoService(ISolicitudPrestamoRepository solicitudRepository, IMapper mapper)
            : base(solicitudRepository, mapper)
        {
            _solicitudRepository = solicitudRepository;
        }

        public async Task<IEnumerable<SolicitudPrestamoDto>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            var lista = await _solicitudRepository.ObtenerPorUsuarioAsync(usuarioId);

            // 2. Mapeo real de las entidades a DTOs
            return _mapper.Map<IEnumerable<SolicitudPrestamoDto>>(lista);
        }

        public async Task<IEnumerable<SolicitudPrestamoDto>> ObtenerPorEstadoAsync(string estado)
        {
            // 3. Conversión del string recibido al Enum correspondiente ignorando mayúsculas/minúsculas (true)
            var estadoEnum = Enum.Parse<SIGEBI.Domain.Enums.EstadoSolicitud>(estado, true);

            var lista = await _solicitudRepository.ObtenerPorEstadoAsync(estadoEnum);

            // Mapeo real
            return _mapper.Map<IEnumerable<SolicitudPrestamoDto>>(lista);
        }

        public async Task<bool> RegistrarSolicitudAsync(SaveSolicitudPrestamoDto dto)
        {
            // Aquí irá la validación lógica (ej. si el usuario tiene multas pendientes)
            return true;
        }

        public async Task<bool> EvaluarSolicitudAsync(UpdateSolicitudPrestamoDto dto)
        {
            // Lógica para aprobar o rechazar la solicitud
            return true;
        }
    }
}