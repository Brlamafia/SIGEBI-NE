using System;
using System.Collections.Generic;
using System.Text;

namespace SIGEBI.Domain.Services
{
    // Servicio de dominio: coordina reglas que involucran préstamo, usuario e inventario.
    public sealed class PrestamoDomainService
    {
        public SIGEBI.Domain.Entities.Prestamos.Prestamo RegistrarPrestamo(
            int usuarioId,
            bool usuarioHabilitado,
            bool tieneMultasPendientes,
            bool tienePrestamosVencidos,
            int cantidadPrestamosActivos,
            int limitePrestamos,
            int libroId,
            int solicitudPrestamoId,
            int empleadoPrestamoId,
            DateTime fechaPrestamo,
            DateTime fechaEsperadaDevolucion,
            SIGEBI.Domain.Entities.Catalogo.Inventario inventario)
        {
            ArgumentNullException.ThrowIfNull(inventario);

            // Fail Fast: todas las restricciones se validan antes de modificar el inventario.
            ValidarElegibilidad(
                usuarioId,
                usuarioHabilitado,
                tieneMultasPendientes,
                tienePrestamosVencidos,
                cantidadPrestamosActivos,
                limitePrestamos);

            ValidarInventarioDelLibro(inventario, libroId);

            var prestamo = new SIGEBI.Domain.Entities.Prestamos.Prestamo(
                usuarioId,
                libroId,
                solicitudPrestamoId,
                empleadoPrestamoId,
                fechaPrestamo,
                fechaEsperadaDevolucion);

            inventario.RegistrarPrestamo();
            return prestamo;
        }

        public bool RegistrarDevolucion(
            SIGEBI.Domain.Entities.Prestamos.Prestamo prestamo,
            SIGEBI.Domain.Entities.Catalogo.Inventario inventario,
            int empleadoDevolucionId,
            DateTime fechaRealDevolucion)
        {
            ArgumentNullException.ThrowIfNull(prestamo);
            ArgumentNullException.ThrowIfNull(inventario);

            ValidarInventarioDelLibro(inventario, prestamo.LibroId);

            if (inventario.CantidadPrestada <= 0)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "El inventario no registra ejemplares prestados para este libro.");
            }

            // Consistencia: préstamo e inventario cambian juntos dentro de la misma operación.
            var fueTardia = prestamo.RegistrarDevolucion(empleadoDevolucionId, fechaRealDevolucion);
            inventario.RegistrarDevolucion();

            return fueTardia;
        }

        private static void ValidarElegibilidad(
            int usuarioId,
            bool usuarioHabilitado,
            bool tieneMultasPendientes,
            bool tienePrestamosVencidos,
            int cantidadPrestamosActivos,
            int limitePrestamos)
        {
            if (usuarioId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(usuarioId), "El identificador debe ser mayor que cero.");
            }

            if (cantidadPrestamosActivos < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cantidadPrestamosActivos),
                    "La cantidad de préstamos activos no puede ser negativa.");
            }

            if (limitePrestamos <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limitePrestamos), "El límite debe ser mayor que cero.");
            }

            if (!usuarioHabilitado)
            {
                throw new SIGEBI.Domain.Exceptions.UsuarioNoHabilitadoException(usuarioId);
            }

            if (tieneMultasPendientes)
            {
                throw new SIGEBI.Domain.Exceptions.MultaPendienteException(usuarioId);
            }

            if (tienePrestamosVencidos)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    $"El usuario con identificador {usuarioId} tiene préstamos vencidos.");
            }

            if (cantidadPrestamosActivos >= limitePrestamos)
            {
                throw new SIGEBI.Domain.Exceptions.LimitePrestamosException(usuarioId, limitePrestamos);
            }
        }

        // DRY: centraliza la comprobación de pertenencia entre libro e inventario.
        private static void ValidarInventarioDelLibro(
            SIGEBI.Domain.Entities.Catalogo.Inventario inventario,
            int libroId)
        {
            if (inventario.LibroId != libroId)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "El inventario proporcionado no corresponde al libro del préstamo.");
            }
        }
    }
}
