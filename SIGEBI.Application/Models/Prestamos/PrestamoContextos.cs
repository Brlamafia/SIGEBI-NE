using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;

namespace SIGEBI.Application.Models.Prestamos;

public sealed record PrestamoRegistroContexto(
    SolicitudPrestamo Solicitud,
    Usuario Usuario,
    Empleado Empleado,
    Inventario Inventario,
    Ejemplar Ejemplar,
    bool TieneMultasPendientes,
    bool TienePrestamosVencidos,
    int PrestamosActivos);

public sealed record PrestamoOperacionContexto(
    Prestamo Prestamo,
    Empleado Empleado,
    Inventario Inventario,
    Ejemplar Ejemplar);
