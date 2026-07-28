using Microsoft.EntityFrameworkCore;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Interfaces.Prestamos;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Enums;
using SIGEBI.Persistence.Context;

namespace SIGEBI.Web.Data;

internal static class CatalogDataSeeder
{
    private const int ExemplarsPerBook = 3;

    private static readonly CatalogBook[] Books =
    [
        new(
            "El principito",
            "Antoine de Saint-Exupéry",
            "9780156012195",
            "Fábula",
            "Salamandra"),
        new(
            "Don Quijote de la Mancha",
            "Miguel de Cervantes",
            "9788420412146",
            "Clásico",
            "Alfaguara"),
        new(
            "1984",
            "George Orwell",
            "9788499890944",
            "Distopía",
            "Debolsillo"),
        new(
            "La sombra del viento",
            "Carlos Ruiz Zafón",
            "9788408172178",
            "Misterio",
            "Planeta"),
        new(
            "Breve historia del tiempo",
            "Stephen Hawking",
            "9788498925221",
            "Ciencia",
            "Crítica"),
        new(
            "Rayuela",
            "Julio Cortázar",
            "9788437604572",
            "Novela",
            "Cátedra"),
        new(
            "Fahrenheit 451",
            "Ray Bradbury",
            "9788497930055",
            "Ciencia ficción",
            "Debolsillo"),
        new(
            "Orgullo y prejuicio",
            "Jane Austen",
            "9788491051328",
            "Romance clásico",
            "Austral"),
        new(
            "Crónica de una muerte anunciada",
            "Gabriel García Márquez",
            "9780307387264",
            "Realismo mágico",
            "Vintage Español")
    ];

    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SigebiContext>();
        var loanService =
            scope.ServiceProvider.GetRequiredService<IPrestamoService>();

