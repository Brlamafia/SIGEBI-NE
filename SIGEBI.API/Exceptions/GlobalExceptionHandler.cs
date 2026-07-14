using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SIGEBI.Application.Exceptions;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Domain.Exceptions;
using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Interfaces;
using System.Security.Claims;

namespace SIGEBI.API.Exceptions;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger,
    IServiceScopeFactory scopeFactory) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            ConflictoConcurrenciaException => (StatusCodes.Status409Conflict, "Conflicto de concurrencia"),
            BusinessRuleException or DomainException =>
                (StatusCodes.Status409Conflict, "Regla de negocio incumplida"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Error no controlado al procesar {Method} {Path}.",
                httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning(exception, "Solicitud rechazada al procesar {Method} {Path}.",
                httpContext.Request.Method, httpContext.Request.Path);

        await RegistrarAuditoriaFallidaAsync(httpContext, exception, cancellationToken);

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = statusCode == StatusCodes.Status500InternalServerError
                    ? "Ocurrió un error inesperado."
                    : exception.Message,
                Instance = httpContext.Request.Path
            },
            Exception = exception
        });
    }

    private async Task RegistrarAuditoriaFallidaAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(
                httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var usuarioResponsableId)
            || usuarioResponsableId <= 0
            || !TryObtenerModulo(httpContext.Request.Path, out var modulo))
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var auditoria = scope.ServiceProvider.GetRequiredService<IAuditoriaWriter>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await auditoria.RegistrarAsync(
                usuarioResponsableId,
                modulo,
                ObtenerAccion(httpContext.Request),
                $"Operación rechazada en {httpContext.Request.Method} {httpContext.Request.Path}: {exception.Message}",
                ResultadoAuditoria.Fallido,
                cancellationToken);
            await unitOfWork.GuardarCambiosAsync(cancellationToken);
        }
        catch (Exception auditException)
        {
            logger.LogError(auditException, "No fue posible registrar la auditoría fallida.");
        }
    }

    private static bool TryObtenerModulo(PathString path, out ModuloAuditoria modulo)
    {
        var segmento = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault();
        modulo = segmento?.ToLowerInvariant() switch
        {
            "prestamos" or "devoluciones" => ModuloAuditoria.Prestamos,
            "multas" => ModuloAuditoria.Multas,
            "inventario" => ModuloAuditoria.Inventario,
            "auditoria" => ModuloAuditoria.Auditoria,
            _ => default
        };
        return modulo != default;
    }

    private static AccionAuditoria ObtenerAccion(HttpRequest request)
    {
        var path = request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (path.Contains("rechazar")) return AccionAuditoria.Rechazar;
        if (path.Contains("cancelar")) return AccionAuditoria.Cancelar;
        if (path.Contains("devoluciones")) return AccionAuditoria.Devolver;
        if (path.Contains("pagar")) return AccionAuditoria.Pagar;
        if (path.Contains("resolver")) return AccionAuditoria.Resolver;
        if (path.Contains("ajustar")) return AccionAuditoria.Ajustar;
        if (path.Contains("perdidas")) return AccionAuditoria.RegistrarPerdida;
        if (path.Contains("danio")) return AccionAuditoria.RegistrarDanio;
        if (request.Method == HttpMethods.Put) return AccionAuditoria.ActualizarEstado;
        return AccionAuditoria.Registrar;
    }
}
