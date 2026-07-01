// B.R
using System;
using SIGEBI.Domain.Base;

namespace SIGEBI.Domain.Entities.Usuarios
{
    // B.R: Entidad que representa al Administrador del sistema. 
    // Hereda de EntidadAuditable, por lo que el Id ya está incluido.
    public class Administrador : EntidadAuditable
    {
        public string Nombre { get; private set; } = string.Empty;
        public string Apellido { get; private set; } = string.Empty;
        public string Correo { get; private set; } = string.Empty;

        // Relación con Cargo
        public int CargoId { get; private set; }
        public Cargo? Cargo { get; private set; }

        private Administrador() { }

        public Administrador(string nombre, string apellido, string correo, int cargoId)
        {
            Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre.Trim() : throw new ArgumentException("El nombre es obligatorio.");
            Apellido = !string.IsNullOrWhiteSpace(apellido) ? apellido.Trim() : throw new ArgumentException("El apellido es obligatorio.");
            Correo = !string.IsNullOrWhiteSpace(correo) ? correo.Trim() : throw new ArgumentException("El correo es obligatorio.");
            CargoId = cargoId;
        }
    }
}