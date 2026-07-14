using SIGEBI.Domain.Enums;

namespace SIGEBI.Domain.Policies;

public sealed record CondicionesPrestamo(int LimitePrestamos, int DiasPrestamo);

public sealed class PoliticaPrestamos
{
    private static readonly IReadOnlyDictionary<TipoUsuario, CondicionesPrestamo> Condiciones =
        new Dictionary<TipoUsuario, CondicionesPrestamo>
        {
            [TipoUsuario.Estudiante] = new(3, 7),
            [TipoUsuario.Docente] = new(5, 14),
            [TipoUsuario.Administrativo] = new(3, 7),
            [TipoUsuario.Externo] = new(1, 3)
        };

    public CondicionesPrestamo ObtenerCondiciones(TipoUsuario tipoUsuario)
        => Condiciones.TryGetValue(tipoUsuario, out var condiciones)
            ? condiciones
            : throw new ArgumentOutOfRangeException(nameof(tipoUsuario), "El tipo de usuario no es válido.");

    public DateTime CalcularFechaLimite(TipoUsuario tipoUsuario, DateTime fechaPrestamo)
    {
        if (fechaPrestamo == default)
            throw new ArgumentException("La fecha del préstamo es obligatoria.", nameof(fechaPrestamo));

        return fechaPrestamo.AddDays(ObtenerCondiciones(tipoUsuario).DiasPrestamo);
    }
}
