using System;
using System.Collections.Generic;
using System.Text;
using SIGEBI.Domain.Common;

namespace SIGEBI.Domain.Entities.Prestamos
{
    // Responsabilidad única: administra el ciclo de vida de una penalización.
    public class Multa : SIGEBI.Domain.Base.EntidadAuditable
    {
        // Encapsulación: los datos de cierre solo pueden cambiar mediante operaciones válidas.
        public int UsuarioId { get; private set; }
        public int? PrestamoId { get; private set; }
        public SIGEBI.Domain.Enums.TipoMulta Tipo { get; private set; }
        public decimal Monto { get; private set; }
        public string Motivo { get; private set; } = string.Empty;
        public SIGEBI.Domain.Enums.EstadoMulta Estado { get; private set; }
        public DateTime FechaGeneracion { get; private set; }
        public int? EmpleadoResolucionId { get; private set; }
        public DateTime? FechaResolucion { get; private set; }
        public string? ObservacionResolucion { get; private set; }

        public bool EstaPendiente => Estado == SIGEBI.Domain.Enums.EstadoMulta.Pendiente;

        private Multa()
        {
        }

        public Multa(
            int usuarioId,
            int? prestamoId,
            SIGEBI.Domain.Enums.TipoMulta tipo,
            decimal monto,
            string motivo)
        {
            // Fail Fast: una multa nunca debe crearse con datos inválidos.
            Guard.AgainstNonPositive(usuarioId, nameof(usuarioId));

            if (!Enum.IsDefined(tipo))
            {
                throw new ArgumentOutOfRangeException(nameof(tipo), "El tipo de multa no es válido.");
            }

            if (prestamoId.HasValue)
            {
                Guard.AgainstNonPositive(prestamoId.Value, nameof(prestamoId));
            }

            if (tipo == SIGEBI.Domain.Enums.TipoMulta.Retraso && !prestamoId.HasValue)
            {
                throw new ArgumentException(
                    "Una multa por retraso debe estar asociada a un préstamo.",
                    nameof(prestamoId));
            }

            if (monto <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(monto), "El monto debe ser mayor que cero.");
            }

            UsuarioId = usuarioId;
            PrestamoId = prestamoId;
            Tipo = tipo;
            Monto = monto;
            Motivo = Guard.AgainstNullOrWhiteSpace(motivo, nameof(motivo));
            Estado = SIGEBI.Domain.Enums.EstadoMulta.Pendiente;
            FechaGeneracion = DateTime.UtcNow;
        }

        // Entidad con comportamiento: evita alterar directamente el estado desde otras capas.
        public void MarcarComoPagada()
        {
            ValidarPendiente();

            Estado = SIGEBI.Domain.Enums.EstadoMulta.Pagada;
            MarcarComoModificada();
        }

        public void Resolver(
            int empleadoResolucionId,
            DateTime fechaResolucion,
            string observacion)
        {
            ValidarPendiente();
            Guard.AgainstNonPositive(empleadoResolucionId, nameof(empleadoResolucionId));

            if (fechaResolucion < FechaGeneracion)
            {
                throw new ArgumentException(
                    "La fecha de resolución no puede ser anterior a la generación de la multa.",
                    nameof(fechaResolucion));
            }

            EmpleadoResolucionId = empleadoResolucionId;
            FechaResolucion = fechaResolucion;
            ObservacionResolucion = Guard.AgainstNullOrWhiteSpace(observacion, nameof(observacion));
            Estado = SIGEBI.Domain.Enums.EstadoMulta.Resuelta;
            MarcarComoModificada();
        }

        // DRY: concentra las validaciones reutilizadas por las operaciones de la entidad.
        private void ValidarPendiente()
        {
            if (!EstaPendiente)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "Solo una multa pendiente puede pagarse o resolverse.");
            }
        }

    }
}
