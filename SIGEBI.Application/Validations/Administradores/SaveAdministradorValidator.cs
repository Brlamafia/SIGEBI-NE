using FluentValidation;
using SIGEBI.Application.Dtos.Administradores;

namespace SIGEBI.Application.Validations.Administradores;

public sealed class SaveAdministradorValidator : AbstractValidator<SaveAdministradorDto>
{
    public SaveAdministradorValidator()
    {
        RuleFor(x => x.UsuarioId).GreaterThan(0);
        RuleFor(x => x.CargoId).GreaterThan(0);
        RuleFor(x => x.UsuarioResponsableId).GreaterThan(0);
    }
}
