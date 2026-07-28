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
        public string ContrasenaHash { get; private set; } = string.Empty;
        public TipoUsuario TipoUsuario { get; private set; }
        public EstadoUsuario Estado { get; private set; }
        public int IntentosAccesoFallidos { get; private set; }
        public DateTime? BloqueadoHasta { get; private set; }
        public IReadOnlyCollection<Rol> Roles => _roles;
        public bool EstaBloqueado(DateTime fechaUtc) =>
            BloqueadoHasta.HasValue && BloqueadoHasta.Value > fechaUtc;

        private Usuario() { }

        public Usuario(
            string nombre,
            string apellido,
            string cedula,
            string email,
            TipoUsuario tipoUsuario,
            string telefono = "")
        {
            Nombre = ValidarTextoObligatorio(nombre, nameof(nombre));
            Apellido = ValidarTextoObligatorio(apellido, nameof(apellido));
            Cedula = ValidarTextoObligatorio(cedula, nameof(cedula));
            Email = ValidarTextoObligatorio(email, nameof(email));
            Telefono = telefono?.Trim() ?? string.Empty;
            TipoUsuario = tipoUsuario;
            Estado = EstadoUsuario.Activo;
        }

        public void ActualizarDatos(
            string nombre,
            string apellido,
            string cedula,
            string telefono,
            string email,
            TipoUsuario tipoUsuario,
            EstadoUsuario estado)
        {
            Nombre = ValidarTextoObligatorio(nombre, nameof(nombre));
            Apellido = ValidarTextoObligatorio(apellido, nameof(apellido));
            Cedula = ValidarTextoObligatorio(cedula, nameof(cedula));
            Telefono = telefono?.Trim() ?? string.Empty;
            Email = ValidarTextoObligatorio(email, nameof(email));
            TipoUsuario = Enum.IsDefined(tipoUsuario)
                ? tipoUsuario
                : throw new ArgumentOutOfRangeException(nameof(tipoUsuario));
            Estado = Enum.IsDefined(estado)
                ? estado
                : throw new ArgumentOutOfRangeException(nameof(estado));
            MarcarComoModificada();
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

        public void EstablecerContrasenaHash(string contrasenaHash)
        {
            if (string.IsNullOrWhiteSpace(contrasenaHash))
                throw new ArgumentException("La contraseña cifrada es obligatoria.", nameof(contrasenaHash));
            ContrasenaHash = contrasenaHash;
            MarcarComoModificada();
        }

        public void RegistrarIntentoFallido(int maximoIntentos, TimeSpan duracionBloqueo)
        {
            if (maximoIntentos <= 0 || duracionBloqueo <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(maximoIntentos));

            IntentosAccesoFallidos++;
            if (IntentosAccesoFallidos >= maximoIntentos)
                BloqueadoHasta = DateTime.UtcNow.Add(duracionBloqueo);
            MarcarComoModificada();
        }

        public void RegistrarAccesoExitoso()
        {
            IntentosAccesoFallidos = 0;
            BloqueadoHasta = null;
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
