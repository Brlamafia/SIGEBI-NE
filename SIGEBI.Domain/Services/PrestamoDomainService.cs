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
            SIGEBI.Domain.Entities.Prestamos.SolicitudPrestamo solicitud,
            int empleadoPrestamoId,
            DateTime fechaPrestamo,
            DateTime fechaEsperadaDevolucion,
            SIGEBI.Domain.Entities.Catalogo.Inventario inventario,
            SIGEBI.Domain.Entities.Catalogo.Ejemplar ejemplar)
        {
            ArgumentNullException.ThrowIfNull(solicitud);
            ArgumentNullException.ThrowIfNull(inventario);
            ArgumentNullException.ThrowIfNull(ejemplar);

            // Fail Fast: todas las restricciones se validan antes de modificar el inventario.
            ValidarElegibilidad(
                usuarioId,
                usuarioHabilitado,
                tieneMultasPendientes,
                tienePrestamosVencidos,
                cantidadPrestamosActivos,
                limitePrestamos);

            ValidarSolicitudAprobada(solicitud, usuarioId);
            ValidarInventarioDelLibro(inventario, solicitud.LibroId);
            ValidarEjemplarDelLibro(ejemplar, solicitud.LibroId);

            var prestamo = new SIGEBI.Domain.Entities.Prestamos.Prestamo(
                usuarioId,
                solicitud.LibroId,
                ejemplar.Id,
                solicitud.Id,
                empleadoPrestamoId,
                fechaPrestamo,
                fechaEsperadaDevolucion);

            inventario.RegistrarPrestamo();
            ejemplar.Prestar();
            return prestamo;
        }

        private static void ValidarSolicitudAprobada(
            SIGEBI.Domain.Entities.Prestamos.SolicitudPrestamo solicitud,
            int usuarioId)
        {
            if (solicitud.Id <= 0)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "La solicitud debe estar registrada antes de crear el préstamo.");
            }

            if (solicitud.Estado != SIGEBI.Domain.Enums.EstadoSolicitud.Aprobada)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "Solo una solicitud aprobada puede convertirse en préstamo.");
            }

            if (solicitud.UsuarioId != usuarioId)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "La solicitud no corresponde al usuario del préstamo.");
            }
        }

        public bool RegistrarDevolucion(
            SIGEBI.Domain.Entities.Prestamos.Prestamo prestamo,
            SIGEBI.Domain.Entities.Catalogo.Inventario inventario,
            SIGEBI.Domain.Entities.Catalogo.Ejemplar ejemplar,
            int empleadoDevolucionId,
            DateTime fechaRealDevolucion)
        {
            ArgumentNullException.ThrowIfNull(prestamo);
            ArgumentNullException.ThrowIfNull(inventario);
            ArgumentNullException.ThrowIfNull(ejemplar);

            ValidarInventarioDelLibro(inventario, prestamo.LibroId);
            ValidarEjemplarDelPrestamo(ejemplar, prestamo);

            if (inventario.CantidadPrestada <= 0)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "El inventario no registra ejemplares prestados para este libro.");
            }

            // Consistencia: préstamo e inventario cambian juntos dentro de la misma operación.
            var fueTardia = prestamo.RegistrarDevolucion(empleadoDevolucionId, fechaRealDevolucion);
            inventario.RegistrarDevolucion();
            ejemplar.Devolver();

            return fueTardia;
        }

        public void CancelarPrestamo(
            SIGEBI.Domain.Entities.Prestamos.Prestamo prestamo,
            SIGEBI.Domain.Entities.Catalogo.Inventario inventario,
            SIGEBI.Domain.Entities.Catalogo.Ejemplar ejemplar)
        {
            ArgumentNullException.ThrowIfNull(prestamo);
            ArgumentNullException.ThrowIfNull(inventario);
            ArgumentNullException.ThrowIfNull(ejemplar);
            ValidarInventarioDelLibro(inventario, prestamo.LibroId);
            ValidarEjemplarDelPrestamo(ejemplar, prestamo);
            ValidarExistenciaPrestada(inventario);

            prestamo.Cancelar();
            inventario.CancelarPrestamo();
            ejemplar.Devolver();
        }

        public void RegistrarPerdida(
            SIGEBI.Domain.Entities.Prestamos.Prestamo prestamo,
            SIGEBI.Domain.Entities.Catalogo.Inventario inventario,
            SIGEBI.Domain.Entities.Catalogo.Ejemplar ejemplar,
            int empleadoResponsableId,
            DateTime fechaReporte)
        {
            ArgumentNullException.ThrowIfNull(prestamo);
            ArgumentNullException.ThrowIfNull(inventario);
            ArgumentNullException.ThrowIfNull(ejemplar);
            ValidarInventarioDelLibro(inventario, prestamo.LibroId);
            ValidarEjemplarDelPrestamo(ejemplar, prestamo);
            ValidarExistenciaPrestada(inventario);

            prestamo.RegistrarPerdida(empleadoResponsableId, fechaReporte);
            inventario.RegistrarPerdida();
            ejemplar.RegistrarPerdida();
        }

        public void RegistrarDevolucionConDanio(
            SIGEBI.Domain.Entities.Prestamos.Prestamo prestamo,
            SIGEBI.Domain.Entities.Catalogo.Inventario inventario,
            SIGEBI.Domain.Entities.Catalogo.Ejemplar ejemplar,
            int empleadoResponsableId,
            DateTime fechaDevolucion)
        {
            ArgumentNullException.ThrowIfNull(prestamo);
            ArgumentNullException.ThrowIfNull(inventario);
            ArgumentNullException.ThrowIfNull(ejemplar);
            ValidarInventarioDelLibro(inventario, prestamo.LibroId);
            ValidarEjemplarDelPrestamo(ejemplar, prestamo);
            ValidarExistenciaPrestada(inventario);

            prestamo.RegistrarDevolucionConDanio(empleadoResponsableId, fechaDevolucion);
            inventario.RegistrarDanio();
            ejemplar.RegistrarDanio();
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

        private static void ValidarExistenciaPrestada(
            SIGEBI.Domain.Entities.Catalogo.Inventario inventario)
        {
            if (inventario.CantidadPrestada <= 0)
            {
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "El inventario no registra ejemplares prestados para este libro.");
            }
        }

        private static void ValidarEjemplarDelLibro(
            SIGEBI.Domain.Entities.Catalogo.Ejemplar ejemplar,
            int libroId)
        {
            if (ejemplar.LibroId != libroId)
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "El ejemplar proporcionado no corresponde al libro del préstamo.");
        }

        private static void ValidarEjemplarDelPrestamo(
            SIGEBI.Domain.Entities.Catalogo.Ejemplar ejemplar,
            SIGEBI.Domain.Entities.Prestamos.Prestamo prestamo)
        {
            ValidarEjemplarDelLibro(ejemplar, prestamo.LibroId);
            if (ejemplar.Id != prestamo.EjemplarId)
                throw new SIGEBI.Domain.Exceptions.DomainException(
                    "El ejemplar no corresponde al préstamo indicado.");
        }
    }
}
