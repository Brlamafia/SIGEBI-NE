using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Application.Dtos.Catalogo
{
    public class SaveLibroDto
    {
        public string Titulo { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public string Editorial { get; set; } = string.Empty;
    }
}
