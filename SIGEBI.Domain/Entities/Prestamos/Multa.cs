using System;
using System.Collections.Generic;
using System.Text;

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
            ValidarIdentificador(usuarioId, nameof(usuarioId));

            if (prestamoId.HasValue)
            {
                ValidarIdentificador(prestamoId.Value, nameof(prestamoId));
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

            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw new ArgumentException("El motivo de la multa es obligatorio.", nameof(motivo));
            }

            UsuarioId = usuarioId;
            PrestamoId = prestamoId;
            Tipo = tipo;
            Monto = monto;
            Motivo = motivo.Trim();
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
            ValidarIdentificador(empleadoResolucionId, nameof(empleadoResolucionId));

            if (fechaResolucion < FechaGeneracion)
            {
                throw new ArgumentException(
                    "La fecha de resolución no puede ser anterior a la generación de la multa.",
                    nameof(fechaResolucion));
            }

            if (string.IsNullOrWhiteSpace(observacion))
            {
                throw new ArgumentException("La observación de resolución es obligatoria.", nameof(observacion));
            }

            EmpleadoResolucionId = empleadoResolucionId;
            FechaResolucion = fechaResolucion;
            ObservacionResolucion = observacion.Trim();
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

        private static void ValidarIdentificador(int identificador, string nombreParametro)
        {
            if (identificador <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nombreParametro,
                    "El identificador debe ser mayor que cero.");
            }
        }
    }
}
