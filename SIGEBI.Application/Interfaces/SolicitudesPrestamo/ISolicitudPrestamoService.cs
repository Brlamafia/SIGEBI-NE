using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIGEBI.Application.Interfaces.SolicitudesPrestamo
{
    public interface ISolicitudPrestamoService : IBaseService<SolicitudPrestamoDto>
    {
        Task<IEnumerable<SolicitudPrestamoDto>> ObtenerPorUsuarioAsync(int usuarioId);
        Task<IEnumerable<SolicitudPrestamoDto>> ObtenerPorEstadoAsync(string estado);
        Task<bool> RegistrarSolicitudAsync(SaveSolicitudPrestamoDto dto);
        Task<bool> EvaluarSolicitudAsync(UpdateSolicitudPrestamoDto dto);
    }
}