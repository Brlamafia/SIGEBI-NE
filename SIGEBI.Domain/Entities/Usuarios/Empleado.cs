// B.R
// Herencia y SRP: representa la extensión laboral de un usuario del sistema.
using SIGEBI.Domain.Base;

namespace SIGEBI.Domain.Entities.Usuarios
{
    // Extensión del usuario para funciones específicas del personal bibliotecario.
    public class Empleado : EntidadAuditable
    {
        public int UsuarioId { get; private set; }
        public int CargoId { get; private set; }
        public Usuario? Usuario { get; private set; }
        public Cargo? Cargo { get; private set; }

        private Empleado() { }

        public Empleado(int usuarioId, int cargoId)
        {
            if (usuarioId <= 0) throw new ArgumentOutOfRangeException(nameof(usuarioId));
            if (cargoId <= 0) throw new ArgumentOutOfRangeException(nameof(cargoId));

            UsuarioId = usuarioId;
            CargoId = cargoId;
        }

        public void ActualizarCargo(int cargoId)
        {
            if (cargoId <= 0) throw new ArgumentOutOfRangeException(nameof(cargoId));
            CargoId = cargoId;
            MarcarComoModificada();
        }
    }
}
