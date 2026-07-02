using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Domain.Services
{
    // Servicio de dominio: contiene cálculos y reglas que requieren préstamo y multa.
    public sealed class MultaDomainService
    {
        public bool TieneMultasPendientes(IEnumerable<SIGEBI.Domain.Entities.Prestamos.Multa> multas)
        {
            ArgumentNullException.ThrowIfNull(multas);
            return multas.Any(multa => multa.EstaPendiente);
        }

        public SIGEBI.Domain.Entities.Prestamos.Multa GenerarMultaPorRetraso(
            SIGEBI.Domain.Entities.Prestamos.Prestamo prestamo,
            decimal montoPorDia,
            IEnumerable<SIGEBI.Domain.Entities.Prestamos.Multa> multasExistentes)
        {
            ArgumentNullException.ThrowIfNull(prestamo);
            ArgumentNullException.ThrowIfNull(multasExistentes);

            // Fail Fast: el cálculo solo es válido para un préstamo persistido y devuelto.
            if (prestamo.Id <= 0)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "El préstamo debe estar registrado antes de generar una multa.");
            }

            if (montoPorDia <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(montoPorDia), "El monto por día debe ser mayor que cero.");
            }

            if (multasExistentes.Any(multa =>
                multa.PrestamoId == prestamo.Id
                && multa.Tipo == SIGEBI.Domain.Enums.TipoMulta.Retraso))
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "El préstamo ya posee una multa por retraso.");
            }

            if (prestamo.Estado != SIGEBI.Domain.Enums.EstadoPrestamo.Devuelto
                || !prestamo.FechaRealDevolucion.HasValue)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "La multa por retraso solo puede generarse después de registrar la devolución.");
            }

            var diasRetraso = (prestamo.FechaRealDevolucion.Value.Date
                - prestamo.FechaEsperadaDevolucion.Date).Days;

            if (diasRetraso <= 0)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "El préstamo fue devuelto dentro del plazo y no genera multa por retraso.");
            }

            // Regla de cálculo: monto total igual a días de retraso por tarifa diaria.
            var montoTotal = diasRetraso * montoPorDia;
            var motivo = $"Devolución realizada con {diasRetraso} día(s) de retraso.";

            return new SIGEBI.Domain.Entities.Prestamos.Multa(
                prestamo.UsuarioId,
                prestamo.Id,
                SIGEBI.Domain.Enums.TipoMulta.Retraso,
                montoTotal,
                motivo);
        }

        public SIGEBI.Domain.Entities.Prestamos.Multa GenerarMultaPorPerdida(
            SIGEBI.Domain.Entities.Prestamos.Prestamo prestamo,
            decimal monto,
            string motivo,
            IEnumerable<SIGEBI.Domain.Entities.Prestamos.Multa> multasExistentes)
            => GenerarMultaPorIncidencia(
                prestamo,
                monto,
                motivo,
                SIGEBI.Domain.Enums.TipoMulta.Perdida,
                SIGEBI.Domain.Enums.EstadoPrestamo.Perdido,
                multasExistentes);

        public SIGEBI.Domain.Entities.Prestamos.Multa GenerarMultaPorDanio(
            SIGEBI.Domain.Entities.Prestamos.Prestamo prestamo,
            decimal monto,
            string motivo,
            IEnumerable<SIGEBI.Domain.Entities.Prestamos.Multa> multasExistentes)
            => GenerarMultaPorIncidencia(
                prestamo,
                monto,
                motivo,
                SIGEBI.Domain.Enums.TipoMulta.Danio,
                SIGEBI.Domain.Enums.EstadoPrestamo.DevueltoConDanio,
                multasExistentes);

        private static SIGEBI.Domain.Entities.Prestamos.Multa GenerarMultaPorIncidencia(
            SIGEBI.Domain.Entities.Prestamos.Prestamo prestamo,
            decimal monto,
            string motivo,
            SIGEBI.Domain.Enums.TipoMulta tipo,
            SIGEBI.Domain.Enums.EstadoPrestamo estadoRequerido,
            IEnumerable<SIGEBI.Domain.Entities.Prestamos.Multa> multasExistentes)
        {
            ArgumentNullException.ThrowIfNull(prestamo);
            ArgumentNullException.ThrowIfNull(multasExistentes);

            if (prestamo.Id <= 0)
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "El préstamo debe estar registrado antes de generar una multa.");
            if (prestamo.Estado != estadoRequerido)
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "El estado del préstamo no corresponde al tipo de multa solicitado.");
            if (multasExistentes.Any(multa => multa.PrestamoId == prestamo.Id && multa.Tipo == tipo))
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "El préstamo ya posee una multa de este tipo.");

            return new SIGEBI.Domain.Entities.Prestamos.Multa(
                prestamo.UsuarioId,
                prestamo.Id,
                tipo,
                monto,
                motivo);
        }
    }
}
