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
            Nombre = nombre?.Trim() ?? string.Empty;
            Codigo = codigo?.Trim() ?? string.Empty;
        }
    }
}