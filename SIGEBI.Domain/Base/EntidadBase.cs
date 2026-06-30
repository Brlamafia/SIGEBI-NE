using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Domain.Base
{
    // Abstracción: concentra la identidad común de todas las entidades del dominio.
    public abstract class EntidadBase
    {
        // Encapsulación: solo la entidad y sus clases derivadas pueden asignar el identificador.
        public int Id { get; protected set; }
    }
}
