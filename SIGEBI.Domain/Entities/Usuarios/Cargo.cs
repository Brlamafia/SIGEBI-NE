using SIGEBI.Domain.Base;

namespace SIGEBI.Domain.Entities.Usuarios
{
    // Encapsulación: el nombre del cargo solo cambia mediante operaciones válidas del dominio.
    public class Cargo : EntidadAuditable
    {
        public string Nombre { get; private set; } = string.Empty;

        private Cargo() { }

        public Cargo(string nombre)
        {
            Nombre = ValidarNombre(nombre);
        }

        public void Renombrar(string nombre)
        {
            Nombre = ValidarNombre(nombre);
            MarcarComoModificada();
        }

        private static string ValidarNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre del cargo es obligatorio.", nameof(nombre));

            return nombre.Trim();
        }
    }
}
