// B.R
using SIGEBI.Domain.Base;

namespace SIGEBI.Domain.Entities.Usuarios
{
    // Define las capacidades o acciones específicas permitidas en el sistema.
    public class Permiso : EntidadAuditable
    {
        public string Nombre { get; private set; } = string.Empty;
        public string Codigo { get; private set; } = string.Empty;

        private Permiso() { }

        public Permiso(string nombre, string codigo)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre del permiso es obligatorio.", nameof(nombre));
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("El código del permiso es obligatorio.", nameof(codigo));

            Nombre = nombre.Trim();
            Codigo = codigo.Trim();
        }
    }
}
