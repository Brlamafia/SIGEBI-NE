using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Domain.Enums
{
    // Lenguaje del dominio: representa únicamente los motivos definidos por la arquitectura.
    // Los valores numéricos se fijan para mantener estabilidad en la base de datos.
    public enum TipoMulta
    {
        Retraso = 1,
        Perdida = 2,
        Danio = 3,
        Otra = 4
    }
}
