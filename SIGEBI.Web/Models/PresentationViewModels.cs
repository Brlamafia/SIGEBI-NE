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
    public int NotificacionesSinLeer { get; init; }
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
    public bool HayPaginaSiguiente { get; init; }
    public IReadOnlySet<int> LibrosConSolicitudPendiente { get; init; } =
        new HashSet<int>();
    public required IReadOnlyCollection<string> GenerosDisponibles { get; init; }
    public required IReadOnlyCollection<string> EditorialesDisponibles { get; init; }
    public string? RestriccionSolicitud { get; init; }
}

public sealed class CatalogoDetalleViewModel
{
    public required LibroDto Libro { get; init; }
    public bool SolicitudPendiente { get; init; }
    public string? RestriccionSolicitud { get; init; }
}

public sealed class SolicitarLibroViewModel
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int LibroId { get; set; }
    public bool VolverAlDetalle { get; set; }
}

public sealed class CancelarSolicitudViewModel
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int Id { get; set; }
}

public sealed class MarcarNotificacionViewModel
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int Pagina { get; set; } = 1;
}

public sealed class NotificacionesViewModel
{
    public required IReadOnlyCollection<NotificacionDto> Notificaciones { get; init; }
    public int Pagina { get; init; } = 1;
    public bool HayPaginaSiguiente { get; init; }
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

public sealed class EmptyStateViewModel
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string? ActionText { get; init; }
    public string? ActionController { get; init; }
    public string? ActionName { get; init; }
}

public sealed class ApiErrorViewModel
{
    public int StatusCode { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string? RequestId { get; init; }
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
    [System.ComponentModel.DataAnnotations.RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "La contraseña debe contener mayúscula, minúscula y número.")]
    [System.ComponentModel.DataAnnotations.DataType(
        System.ComponentModel.DataAnnotations.DataType.Password)]
    [System.ComponentModel.DataAnnotations.Display(Name = "Nueva contraseña")]
    public string PasswordNueva { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.DataType(
        System.ComponentModel.DataAnnotations.DataType.Password)]
    [System.ComponentModel.DataAnnotations.Compare(
        nameof(PasswordNueva),
        ErrorMessage = "Las contraseñas no coinciden.")]
    [System.ComponentModel.DataAnnotations.Display(Name = "Confirmar contraseña")]
    public string ConfirmarPasswordNueva { get; set; } = string.Empty;
}
