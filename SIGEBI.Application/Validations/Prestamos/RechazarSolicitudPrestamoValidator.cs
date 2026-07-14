using FluentValidation;
using SIGEBI.Application.Dtos.Prestamos;

namespace SIGEBI.Application.Validations.Prestamos;

public sealed class RechazarSolicitudPrestamoValidator : AbstractValidator<RechazarSolicitudPrestamoDto>
{
    public RechazarSolicitudPrestamoValidator()
    {
        RuleFor(x => x.SolicitudPrestamoId).GreaterThan(0);
        RuleFor(x => x.EmpleadoResponsableId).GreaterThan(0);
        RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
    }
}
