using System.ComponentModel.DataAnnotations;

namespace SIGEBI.Web.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Introduzca un correo válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Mantener mi sesión iniciada")]
    public bool Recordarme { get; set; }

    public string? ReturnUrl { get; set; }

    public bool GoogleEnabled { get; set; }
}
