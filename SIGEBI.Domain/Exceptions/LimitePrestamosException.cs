using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Domain.Exceptions
{
    // Excepción específica: aplica el límite configurable de préstamos activos por usuario.
    public sealed class LimitePrestamosException : DomainException
    {
        public LimitePrestamosException()
            : base("El usuario alcanzó el límite de préstamos activos permitido.")
        {
        }

        public LimitePrestamosException(int usuarioId, int limitePermitido)
            : base($"El usuario con identificador {usuarioId} alcanzó el límite de {limitePermitido} préstamos activos.")
        {
        }
    }
}
