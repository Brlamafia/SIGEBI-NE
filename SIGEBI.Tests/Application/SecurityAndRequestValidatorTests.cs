using SIGEBI.Application.Dtos.Roles;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Validations;

namespace SIGEBI.Tests.Application;

public sealed class SecurityAndRequestValidatorTests
{
    [Fact]
    public void SolicitudRechazada_ExigeMotivo()
    {
        var validator = new UpdateSolicitudPrestamoValidator();

        var invalid = validator.Validate(new UpdateSolicitudPrestamoDto
        {
            Id = 4,
            Estado = "Rechazada"
        });
        var valid = validator.Validate(new UpdateSolicitudPrestamoDto
        {
            Id = 4,
            Estado = "Rechazada",
            MotivoRechazo = "El usuario posee préstamos vencidos."
        });

        Assert.False(invalid.IsValid);
        Assert.Contains(invalid.Errors, error =>
            error.PropertyName == nameof(UpdateSolicitudPrestamoDto.MotivoRechazo));
        Assert.True(valid.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sigebi admin")]
    [InlineData("sigebi/admin")]
    public void CodigoPermiso_RechazaFormatosNoNormalizados(string codigo)
    {
        var result = new SavePermisoValidator().Validate(new SavePermisoDto
        {
            Nombre = "Administración",
            Codigo = codigo
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Asignaciones_ExigenIdentificadoresValidos()
    {
        Assert.False(new AsignarRolValidator().Validate(new AsignarRolDto()).IsValid);
        Assert.False(new AsignarPermisoValidator().Validate(new AsignarPermisoDto()).IsValid);
    }
}
