using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Domain.Entities.Auditoria
{
    // Inmutabilidad: representa un hecho histórico que no puede alterarse después de crearse.
    public class Auditoria : SIGEBI.Domain.Base.EntidadBase
    {
        // Encapsulación: ninguna capa externa puede modificar los datos registrados.
        public int UsuarioResponsableId { get; private set; }
        public string Modulo { get; private set; } = string.Empty;
        public string Accion { get; private set; } = string.Empty;
        public string Descripcion { get; private set; } = string.Empty;
        public string Resultado { get; private set; } = string.Empty;
        public DateTime Fecha { get; private set; }

        private Auditoria()
        {
        }

        public Auditoria(
            int usuarioResponsableId,
            string modulo,
            string accion,
            string descripcion,
            string resultado)
        {
            // Fail Fast: evita almacenar registros incompletos o sin responsable.
            if (usuarioResponsableId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(usuarioResponsableId),
                    "El identificador del responsable debe ser mayor que cero.");
            }

            UsuarioResponsableId = usuarioResponsableId;
            Modulo = ValidarTextoObligatorio(modulo, nameof(modulo));
            Accion = ValidarTextoObligatorio(accion, nameof(accion));
            Descripcion = ValidarTextoObligatorio(descripcion, nameof(descripcion));
            Resultado = ValidarTextoObligatorio(resultado, nameof(resultado));
            Fecha = DateTime.UtcNow;
        }

        // DRY: centraliza la validación común de todos los campos descriptivos.
        private static string ValidarTextoObligatorio(string valor, string nombreParametro)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new ArgumentException("El valor es obligatorio.", nombreParametro);
            }

            return valor.Trim();
        }
    }
}
