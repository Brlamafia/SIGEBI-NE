using AutoMapper;
using Moq;
using SIGEBI.Application.Dtos.Inventario;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Inventario;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Services.Catalogo;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Domain.Interfaces;

namespace SIGEBI.Tests.Application;

public class CatalogoBusquedaTests
{
    [Fact]
    public async Task Buscar_FiltraGeneroEditorialYDisponibilidad()
    {
        var libros = new[]
        {
            new Libro("Clean Code", "Robert Martin", "9781", "Tecnología", "Prentice"),
            new Libro("La fiesta del chivo", "Mario Vargas Llosa", "9782", "Novela", "Alfaguara")
        };
        AsignarId(libros[0], 1);
        AsignarId(libros[1], 2);
        var repository = new Mock<ILibroRepository>();
        repository.Setup(r => r.BuscarAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                string? termino,
                string? genero,
                string? editorial,
                bool? disponible,
                int? skip,
                int? take,
                CancellationToken _) =>
                libros.Where(libro =>
                        string.IsNullOrWhiteSpace(termino) ||
                        libro.Titulo.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                        libro.Autor.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                        libro.ISBN.Contains(termino, StringComparison.OrdinalIgnoreCase))
                    .Where(libro =>
                        string.IsNullOrWhiteSpace(genero) ||
                        libro.Genero.Contains(genero, StringComparison.OrdinalIgnoreCase))
                    .Where(libro =>
                        string.IsNullOrWhiteSpace(editorial) ||
                        libro.Editorial.Contains(editorial, StringComparison.OrdinalIgnoreCase))
                    .Skip(skip ?? 0)
                    .Take(take ?? int.MaxValue)
                    .ToArray());
        var inventario = new Mock<IInventarioService>();
        inventario.Setup(s => s.ObtenerPorLibrosAsync(
                It.IsAny<IReadOnlyCollection<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new InventarioDto { LibroId = 1, CantidadTotal = 2, CantidadDisponible = 1, CantidadPrestada = 1 },
                new InventarioDto { LibroId = 2, CantidadTotal = 1, CantidadDisponible = 0, CantidadPrestada = 1 }
            });
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<SIGEBI.Application.Dtos.Catalogo.LibroDto>(It.IsAny<Libro>()))
            .Returns<Libro>(libro => new SIGEBI.Application.Dtos.Catalogo.LibroDto
            {
                Id = libro.Id,
                Titulo = libro.Titulo,
                Autor = libro.Autor,
                ISBN = libro.ISBN,
                Genero = libro.Genero,
                Editorial = libro.Editorial,
                Estado = libro.Estado
            });
        var service = new LibroService(
            repository.Object,
            Mock.Of<IPrestamoRepository>(),
            inventario.Object,
            Mock.Of<IAuditoriaWriter>(),
            Mock.Of<IUsuarioActual>(),
            Mock.Of<IUnitOfWork>(),
            mapper.Object);

        var resultado = (await service.BuscarLibrosAsync(
            genero: "tec",
            editorial: "prent",
            disponible: true,
            skip: 0,
            take: 10)).ToArray();

        var libro = Assert.Single(resultado);
        Assert.Equal("Clean Code", libro.Titulo);
        Assert.Equal(1, libro.CantidadDisponible);
        repository.Verify(item => item.BuscarAsync(
            null,
            "tec",
            "prent",
            true,
            0,
            10,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static void AsignarId(SIGEBI.Domain.Base.EntidadBase entity, int id) =>
        typeof(SIGEBI.Domain.Base.EntidadBase)
            .GetProperty(nameof(SIGEBI.Domain.Base.EntidadBase.Id))!
            .SetValue(entity, id);
}
