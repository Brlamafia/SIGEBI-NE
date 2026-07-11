using FluentValidation;
using SIGEBI.Application.Dtos.Inventario;

namespace SIGEBI.Application.Validations.Inventario
{
    public class AjustarInventarioValidator : AbstractValidator<AjustarInventarioDto>
    {
        public AjustarInventarioValidator()
        {
            RuleFor(x => x.InventarioId)
                .GreaterThan(0).WithMessage("Se requiere un inventario válido.");
            RuleFor(x => x.NuevaCantidadTotal)
                .GreaterThanOrEqualTo(0).WithMessage("La cantidad total no puede ser negativa.");
            RuleFor(x => x.UsuarioResponsableId)
                .GreaterThan(0).WithMessage("Se requiere un usuario responsable válido.");
        }
    }
}
