using FluentValidation;
using SIGEBI.Application.Dtos.Cargos;
using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Dtos.Empleados;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Application.Dtos.Auth;

namespace SIGEBI.Application.Validations;

public sealed class SaveLibroValidator : AbstractValidator<SaveLibroDto>
{
    public SaveLibroValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Autor).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ISBN).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Genero).MaximumLength(100);
        RuleFor(x => x.Editorial).MaximumLength(150);
        RuleFor(x => x.NumeroEjemplares).GreaterThan(0).LessThanOrEqualTo(500);
    }
}

public sealed class UpdateLibroValidator : AbstractValidator<UpdateLibroDto>
{
    public UpdateLibroValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Autor).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Genero).MaximumLength(100);
        RuleFor(x => x.Editorial).MaximumLength(150);
    }
}

public sealed class SaveUsuarioValidator : AbstractValidator<SaveUsuarioDto>
{
    public SaveUsuarioValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Apellido).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Cedula).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Telefono).MaximumLength(20);
        RuleFor(x => x.TipoUsuario).IsInEnum();
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("La contraseña debe contener una mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe contener una minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe contener un número.");
    }
}

public sealed class CambiarPasswordValidator : AbstractValidator<CambiarPasswordDto>
{
    public CambiarPasswordValidator()
    {
        RuleFor(x => x.PasswordActual).NotEmpty();
        RuleFor(x => x.PasswordNueva)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("La contraseña debe contener una mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe contener una minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe contener un número.");
    }
}

public sealed class UpdateUsuarioValidator : AbstractValidator<UpdateUsuarioDto>
{
    public UpdateUsuarioValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Apellido).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Cedula).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Telefono).MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.TipoUsuario).IsInEnum();
        RuleFor(x => x.Estado).IsInEnum();
    }
}

public sealed class SaveCargoValidator : AbstractValidator<SaveCargoDto>
{
    public SaveCargoValidator() => RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
}

public sealed class UpdateCargoValidator : AbstractValidator<UpdateCargoDto>
{
    public UpdateCargoValidator() => RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
}

public sealed class SaveEmpleadoValidator : AbstractValidator<SaveEmpleadoDto>
{
    public SaveEmpleadoValidator()
    {
        RuleFor(x => x.UsuarioId).GreaterThan(0);
        RuleFor(x => x.CargoId).GreaterThan(0);
    }
}

public sealed class UpdateEmpleadoValidator : AbstractValidator<UpdateEmpleadoDto>
{
    public UpdateEmpleadoValidator() => RuleFor(x => x.CargoId).GreaterThan(0);
}

public sealed class SaveNotificacionValidator : AbstractValidator<SaveNotificacionDto>
{
    public SaveNotificacionValidator()
    {
        RuleFor(x => x.UsuarioId).GreaterThan(0);
        RuleFor(x => x.Mensaje).NotEmpty().MaximumLength(1000);
    }
}
