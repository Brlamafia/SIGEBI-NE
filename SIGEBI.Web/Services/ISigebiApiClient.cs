using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Dtos.Usuarios;

namespace SIGEBI.Web.Services;

public interface ISigebiApiClient
{
    Task<ApiSession> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task RegisterAsync(SaveUsuarioDto request, CancellationToken cancellationToken = default);
    Task<ApiSession> ExternalLoginAsync(string email, CancellationToken cancellationToken = default);
    Task<ApiSession> ExternalRegisterAsync(SaveUsuarioDto request, CancellationToken cancellationToken = default);
    Task<string?> RequestPasswordResetAsync(string email, string resetUrlBase, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(string token, string password, CancellationToken cancellationToken = default);
    Task<UsuarioDto> GetMeAsync(CancellationToken cancellationToken = default);
    Task<MySummary> GetMySummaryAsync(CancellationToken cancellationToken = default);
    Task ChangeMyPasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LibroDto>> SearchBooksAsync(string? term, string? genre, string? publisher, bool? available, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LibroDto>> GetBooksAsync(int page = 1, int pageSize = 200, CancellationToken cancellationToken = default);
    Task<LibroDto> GetBookByIdAsync(int bookId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SolicitudPrestamoDto>> GetMyRequestsAsync(CancellationToken cancellationToken = default);
    Task CreateRequestAsync(int bookId, CancellationToken cancellationToken = default);
    Task CancelRequestAsync(int requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<NotificacionDto>> GetMyNotificationsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task MarkNotificationReadAsync(int notificationId, CancellationToken cancellationToken = default);
}

public sealed class ApiSession
{
    public string Token { get; init; } = string.Empty;
    public UsuarioDto Usuario { get; init; } = new();
    public IReadOnlyCollection<string> Roles { get; init; } = [];
    public IReadOnlyCollection<string> Permisos { get; init; } = [];
}

public sealed class MySummary
{
    public UsuarioDto Usuario { get; init; } = new();
    public IReadOnlyCollection<PrestamoDto> Prestamos { get; init; } = [];
    public IReadOnlyCollection<MultaDto> Multas { get; init; } = [];
    public IReadOnlyCollection<NotificacionDto> Notificaciones { get; init; } = [];
}
