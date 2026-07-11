using FluentValidation;
using SIGEBI.Application.Dtos.Auditoria;

namespace SIGEBI.Application.Validations.Auditoria
{
    public class FiltroAuditoriaValidator : AbstractValidator<FiltroAuditoriaDto>
    {
        public FiltroAuditoriaValidator()
        {
            RuleFor(x => x.UsuarioResponsableId)
                .GreaterThan(0)
                .When(x => x.UsuarioResponsableId.HasValue)
                .WithMessage("El usuario responsable debe ser válido.");

            RuleFor(x => x.FechaHasta)
                .GreaterThanOrEqualTo(x => x.FechaDesde)
                .When(x => x.FechaDesde.HasValue && x.FechaHasta.HasValue)
                .WithMessage("La fecha final no puede ser anterior a la fecha inicial.");
        }
    }
}
