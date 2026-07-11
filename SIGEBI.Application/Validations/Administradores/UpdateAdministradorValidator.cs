using FluentValidation;
using SIGEBI.Application.Dtos.Administradores;

namespace SIGEBI.Application.Validations.Administradores;

public sealed class UpdateAdministradorValidator : AbstractValidator<UpdateAdministradorDto>
{
    public UpdateAdministradorValidator()
    {
        RuleFor(x => x.CargoId).GreaterThan(0);
        RuleFor(x => x.UsuarioResponsableId).GreaterThan(0);
    }
}
