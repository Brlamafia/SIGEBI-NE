using FluentValidation;
using SIGEBI.Application.Dtos.Prestamos;

namespace SIGEBI.Application.Validations.Prestamos;

public sealed class RegistrarPrestamoValidator : AbstractValidator<RegistrarPrestamoDto>
{
    public RegistrarPrestamoValidator()
    {
        RuleFor(x => x.SolicitudPrestamoId).GreaterThan(0);
        RuleFor(x => x.EmpleadoPrestamoId).GreaterThan(0);
        RuleFor(x => x.FechaEsperadaDevolucion).GreaterThan(x => x.FechaPrestamo);
    }
}
