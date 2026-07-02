using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Domain.Entities.Prestamos
{
    // Responsabilidad única: administra el ciclo de vida de un préstamo bibliotecario.
    public class Prestamo : SIGEBI.Domain.Base.EntidadAuditable
    {
        // Encapsulación: el estado solo cambia mediante operaciones válidas del dominio.
        public int UsuarioId { get; private set; }
        public int LibroId { get; private set; }
        public int SolicitudPrestamoId { get; private set; }
        public int EmpleadoPrestamoId { get; private set; }
        public int? EmpleadoDevolucionId { get; private set; }
        public DateTime FechaPrestamo { get; private set; }
        public DateTime FechaEsperadaDevolucion { get; private set; }
        public DateTime? FechaRealDevolucion { get; private set; }
        public SIGEBI.Domain.Enums.EstadoPrestamo Estado { get; private set; }

        private Prestamo()
        {
        }

        public Prestamo(
            int usuarioId,
            int libroId,
            int solicitudPrestamoId,
            int empleadoPrestamoId,
            DateTime fechaPrestamo,
            DateTime fechaEsperadaDevolucion)
        {
            // Fail Fast: evita que el préstamo nazca con relaciones o fechas inválidas.
            ValidarIdentificador(usuarioId, nameof(usuarioId));
            ValidarIdentificador(libroId, nameof(libroId));
            ValidarIdentificador(solicitudPrestamoId, nameof(solicitudPrestamoId));
            ValidarIdentificador(empleadoPrestamoId, nameof(empleadoPrestamoId));

            if (fechaEsperadaDevolucion <= fechaPrestamo)
            {
                throw new ArgumentException(
                    "La fecha esperada de devolución debe ser posterior a la fecha del préstamo.",
                    nameof(fechaEsperadaDevolucion));
            }

            UsuarioId = usuarioId;
            LibroId = libroId;
            SolicitudPrestamoId = solicitudPrestamoId;
            EmpleadoPrestamoId = empleadoPrestamoId;
            FechaPrestamo = fechaPrestamo;
            FechaEsperadaDevolucion = fechaEsperadaDevolucion;
            Estado = SIGEBI.Domain.Enums.EstadoPrestamo.Activo;
        }

        // Entidad con comportamiento: la devolución forma parte del ciclo de vida del préstamo.
        public bool RegistrarDevolucion(int empleadoDevolucionId, DateTime fechaRealDevolucion)
        {
            ValidarIdentificador(empleadoDevolucionId, nameof(empleadoDevolucionId));

            if (Estado is not SIGEBI.Domain.Enums.EstadoPrestamo.Activo
                and not SIGEBI.Domain.Enums.EstadoPrestamo.Vencido)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "Solo puede devolverse un préstamo activo o vencido.");
            }

            if (fechaRealDevolucion < FechaPrestamo)
            {
                throw new ArgumentException(
                    "La fecha real de devolución no puede ser anterior a la fecha del préstamo.",
                    nameof(fechaRealDevolucion));
            }

            EmpleadoDevolucionId = empleadoDevolucionId;
            FechaRealDevolucion = fechaRealDevolucion;
            Estado = SIGEBI.Domain.Enums.EstadoPrestamo.Devuelto;
            MarcarComoModificada();

            return fechaRealDevolucion > FechaEsperadaDevolucion;
        }

        public void MarcarComoVencido(DateTime fechaReferencia)
        {
            if (Estado != SIGEBI.Domain.Enums.EstadoPrestamo.Activo)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "Solo un préstamo activo puede marcarse como vencido.");
            }

            if (fechaReferencia <= FechaEsperadaDevolucion)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "El préstamo todavía se encuentra dentro del plazo de devolución.");
            }

            Estado = SIGEBI.Domain.Enums.EstadoPrestamo.Vencido;
            MarcarComoModificada();
        }

        public void Cancelar()
        {
            if (Estado != SIGEBI.Domain.Enums.EstadoPrestamo.Activo)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "Solo un préstamo activo puede cancelarse.");
            }

            Estado = SIGEBI.Domain.Enums.EstadoPrestamo.Cancelado;
            MarcarComoModificada();
        }

        public void RegistrarPerdida(int empleadoResponsableId, DateTime fechaReporte)
        {
            CerrarPrestamo(empleadoResponsableId, fechaReporte, SIGEBI.Domain.Enums.EstadoPrestamo.Perdido);
        }

        public void RegistrarDevolucionConDanio(int empleadoResponsableId, DateTime fechaDevolucion)
        {
            CerrarPrestamo(
                empleadoResponsableId,
                fechaDevolucion,
                SIGEBI.Domain.Enums.EstadoPrestamo.DevueltoConDanio);
        }

        private void CerrarPrestamo(
            int empleadoResponsableId,
            DateTime fechaCierre,
            SIGEBI.Domain.Enums.EstadoPrestamo estadoFinal)
        {
            ValidarIdentificador(empleadoResponsableId, nameof(empleadoResponsableId));

            if (Estado is not SIGEBI.Domain.Enums.EstadoPrestamo.Activo
                and not SIGEBI.Domain.Enums.EstadoPrestamo.Vencido)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "Solo puede cerrarse un préstamo activo o vencido.");
            }

            if (fechaCierre < FechaPrestamo)
            {
                throw new ArgumentException(
                    "La fecha de cierre no puede ser anterior a la fecha del préstamo.",
                    nameof(fechaCierre));
            }

            EmpleadoDevolucionId = empleadoResponsableId;
            FechaRealDevolucion = fechaCierre;
            Estado = estadoFinal;
            MarcarComoModificada();
        }

        // DRY: centraliza una validación utilizada por el constructor y la devolución.
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
