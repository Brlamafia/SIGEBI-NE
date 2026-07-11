using FluentValidation;
using SIGEBI.Application.Dtos.Multas;

namespace SIGEBI.Application.Validations.Multas
{
    public class PagarMultaValidator : AbstractValidator<PagarMultaDto>
    {
        public PagarMultaValidator()
        {
            RuleFor(x => x.MultaId)
                .GreaterThan(0).WithMessage("Se requiere una multa válida.");
            RuleFor(x => x.UsuarioResponsableId)
                .GreaterThan(0).WithMessage("Se requiere un usuario responsable válido.");
        }
    }
}
