namespace SIGEBI.Web;

/// <summary>
/// Constantes compartidas del proyecto Web para evitar cadenas mágicas duplicadas.
/// </summary>
public static class WebConstants
{
    /// <summary>
    /// Nombre de la cookie de sesión principal usada para autenticación.
    /// </summary>
    public const string SessionCookieName = "SIGEBI.Web.Session";

    /// <summary>
    /// Nombre de la cookie temporal usada durante el flujo de autenticación externa (Google).
    /// </summary>
    public const string ExternalCookieName = "SIGEBI.Web.External";

    /// <summary>
    /// Esquema de autenticación para la cookie externa.
    /// </summary>
    public const string ExternalScheme = "External";
}
