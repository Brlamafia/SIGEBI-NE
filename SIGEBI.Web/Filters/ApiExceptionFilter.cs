using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SIGEBI.Web.Models;
using SIGEBI.Web.Services;

namespace SIGEBI.Web.Filters;

public sealed class ApiExceptionFilter(
    ILogger<ApiExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not SigebiApiException exception)
            return;

        logger.LogError(
            exception,
            "La capa de presentación no pudo completar la solicitud a la API. Código {StatusCode}.",
            exception.StatusCode);

        var statusCode = exception.StatusCode is >= 400 and <= 599
            ? exception.StatusCode
            : StatusCodes.Status500InternalServerError;
        var viewData = new ViewDataDictionary<ApiErrorViewModel>(
            new EmptyModelMetadataProvider(),
            context.ModelState)
        {
            Model = new ApiErrorViewModel
            {
                StatusCode = statusCode,
                Title = statusCode switch
                {
                    StatusCodes.Status503ServiceUnavailable => "Servicio temporalmente no disponible",
                    StatusCodes.Status504GatewayTimeout => "La respuesta está tardando demasiado",
                    StatusCodes.Status404NotFound => "Información no encontrada",
                    _ => "No pudimos completar la operación"
                },
                Message = exception.Message,
                RequestId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier
            }
        };

        context.HttpContext.Response.StatusCode = statusCode;
        context.Result = new ViewResult
        {
            ViewName = "~/Views/Shared/ApiError.cshtml",
            ViewData = viewData
        };
        context.ExceptionHandled = true;
    }
}
