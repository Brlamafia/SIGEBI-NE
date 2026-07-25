using FluentValidation;
using SIGEBI.Application.Dtos.Prestamos;

namespace SIGEBI.Application.Validations.Prestamos
{
    public class RegistrarPerdidaValidator : AbstractValidator<RegistrarPerdidaDto>
    {
        public RegistrarPerdidaValidator()
        {
            RuleFor(x => x.PrestamoId)
                .GreaterThan(0).WithMessage("Se requiere un préstamo válido.");
            RuleFor(x => x.EmpleadoResponsableId)
                .GreaterThan(0).WithMessage("Se requiere un empleado responsable válido.");
            RuleFor(x => x.Motivo)
                .NotEmpty().WithMessage("El motivo de la pérdida es obligatorio.")
                .MaximumLength(500).WithMessage("El motivo no puede exceder 500 caracteres.");
        }
    }
}
