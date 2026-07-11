namespace SIGEBI.Domain.Policies;

public sealed class PoliticaPrestamos
{
    public int LimitePrestamosPorUsuario { get; }

    public PoliticaPrestamos(int limitePrestamosPorUsuario = 3)
    {
        if (limitePrestamosPorUsuario <= 0)
            throw new ArgumentOutOfRangeException(nameof(limitePrestamosPorUsuario));

        LimitePrestamosPorUsuario = limitePrestamosPorUsuario;
    }
}
