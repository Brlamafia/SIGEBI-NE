using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SIGEBI.Domain.Enums;
using SIGEBI.Persistence.Context;
using SIGEBI.Persistence.Repositories.Auditoria;
using AuditoriaEntidad = SIGEBI.Domain.Entities.Auditoria.Auditoria;

namespace SIGEBI.Tests.Persistence;

public sealed class AuditoriaRepositoryTests
{
    [Fact]
    public async Task ObtenerTodas_DevuelveLaPrimeraPaginaProtegida()
    {
        var options = new DbContextOptionsBuilder<SigebiContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new SigebiContext(options);
        var registros = Enumerable.Range(1, 501)
            .Select(index => new AuditoriaEntidad(
                1,
                ModuloAuditoria.Auditoria,
                AccionAuditoria.Registrar,
                $"Consulta {index}",
                ResultadoAuditoria.Exitoso));
        await context.Set<AuditoriaEntidad>().AddRangeAsync(registros);
        await context.SaveChangesAsync();
        var repository = new AuditoriaRepository(
            context,
            NullLogger<AuditoriaRepository>.Instance);

        var result = await repository.ObtenerTodasAsync();

        Assert.Equal(200, result.Count);
    }
}
