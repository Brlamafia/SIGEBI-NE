using FluentValidation;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;

namespace SIGEBI.Application.Validations
{
    public class SaveSolicitudPrestamoValidator : AbstractValidator<SaveSolicitudPrestamoDto>
    {
        public SaveSolicitudPrestamoValidator()
        {
            RuleFor(x => x.LibroId)
                .GreaterThan(0).WithMessage("Se requiere un identificador de libro válido.");
        }
    }
}
