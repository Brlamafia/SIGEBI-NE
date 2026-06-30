using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Domain.Exceptions
{
    // Excepción específica: aplica la restricción de préstamos por multas pendientes.
    public sealed class MultaPendienteException : DomainException
    {
        public MultaPendienteException()
            : base("El usuario tiene multas pendientes.")
        {
        }

        public MultaPendienteException(int usuarioId)
            : base($"El usuario con identificador {usuarioId} tiene multas pendientes.")
        {
        }
    }
}
