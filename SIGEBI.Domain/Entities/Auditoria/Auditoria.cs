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
        public SIGEBI.Domain.Enums.ModuloAuditoria Modulo { get; private set; }
        public SIGEBI.Domain.Enums.AccionAuditoria Accion { get; private set; }
        public string Descripcion { get; private set; } = string.Empty;
        public SIGEBI.Domain.Enums.ResultadoAuditoria Resultado { get; private set; }
        public DateTime Fecha { get; private set; }

        private Auditoria()
        {
        }

        public Auditoria(
            int usuarioResponsableId,
            SIGEBI.Domain.Enums.ModuloAuditoria modulo,
            SIGEBI.Domain.Enums.AccionAuditoria accion,
            string descripcion,
            SIGEBI.Domain.Enums.ResultadoAuditoria resultado)
        {
            // Fail Fast: evita almacenar registros incompletos o sin responsable.
            if (usuarioResponsableId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(usuarioResponsableId),
                    "El identificador del responsable debe ser mayor que cero.");
            }

            UsuarioResponsableId = usuarioResponsableId;
            Modulo = ValidarEnum(modulo, nameof(modulo));
            Accion = ValidarEnum(accion, nameof(accion));
            Descripcion = ValidarTextoObligatorio(descripcion, nameof(descripcion));
            Resultado = ValidarEnum(resultado, nameof(resultado));
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

        private static TEnum ValidarEnum<TEnum>(TEnum valor, string nombreParametro)
            where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(valor))
                throw new ArgumentOutOfRangeException(nombreParametro, "El valor indicado no es válido.");

            return valor;
        }
    }
}
