using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Usuarios;

namespace SIGEBI.Application.Interfaces.Usuarios
{
    public interface IUsuarioService : IBaseService<UsuarioDto>
    {
        Task<UsuarioDto> CrearAsync(
            SaveUsuarioDto dto,
            CancellationToken cancellationToken = default);
        Task<UsuarioDto> ActualizarAsync(
            int usuarioId,
            UpdateUsuarioDto dto,
            CancellationToken cancellationToken = default);
        Task EliminarAsync(
            int usuarioId,
            CancellationToken cancellationToken = default);
        Task<object> ConsultarHistorialCompletoAsync(int usuarioId);
    }
}
