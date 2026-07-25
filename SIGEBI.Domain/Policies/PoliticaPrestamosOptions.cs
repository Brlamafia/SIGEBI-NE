using SIGEBI.Domain.Enums;

namespace SIGEBI.Domain.Policies;

public sealed class PoliticaPrestamosOptions
{
    public Dictionary<TipoUsuario, CondicionesPrestamo> Condiciones { get; init; } =
        new()
        {
            [TipoUsuario.Estudiante] = new(3, 7),
            [TipoUsuario.Docente] = new(5, 14),
            [TipoUsuario.Administrativo] = new(3, 7),
            [TipoUsuario.Externo] = new(1, 3)
        };

    public decimal MontoMultaPorDia { get; init; } = 50m;
    public decimal MontoMultaPorDanio { get; init; } = 500m;
    public decimal MontoMultaPorPerdida { get; init; } = 1500m;
    public int DiasAnticipacionRecordatorio { get; init; } = 2;
}
