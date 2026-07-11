using FluentValidation;
using SIGEBI.Application.Dtos.Prestamos;

namespace SIGEBI.Application.Validations.Prestamos
{
    public class CancelarPrestamoValidator : AbstractValidator<CancelarPrestamoDto>
    {
        public CancelarPrestamoValidator()
        {
            RuleFor(x => x.PrestamoId)
                .GreaterThan(0).WithMessage("Se requiere un préstamo válido.");
            RuleFor(x => x.EmpleadoResponsableId)
                .GreaterThan(0).WithMessage("Se requiere un empleado responsable válido.");
        }
    }
}
