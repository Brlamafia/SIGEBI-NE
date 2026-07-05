using FluentValidation;
using SIGEBI.Application.Dtos.Roles;

namespace SIGEBI.Application.Validations
{
    public class SaveRolValidator : AbstractValidator<SaveRolDto>
    {
        public SaveRolValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del rol es obligatorio.")
                .Length(3, 50).WithMessage("El nombre debe tener entre 3 y 50 caracteres.");

            RuleFor(x => x.Descripcion)
                .NotEmpty().WithMessage("La descripción es obligatoria.")
                .MaximumLength(150).WithMessage("La descripción no puede exceder los 150 caracteres.");
        }
    }
}