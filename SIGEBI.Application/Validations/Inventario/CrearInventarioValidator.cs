using FluentValidation;
using SIGEBI.Application.Dtos.Inventario;

namespace SIGEBI.Application.Validations.Inventario;

public sealed class CrearInventarioValidator : AbstractValidator<CrearInventarioDto>
{
    public CrearInventarioValidator()
    {
        RuleFor(x => x.LibroId).GreaterThan(0);
        RuleFor(x => x.CantidadTotal).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UsuarioResponsableId).GreaterThan(0);
        RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
    }
}
