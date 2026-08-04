using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Persistence.Context;
using SIGEBI.Persistence.Repositories.Usuarios;

namespace SIGEBI.Tests.Persistence;

public sealed class RolRepositoryLoggingTests
{
    [Fact]
    public async Task ObtenerPorId_RegistraInicioYResultado()
    {
        var options = new DbContextOptionsBuilder<SigebiContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new SigebiContext(options);
        var logger = new ListLogger<RolRepository>();
        var repository = new RolRepository(context, logger);

        var result = await repository.GetByIdAsync(25);

        Assert.Null(result);
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("Consultando rol 25"));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("Encontrado: False"));
    }

    [Fact]
    public async Task ObtenerPorId_RegistraErrorYConservaLaExcepcion()
    {
        var options = new DbContextOptionsBuilder<SigebiContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new SigebiContext(options);
        var logger = new ListLogger<RolRepository>();
        var repository = new RolRepository(context, logger);
        await context.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => repository.GetByIdAsync(25));

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Error &&
            entry.Message.Contains("Error al consultar el rol 25"));
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
