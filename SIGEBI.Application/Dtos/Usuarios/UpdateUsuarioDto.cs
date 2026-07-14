using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Application.Dtos.Usuarios
{
    public class UpdateUsuarioDto
    {
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
