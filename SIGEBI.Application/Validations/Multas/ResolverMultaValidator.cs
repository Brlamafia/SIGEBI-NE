using FluentValidation;
using SIGEBI.Application.Dtos.Multas;

namespace SIGEBI.Application.Validations.Multas
{
    public class ResolverMultaValidator : AbstractValidator<ResolverMultaDto>
    {
        public ResolverMultaValidator()
        {
            RuleFor(x => x.MultaId)
                .GreaterThan(0).WithMessage("Se requiere una multa válida.");
            RuleFor(x => x.EmpleadoResolucionId)
                .GreaterThan(0).WithMessage("Se requiere un empleado responsable válido.");
            RuleFor(x => x.Observacion)
                .NotEmpty().WithMessage("La observación es obligatoria.")
                .MaximumLength(500).WithMessage("La observación no puede exceder 500 caracteres.");
        }
    }
}