        foreach (var item in Books)
        {
            var book = await context.Libros.SingleOrDefaultAsync(
                book => book.ISBN == item.Isbn,
                cancellationToken);

            if (book is null)
            {
                book = new Libro(
                    item.Title,
                    item.Author,
                    item.Isbn,
                    item.Genre,
                    item.Publisher);
                context.Libros.Add(book);
                await context.SaveChangesAsync(cancellationToken);
            }

            var inventory = await context.Inventario.SingleOrDefaultAsync(
                item => item.LibroId == book.Id,
                cancellationToken);
            if (inventory is null)
            {
                context.Inventario.Add(new Inventario(book.Id, ExemplarsPerBook));
            }
            else if (inventory.CantidadTotal < ExemplarsPerBook)
            {
                inventory.AjustarCantidadTotal(ExemplarsPerBook);
            }

            var registeredCodes = await context.Ejemplares
                .Where(item => item.LibroId == book.Id)
                .Select(item => item.Codigo)
                .ToListAsync(cancellationToken);
            var codePrefix = $"NE-{item.Isbn[^6..]}";

            for (var number = 1; number <= ExemplarsPerBook; number++)
            {
                var code = $"{codePrefix}-{number:D2}";
                if (!registeredCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
                {
                    context.Ejemplares.Add(new Ejemplar(book.Id, code));
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        await SeedPresentationEvidenceAsync(
            context,
            loanService,
            cancellationToken);
    }

    private static async Task SeedPresentationEvidenceAsync(
        SigebiContext context,
        IPrestamoService loanService,
        CancellationToken cancellationToken)
    {
        var reader = await context.Usuarios.FirstOrDefaultAsync(
            item => item.Nombre == "Bryant" &&
                    item.Apellido == "Romano" &&
                    item.Estado == EstadoUsuario.Activo,
            cancellationToken)
            ?? await context.Usuarios.FirstOrDefaultAsync(
                item => item.TipoUsuario == TipoUsuario.Estudiante &&
                        item.Estado == EstadoUsuario.Activo,
                cancellationToken);
        var employeeIds = await context.Database
            .SqlQuery<int>(
                $"""SELECT id_empleado AS "Value" FROM "Empleados" ORDER BY id_empleado LIMIT 1""")
            .ToListAsync(cancellationToken);
        var employeeId = employeeIds.FirstOrDefault();

        if (reader is null || employeeId <= 0)
        {
            return;
        }

        await SeedReturnedLoanAsync(
            context,
            reader.Id,
            employeeId,
            "9788437604572",
            DateTime.UtcNow.Date.AddDays(-45),
            14,
            12,
            cancellationToken);
        await SeedReturnedLoanAsync(
            context,
            reader.Id,
            employeeId,
            "9788497930055",
            DateTime.UtcNow.Date.AddDays(-28),
            10,
            15,
            cancellationToken);

        await SeedActiveLoanAsync(
            context,
            loanService,
            reader.Id,
            employeeId,
            "9788491051328",
            cancellationToken);

        await SeedPaidFineAsync(
            context,
            reader.Id,
            TipoMulta.Danio,
            125m,
            "Daño menor reportado en un ejemplar devuelto.",
            cancellationToken);
        const string cardReplacementReason =
            "Reposición de carnet bibliotecario.";
        await SeedPaidFineAsync(
            context,
            reader.Id,
            TipoMulta.Otra,
            75m,
            cardReplacementReason,
            cancellationToken);
        await SeedPendingFineAsync(
            context,
            reader.Id,
            cancellationToken);
    }

    private static async Task SeedActiveLoanAsync(
        SigebiContext context,
        IPrestamoService loanService,
        int userId,
        int employeeId,
        string isbn,
        CancellationToken cancellationToken)
    {
        if (await context.Prestamos.AnyAsync(
                item => item.UsuarioId == userId &&
                        (item.Estado == EstadoPrestamo.Activo ||
                         item.Estado == EstadoPrestamo.Vencido),
                cancellationToken))
        {
            return;
        }

        var book = await context.Libros.SingleAsync(
            item => item.ISBN == isbn,
            cancellationToken);
        var pendingRequests = await context.SolicitudesPrestamo
            .Where(item =>
                item.UsuarioId == userId &&
                item.LibroId == book.Id &&
                item.Estado == EstadoSolicitud.Pendiente)
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);
        var request = pendingRequests.FirstOrDefault();
        if (request is null)
        {
            request = new SolicitudPrestamo(userId, book.Id);
            context.SolicitudesPrestamo.Add(request);
        }
        else
        {
            foreach (var duplicate in pendingRequests.Skip(1))
                duplicate.Cancelar();
        }

        await context.SaveChangesAsync(cancellationToken);

        await loanService.RegistrarPrestamoAsync(
            new RegistrarPrestamoDto
            {
                SolicitudPrestamoId = request.Id,
                EmpleadoPrestamoId = employeeId,
                FechaPrestamo = DateTime.UtcNow.Date.AddDays(-2)
            },
            cancellationToken);
    }

    private static async Task SeedReturnedLoanAsync(
        SigebiContext context,
        int userId,
        int employeeId,
        string isbn,
        DateTime loanDate,
        int allowedDays,
        int returnedAfterDays,
        CancellationToken cancellationToken)
    {
        var book = await context.Libros.SingleAsync(
            item => item.ISBN == isbn,
            cancellationToken);
        if (await context.Prestamos.AnyAsync(
                item => item.UsuarioId == userId && item.LibroId == book.Id,
                cancellationToken))
        {
            return;
        }

        var exemplar = await context.Ejemplares
            .Where(item => item.LibroId == book.Id)
            .OrderBy(item => item.Id)
            .FirstAsync(cancellationToken);
        var request = new SolicitudPrestamo(userId, book.Id);
        request.Aprobar();
        context.SolicitudesPrestamo.Add(request);
        await context.SaveChangesAsync(cancellationToken);

        var loan = new Prestamo(
            userId,
            book.Id,
            exemplar.Id,
            request.Id,
            employeeId,
            loanDate,
            loanDate.AddDays(allowedDays));
        loan.RegistrarDevolucion(
            employeeId,
            loanDate.AddDays(returnedAfterDays));
        context.Prestamos.Add(loan);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedPaidFineAsync(
        SigebiContext context,
        int userId,
        TipoMulta type,
        decimal amount,
        string reason,
        CancellationToken cancellationToken)
    {
        if (!await context.Multas.AnyAsync(
                item => item.UsuarioId == userId && item.Motivo == reason,
                cancellationToken))
        {
            var fine = new Multa(userId, null, type, amount, reason);
            fine.MarcarComoPagada();
            context.Multas.Add(fine);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedPendingFineAsync(
        SigebiContext context,
        int userId,
        CancellationToken cancellationToken)
    {
        const string reason =
            "Reposición pendiente de material bibliográfico.";
        if (await context.Multas.AnyAsync(
                item => item.UsuarioId == userId && item.Motivo == reason,
                cancellationToken))
        {
            return;
        }

        context.Multas.Add(new Multa(
            userId,
            null,
            TipoMulta.Otra,
            50m,
            reason));
        context.Notificaciones.Add(new Notificacion(
            userId,
            "Tienes una multa pendiente de RD$50.00. Debes regularizarla antes de solicitar otro préstamo.",
            TipoNotificacion.Multa));
        await context.SaveChangesAsync(cancellationToken);
    }

    private sealed record CatalogBook(
        string Title,
        string Author,
        string Isbn,
        string Genre,
        string Publisher);
}
