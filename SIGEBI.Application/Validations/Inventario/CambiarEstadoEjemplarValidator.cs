using FluentValidation;
using SIGEBI.Application.Dtos.Inventario;

namespace SIGEBI.Application.Validations.Inventario;

public sealed class CambiarEstadoEjemplarValidator : AbstractValidator<CambiarEstadoEjemplarDto>
{
    public CambiarEstadoEjemplarValidator()
    {
        RuleFor(x => x.EjemplarId).GreaterThan(0);
        RuleFor(x => x.NuevoEstado).NotEmpty();
        RuleFor(x => x.UsuarioResponsableId).GreaterThan(0);
        RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
    }
}
