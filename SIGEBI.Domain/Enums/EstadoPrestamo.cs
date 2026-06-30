using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Domain.Enums
{
    // Estado explícito: limita el ciclo de vida del préstamo a valores válidos del dominio.
    // Los valores numéricos se fijan para mantener estabilidad al persistirlos en la base de datos.
    public enum EstadoPrestamo
    {
        Activo = 1,
        Devuelto = 2,
        Vencido = 3,
        Cancelado = 4
    }
}
