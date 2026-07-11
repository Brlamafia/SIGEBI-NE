using FluentValidation;
using SIGEBI.Application.Dtos.Prestamos;

namespace SIGEBI.Application.Validations.Prestamos
{
    public class RegistrarPrestamoValidator : AbstractValidator<RegistrarPrestamoDto>
    {
        public RegistrarPrestamoValidator()
        {
            RuleFor(x => x.SolicitudPrestamoId)
                .GreaterThan(0).WithMessage("Se requiere una solicitud de préstamo válida.");
            RuleFor(x => x.EmpleadoPrestamoId)
                .GreaterThan(0).WithMessage("Se requiere un empleado responsable válido.");
            RuleFor(x => x.LimitePrestamos)
                .GreaterThan(0).WithMessage("El límite de préstamos debe ser mayor que cero.");
            RuleFor(x => x.FechaEsperadaDevolucion)
                .GreaterThan(x => x.FechaPrestamo)
                .WithMessage("La fecha esperada de devolución debe ser posterior a la fecha de préstamo.");
        }
    }
}
