namespace SIGEBI.Web.Services;

public sealed class SigebiApiException(
    string message,
    int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
