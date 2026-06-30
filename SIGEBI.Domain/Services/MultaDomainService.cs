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
            decimal montoPorDia)
        {
            ArgumentNullException.ThrowIfNull(prestamo);

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
    }
}
