using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Roles;
using System.Threading.Tasks;

namespace SIGEBI.Application.Interfaces.Roles
{
    public interface IRolService : IBaseService<RolDto>
    {
        Task AsignarAUsuarioAsync(AsignarRolDto dto, CancellationToken ct = default);
        Task RemoverDeUsuarioAsync(AsignarRolDto dto, CancellationToken ct = default);
        Task<PermisoDto> CrearPermisoAsync(SavePermisoDto dto, CancellationToken ct = default);
        Task AsignarPermisoAsync(AsignarPermisoDto dto, CancellationToken ct = default);
        Task RemoverPermisoAsync(AsignarPermisoDto dto, CancellationToken ct = default);
    }
}
