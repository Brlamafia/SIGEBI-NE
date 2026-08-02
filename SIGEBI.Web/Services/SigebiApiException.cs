namespace SIGEBI.Web.Services;

public sealed class SigebiApiException(
    string message,
    int statusCode,
    Exception? innerException = null,
    string? responseDetail = null) : Exception(message, innerException)
{
    public int StatusCode { get; } = statusCode;
    public string? ResponseDetail { get; } = responseDetail;
}
