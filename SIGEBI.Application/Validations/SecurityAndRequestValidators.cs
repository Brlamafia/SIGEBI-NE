using FluentValidation;
using SIGEBI.Application.Dtos.Roles;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;

namespace SIGEBI.Application.Validations;

public sealed class UpdateSolicitudPrestamoValidator
    : AbstractValidator<UpdateSolicitudPrestamoDto>
{
    private static readonly string[] ValidStates = ["Aprobada", "Rechazada"];

    public UpdateSolicitudPrestamoValidator()
    {
        RuleFor(item => item.Id).GreaterThan(0);
        RuleFor(item => item.Estado)
            .NotEmpty()
            .Must(state => ValidStates.Contains(
                state,
                StringComparer.OrdinalIgnoreCase))
            .WithMessage("El estado debe ser Aprobada o Rechazada.");
        RuleFor(item => item.MotivoRechazo)
            .NotEmpty()
            .MaximumLength(255)
            .When(item => string.Equals(
                item.Estado,
                "Rechazada",
                StringComparison.OrdinalIgnoreCase));
        RuleFor(item => item.MotivoRechazo)
            .MaximumLength(255)
            .When(item => !string.IsNullOrWhiteSpace(item.MotivoRechazo));
    }
}

public sealed class UpdateRolValidator : AbstractValidator<UpdateRolDto>
{
    public UpdateRolValidator()
    {
        RuleFor(item => item.Nombre).NotEmpty().Length(3, 50);
        RuleFor(item => item.Descripcion).NotEmpty().MaximumLength(150);
    }
}

public sealed class AsignarRolValidator : AbstractValidator<AsignarRolDto>
{
    public AsignarRolValidator()
    {
        RuleFor(item => item.UsuarioId).GreaterThan(0);
        RuleFor(item => item.RolId).GreaterThan(0);
    }
}

public sealed class AsignarPermisoValidator : AbstractValidator<AsignarPermisoDto>
{
    public AsignarPermisoValidator()
    {
        RuleFor(item => item.RolId).GreaterThan(0);
        RuleFor(item => item.PermisoId).GreaterThan(0);
    }
}

public sealed class SavePermisoValidator : AbstractValidator<SavePermisoDto>
{
    public SavePermisoValidator()
    {
        RuleFor(item => item.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(item => item.Codigo)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[A-Z0-9]+(?:[._-][A-Z0-9]+)*$")
            .WithMessage(
                "El código solo puede contener mayúsculas, números, puntos, guiones y guiones bajos.");
    }
}
