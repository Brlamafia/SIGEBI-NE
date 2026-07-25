using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Persistence.Context;
using SIGEBI.Persistence.Repositories.Usuarios;

namespace SIGEBI.Tests.Persistence;

public class UsuarioRepositoryLoggingTests
{
    [Fact]
    public async Task ObtenerPorEmail_RegistraLaOperacionExitosa()
    {
        var options = new DbContextOptionsBuilder<SigebiContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new SigebiContext(options);
        var usuario = new Usuario("Ana", "Pérez", "001", "ana@sigebi.test", TipoUsuario.Estudiante);
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
        var logger = new ListLogger<UsuarioRepository>();
        var repository = new UsuarioRepository(context, logger);

        var result = await repository.ObtenerPorEmailAsync("ana@sigebi.test");

        Assert.NotNull(result);
        Assert.Contains(logger.Messages, message => message.Contains("Consultando usuario por correo"));
    }

    [Fact]
    public async Task ObtenerPorEmail_RegistraErrorYConservaLaExcepcion()
    {
        var options = new DbContextOptionsBuilder<SigebiContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new SigebiContext(options);
        var logger = new ListLogger<UsuarioRepository>();
        var repository = new UsuarioRepository(context, logger);
        await context.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => repository.ObtenerPorEmailAsync("error@sigebi.test"));

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Error &&
            entry.Message.Contains("Error buscando usuario por email"));
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            Messages.Add(message);
            Entries.Add((logLevel, message));
        }
    }
}
