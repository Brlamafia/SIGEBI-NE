using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Dtos.Usuarios;

namespace SIGEBI.Web.Models;

public sealed class DashboardViewModel
{
    public required UsuarioDto Usuario { get; init; }
    public required IReadOnlyCollection<PrestamoDto> Prestamos { get; init; }
    public decimal MontoMultasPendientes { get; init; }
    public required IReadOnlyCollection<NotificacionDto> Notificaciones { get; init; }
    public required IReadOnlyCollection<SolicitudPrestamoDto> Solicitudes { get; init; }
    public required IReadOnlyDictionary<int, string> TitulosLibros { get; init; }
}

public sealed class CatalogoViewModel
{
    public required IReadOnlyCollection<LibroDto> Libros { get; init; }
    public string? Termino { get; init; }
    public string? Genero { get; init; }
    public string? Editorial { get; init; }
    public bool? Disponible { get; init; }
    public int Pagina { get; init; } = 1;
    public IReadOnlySet<int> LibrosConSolicitudPendiente { get; init; } =
        new HashSet<int>();
    public required IReadOnlyCollection<string> GenerosDisponibles { get; init; }
    public required IReadOnlyCollection<string> EditorialesDisponibles { get; init; }
    public string? RestriccionSolicitud { get; init; }
}

public sealed class SolicitudesViewModel
{
    public required IReadOnlyCollection<SolicitudPrestamoDto> Solicitudes { get; init; }
    public required IReadOnlyDictionary<int, string> TitulosLibros { get; init; }
}

public sealed class PrestamosViewModel
{
    public required IReadOnlyCollection<PrestamoDto> Prestamos { get; init; }
    public required IReadOnlyDictionary<int, string> TitulosLibros { get; init; }
}

public sealed class MultasViewModel
{
    public required IReadOnlyCollection<MultaDto> Multas { get; init; }
    public required IReadOnlyDictionary<int, PrestamoDto> Prestamos { get; init; }
    public required IReadOnlyDictionary<int, string> TitulosLibros { get; init; }
}

public sealed class CuentaViewModel
{
    public UsuarioDto Usuario { get; set; } = new();
    public CambiarPasswordViewModel Password { get; set; } = new();
}

public sealed class CambiarPasswordViewModel
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.DataType(
        System.ComponentModel.DataAnnotations.DataType.Password)]
    [System.ComponentModel.DataAnnotations.Display(Name = "Contraseña actual")]
    public string PasswordActual { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MinLength(8)]
    [System.ComponentModel.DataAnnotations.DataType(
        System.ComponentModel.DataAnnotations.DataType.Password)]
    [System.ComponentModel.DataAnnotations.Display(Name = "Nueva contraseña")]
    public string PasswordNueva { get; set; } = string.Empty;
}
