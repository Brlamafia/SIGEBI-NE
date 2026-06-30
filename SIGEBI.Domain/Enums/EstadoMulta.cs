using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Domain.Enums
{
    // Estado explícito: evita transiciones hacia valores no reconocidos por el dominio.
    // Los valores numéricos se fijan para mantener estabilidad en la base de datos.
    public enum EstadoMulta
    {
        Pendiente = 1,
        Pagada = 2,
        Resuelta = 3
    }
}
