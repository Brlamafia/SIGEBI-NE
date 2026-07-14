namespace SIGEBI.API.Jobs;

public sealed class PrestamosVencidosOptions
{
    public const string SectionName = "Jobs:PrestamosVencidos";
    public bool Habilitado { get; set; } = true;
    public int IntervaloMinutos { get; set; } = 60;
    public int UsuarioResponsableId { get; set; } = 1;
}
