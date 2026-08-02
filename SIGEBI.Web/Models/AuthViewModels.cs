using System.ComponentModel.DataAnnotations;
using SIGEBI.Domain.Enums;

namespace SIGEBI.Web.Models;

public sealed class RegisterViewModel : IValidatableObject
{
    [Display(Name = "Tipo de lector")]
    public TipoUsuario? TipoUsuario { get; set; } =
        SIGEBI.Domain.Enums.TipoUsuario.Estudiante;

    [Required, StringLength(100)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [Display(Name = "Cédula")]
    public string Cedula { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), DataType(DataType.Password)]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "La contraseña debe contener mayúscula, minúscula y número.")]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext) =>
        ValidateReaderType(TipoUsuario);

    internal static IEnumerable<ValidationResult> ValidateReaderType(
        TipoUsuario? readerType)
    {
        if (readerType is not (
            SIGEBI.Domain.Enums.TipoUsuario.Estudiante or
            SIGEBI.Domain.Enums.TipoUsuario.Docente))
        {
            yield return new ValidationResult(
                "Selecciona Estudiante o Docente.",
                [nameof(TipoUsuario)]);
        }
    }
}

public sealed class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(8), DataType(DataType.Password)]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "La contraseña debe contener mayúscula, minúscula y número.")]
    [Display(Name = "Nueva contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class GoogleRegistrationViewModel : IValidatableObject
{
    [Display(Name = "Tipo de lector")]
    public TipoUsuario? TipoUsuario { get; set; } =
        SIGEBI.Domain.Enums.TipoUsuario.Estudiante;

    [Required, StringLength(100)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [Display(Name = "Cédula")]
    public string Cedula { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = string.Empty;

    [Display(Name = "Correo verificado por Google")]
    public string Email { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext) =>
        RegisterViewModel.ValidateReaderType(TipoUsuario);
}
