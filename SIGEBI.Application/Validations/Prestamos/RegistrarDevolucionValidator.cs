using FluentValidation;
using SIGEBI.Application.Dtos.Prestamos;

namespace SIGEBI.Application.Validations.Prestamos
{
    public class RegistrarDevolucionValidator : AbstractValidator<RegistrarDevolucionDto>
    {
        public RegistrarDevolucionValidator()
        {
            RuleFor(x => x.PrestamoId)
                .GreaterThan(0).WithMessage("Se requiere un préstamo válido.");
            RuleFor(x => x.EmpleadoDevolucionId)
                .GreaterThan(0).WithMessage("Se requiere un empleado responsable válido.");
            RuleFor(x => x.MontoMultaPorDia)
                .GreaterThan(0).WithMessage("El monto por día debe ser mayor que cero.");
        }
    }
}
