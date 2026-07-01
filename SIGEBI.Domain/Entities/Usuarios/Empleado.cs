// B.R
using SIGEBI.Domain.Base;

namespace SIGEBI.Domain.Entities.Usuarios
{
    // Extensión del usuario para funciones específicas del personal bibliotecario.
    public class Empleado : EntidadAuditable
    {
        public int UsuarioId { get; private set; }
        public string Cargo { get; private set; } = string.Empty;

        private Empleado() { }

        public Empleado(int usuarioId, string cargo)
        {
            if (usuarioId <= 0) throw new ArgumentOutOfRangeException(nameof(usuarioId));
            UsuarioId = usuarioId;
            Cargo = cargo?.Trim() ?? string.Empty;
        }
    }
}