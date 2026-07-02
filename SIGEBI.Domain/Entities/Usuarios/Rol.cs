// B.R
using SIGEBI.Domain.Base;

namespace SIGEBI.Domain.Entities.Usuarios
{
    // Representa el rol de seguridad dentro del sistema.
    public class Rol : EntidadAuditable
    {
        private readonly List<Permiso> _permisos = [];

        public string Nombre { get; private set; } = string.Empty;
        public string Descripcion { get; private set; } = string.Empty;
        public IReadOnlyCollection<Permiso> Permisos => _permisos;

        private Rol() { }

        public Rol(string nombre, string descripcion)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre del rol es obligatorio.", nameof(nombre));

            Nombre = nombre.Trim();
            Descripcion = descripcion?.Trim() ?? string.Empty;
        }

        public void AsignarPermiso(Permiso permiso)
        {
            ArgumentNullException.ThrowIfNull(permiso);

            if (!_permisos.Contains(permiso))
            {
                _permisos.Add(permiso);
                MarcarComoModificada();
            }
        }

        public void RemoverPermiso(Permiso permiso)
        {
            ArgumentNullException.ThrowIfNull(permiso);

            if (_permisos.Remove(permiso))
                MarcarComoModificada();
        }
    }
}
