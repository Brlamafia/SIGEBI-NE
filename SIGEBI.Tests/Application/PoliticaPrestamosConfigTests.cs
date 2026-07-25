using SIGEBI.Domain.Enums;
using SIGEBI.Domain.Policies;

namespace SIGEBI.Tests.Application;

public class PoliticaPrestamosConfigTests
{
    [Fact]
    public void UsaMontosYCondicionesConfiguradas()
    {
        var policy = new PoliticaPrestamos(new PoliticaPrestamosOptions
        {
            MontoMultaPorDia = 125m,
            MontoMultaPorDanio = 900m,
            MontoMultaPorPerdida = 2500m,
            DiasAnticipacionRecordatorio = 3,
            Condiciones = new Dictionary<TipoUsuario, CondicionesPrestamo>
            {
                [TipoUsuario.Estudiante] = new(2, 5)
            }
        });

        Assert.Equal(125m, policy.MontoMultaPorDia);
        Assert.Equal(900m, policy.MontoMultaPorDanio);
        Assert.Equal(2500m, policy.MontoMultaPorPerdida);
        Assert.Equal(3, policy.DiasAnticipacionRecordatorio);
        Assert.Equal(new CondicionesPrestamo(2, 5), policy.ObtenerCondiciones(TipoUsuario.Estudiante));
    }
}
