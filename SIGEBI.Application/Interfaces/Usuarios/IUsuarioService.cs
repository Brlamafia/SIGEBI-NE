using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Usuarios;

namespace SIGEBI.Application.Interfaces.Usuarios
{
    public interface IUsuarioService : IBaseService<UsuarioDto>
    {
        Task<object> ConsultarHistorialCompletoAsync(int usuarioId);
    }
}
