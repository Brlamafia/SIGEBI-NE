using SIGEBI.Domain.Enums;

namespace SIGEBI.Domain.Policies;

public sealed record CondicionesPrestamo(int LimitePrestamos, int DiasPrestamo);

public sealed class PoliticaPrestamos
{
    private readonly PoliticaPrestamosOptions _options;

    public PoliticaPrestamos()
        : this(new PoliticaPrestamosOptions())
    {
    }

    public PoliticaPrestamos(PoliticaPrestamosOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.MontoMultaPorDia < 0 ||
            options.MontoMultaPorDanio < 0 ||
            options.MontoMultaPorPerdida < 0 ||
            options.DiasAnticipacionRecordatorio < 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Los valores de la política no pueden ser negativos.");

        _options = options;
    }

    public decimal MontoMultaPorDia => _options.MontoMultaPorDia;
    public decimal MontoMultaPorDanio => _options.MontoMultaPorDanio;
    public decimal MontoMultaPorPerdida => _options.MontoMultaPorPerdida;
    public int DiasAnticipacionRecordatorio => _options.DiasAnticipacionRecordatorio;

    public CondicionesPrestamo ObtenerCondiciones(TipoUsuario tipoUsuario)
        => _options.Condiciones.TryGetValue(tipoUsuario, out var condiciones)
            ? condiciones
            : throw new ArgumentOutOfRangeException(nameof(tipoUsuario), "El tipo de usuario no es válido.");

    public DateTime CalcularFechaLimite(TipoUsuario tipoUsuario, DateTime fechaPrestamo)
    {
        if (fechaPrestamo == default)
            throw new ArgumentException("La fecha del préstamo es obligatoria.", nameof(fechaPrestamo));

        return fechaPrestamo.AddDays(ObtenerCondiciones(tipoUsuario).DiasPrestamo);
    }
}
