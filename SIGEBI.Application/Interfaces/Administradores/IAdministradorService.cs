using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Administradores;

namespace SIGEBI.Application.Interfaces.Administradores
{
    public interface IAdministradorService
    {
        Task<IReadOnlyCollection<AdministradorDto>> ObtenerTodosAsync(CancellationToken cancellationToken = default);
        Task<AdministradorDto> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<AdministradorDto> CrearAsync(SaveAdministradorDto dto, CancellationToken cancellationToken = default);
        Task<AdministradorDto> ActualizarAsync(int id, UpdateAdministradorDto dto, CancellationToken cancellationToken = default);
    }
}
