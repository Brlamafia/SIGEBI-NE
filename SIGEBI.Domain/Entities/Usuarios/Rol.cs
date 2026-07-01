// B.R
using SIGEBI.Domain.Base;

namespace SIGEBI.Domain.Entities.Usuarios
{
    // Representa el rol de seguridad dentro del sistema.
    public class Rol : EntidadAuditable
    {
        public string Nombre { get; private set; } = string.Empty;
        public string Descripcion { get; private set; } = string.Empty;

        private Rol() { }

        public Rol(string nombre, string descripcion)
        {
            Nombre = nombre?.Trim() ?? string.Empty;
            Descripcion = descripcion?.Trim() ?? string.Empty;
        }
    }
}