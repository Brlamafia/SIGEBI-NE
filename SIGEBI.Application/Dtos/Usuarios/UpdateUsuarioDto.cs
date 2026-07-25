using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Application.Dtos.Usuarios
{
    public class UpdateUsuarioDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public SIGEBI.Domain.Enums.TipoUsuario TipoUsuario { get; set; }
        public SIGEBI.Domain.Enums.EstadoUsuario Estado { get; set; }
    }
}
