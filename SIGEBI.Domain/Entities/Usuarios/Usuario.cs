using System;
using SIGEBI.Domain.Base;
using SIGEBI.Domain.Enums;
// B.R
namespace SIGEBI.Domain.Entities.Usuarios
{
    // Administra la información personal y el estado de acceso de un cliente.
    public class Usuario : EntidadAuditable
    {
        private readonly List<Rol> _roles = [];

        public string Nombre { get; private set; } = string.Empty;
        public string Apellido { get; private set; } = string.Empty;
        public string Cedula { get; private set; } = string.Empty;
        public string Telefono { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public TipoUsuario TipoUsuario { get; private set; }
        public EstadoUsuario Estado { get; private set; }
        public IReadOnlyCollection<Rol> Roles => _roles;

        private Usuario() { }

        public Usuario(string nombre, string apellido, string cedula, string email, TipoUsuario tipoUsuario)
        {
            Nombre = ValidarTextoObligatorio(nombre, nameof(nombre));
            Apellido = ValidarTextoObligatorio(apellido, nameof(apellido));
            Cedula = ValidarTextoObligatorio(cedula, nameof(cedula));
            Email = ValidarTextoObligatorio(email, nameof(email));
            TipoUsuario = tipoUsuario;
            Estado = EstadoUsuario.Activo;
        }

        public void ActualizarContacto(string telefono, string email)
        {
            Telefono = telefono?.Trim() ?? string.Empty;
            Email = ValidarTextoObligatorio(email, nameof(email));
            MarcarComoModificada();
        }

        public void CambiarEstado(EstadoUsuario nuevoEstado)
        {
            Estado = nuevoEstado;
            MarcarComoModificada();
        }

        public void AsignarRol(Rol rol)
        {
            ArgumentNullException.ThrowIfNull(rol);

            if (!_roles.Contains(rol))
            {
                _roles.Add(rol);
                MarcarComoModificada();
            }
        }

        public void RemoverRol(Rol rol)
        {
            ArgumentNullException.ThrowIfNull(rol);

            if (_roles.Remove(rol))
                MarcarComoModificada();
        }

        private static string ValidarTextoObligatorio(string valor, string nombreParametro)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("Este campo es obligatorio.", nombreParametro);
            return valor.Trim();
        }
    }
}
