using FluentValidation;
using SIGEBI.Application.Dtos.Prestamos;

namespace SIGEBI.Application.Validations.Prestamos;

public sealed class ActualizarPrestamosVencidosValidator : AbstractValidator<ActualizarPrestamosVencidosDto>
{
    public ActualizarPrestamosVencidosValidator()
        => RuleFor(x => x.UsuarioResponsableId).GreaterThan(0);
}
