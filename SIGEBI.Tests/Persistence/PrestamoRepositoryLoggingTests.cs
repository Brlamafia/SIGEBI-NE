using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIGEBI.Persistence.Context;
using SIGEBI.Persistence.Repositories.Prestamos;

namespace SIGEBI.Tests.Persistence;

public sealed class PrestamoRepositoryLoggingTests
{
    [Fact]
    public async Task ConsultaEspecializada_RegistraInicioYResultado()
    {
        var options = new DbContextOptionsBuilder<SigebiContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new SigebiContext(options);
        var logger = new ListLogger<PrestamoRepository>();
        var repository = new PrestamoRepository(context, logger);

        var result = await repository.ObtenerPorUsuarioAsync(25);

        Assert.Empty(result);
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("Iniciando consulta"));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("completada con 0 registros"));
    }

    [Fact]
    public async Task ConsultaEspecializada_RegistraErrorYRelanzaLaExcepcion()
    {
        var options = new DbContextOptionsBuilder<SigebiContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new SigebiContext(options);
        var logger = new ListLogger<PrestamoRepository>();
        var repository = new PrestamoRepository(context, logger);
        await context.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => repository.ObtenerPorUsuarioAsync(25));

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Error &&
            entry.Message.Contains("Error al consultar"));
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
