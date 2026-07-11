// B.R
// Encapsulación: el administrador mantiene relaciones válidas con usuario y cargo.
using System;
using SIGEBI.Domain.Base;

namespace SIGEBI.Domain.Entities.Usuarios
{
    // B.R: Entidad que representa al Administrador del sistema. 
    // Hereda de EntidadAuditable, por lo que el Id ya está incluido.
    // Herencia y SRP: representa la extensión administrativa de un usuario del sistema.
    public class Administrador : EntidadAuditable
    {
        public int UsuarioId { get; private set; }
        public int CargoId { get; private set; }
        public Usuario? Usuario { get; private set; }
        public Cargo? Cargo { get; private set; }

        private Administrador() { }

        public Administrador(int usuarioId, int cargoId)
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
