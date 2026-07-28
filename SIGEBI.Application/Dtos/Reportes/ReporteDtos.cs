namespace SIGEBI.Application.Dtos.Reportes;

public sealed record RecursoSolicitadoDto(
    int LibroId,
    string Titulo,
    string Genero,
    int Solicitudes);

public sealed record DemandaCategoriaDto(string Categoria, int Prestamos);

public sealed class ReporteCatalogoDto
{
    public DateTime Desde { get; init; }
    public DateTime Hasta { get; init; }
    public decimal DisponibilidadPromedioPorcentaje { get; init; }
    public IReadOnlyCollection<RecursoSolicitadoDto> RecursosMasSolicitados { get; init; } = [];
    public IReadOnlyCollection<DemandaCategoriaDto> DemandaPorCategoria { get; init; } = [];
}

public sealed class ReportePrestamosDto
{
    public DateTime Desde { get; init; }
    public DateTime Hasta { get; init; }
    public int TotalPrestamos { get; init; }
    public int DevolucionesPuntuales { get; init; }
    public int PrestamosVencidos { get; init; }
    public decimal TasaDevolucionPuntualPorcentaje { get; init; }
}

public sealed class ReporteMultasDto
{
    public DateTime Desde { get; init; }
    public DateTime Hasta { get; init; }
    public int Generadas { get; init; }
    public int Pendientes { get; init; }
    public int Pagadas { get; init; }
    public int Resueltas { get; init; }
    public decimal MontoTotal { get; init; }
}
