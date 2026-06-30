using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Domain.Base
{
    // Herencia: agrega información de auditoría a las entidades que la necesitan.
    public abstract class EntidadAuditable : EntidadBase
    {
        public DateTime FechaCreacion { get; protected set; } = DateTime.UtcNow;
        public DateTime? FechaModificacion { get; protected set; }

        // DRY: centraliza la actualización de la fecha para evitar repetir esta lógica.
        protected void MarcarComoModificada()
        {
            FechaModificacion = DateTime.UtcNow;
        }
    }
}
