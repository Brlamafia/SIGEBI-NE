using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Domain.Exceptions
{
    // Excepción específica: impide prestar a un usuario inactivo o restringido.
    public sealed class UsuarioNoHabilitadoException : DomainException
    {
        public UsuarioNoHabilitadoException()
            : base("El usuario no está habilitado para solicitar préstamos.")
        {
        }

        public UsuarioNoHabilitadoException(int usuarioId)
            : base($"El usuario con identificador {usuarioId} no está habilitado para solicitar préstamos.")
        {
        }
    }
}
