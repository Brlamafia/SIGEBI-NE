using System.Data;
using AutoMapper;
using Moq;
using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Dtos.Inventario;
using SIGEBI.Application.Interfaces.Auditoria;
using SIGEBI.Application.Interfaces.Inventario;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Services.Catalogo;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Interfaces;
using SIGEBI.Domain.Interfaces.Repositories;

namespace SIGEBI.Tests.Application;

public sealed class CatalogoAtomicidadTests
{
    [Fact]
    public async Task CrearLibro_CoordinaLibroInventarioYAuditoriaEnUnaTransaccion()
    {
        var libro = new Libro("Clean Architecture", "Robert Martin", "9780", "Tecnología", "Prentice");
        AsignarId(libro, 12);
        var repositorio = new Mock<IRepository<Libro>>();
        repositorio.Setup(x => x.GetAllAsync()).ReturnsAsync([libro]);
        var inventario = new Mock<IInventarioService>();
        inventario.Setup(x => x.CrearAsync(
                It.IsAny<CrearInventarioDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventarioDto
            {
                LibroId = 12,
                CantidadTotal = 3,
                CantidadDisponible = 3
            });
        inventario.Setup(x => x.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new InventarioDto
                {
                    LibroId = 12,
                    CantidadTotal = 3,
                    CantidadDisponible = 3
                }
            ]);
        var auditoria = new Mock<IAuditoriaWriter>();
        var actual = new Mock<IUsuarioActual>();
        actual.SetupGet(x => x.UsuarioId).Returns(9);
        actual.SetupGet(x => x.EstaAutenticado).Returns(true);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.EjecutarEnTransaccionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> operation, IsolationLevel _, CancellationToken ct) =>
                operation(ct));
        var mapper = new Mock<IMapper>();
        mapper.Setup(x => x.Map<Libro>(It.IsAny<SaveLibroDto>())).Returns(libro);
        mapper.Setup(x => x.Map<LibroDto>(libro)).Returns(new LibroDto
        {
            Id = 12,
            Titulo = libro.Titulo,
            Autor = libro.Autor,
            ISBN = libro.ISBN,
            Genero = libro.Genero,
            Editorial = libro.Editorial,
            Estado = libro.Estado
        });

        var servicio = new LibroService(
            repositorio.Object,
            Mock.Of<IPrestamoRepository>(),
            inventario.Object,
            auditoria.Object,
            actual.Object,
            unitOfWork.Object,
            mapper.Object);

        var resultado = await servicio.AddAsync(new SaveLibroDto
        {
            Titulo = libro.Titulo,
            Autor = libro.Autor,
            ISBN = libro.ISBN,
            Genero = libro.Genero,
            Editorial = libro.Editorial,
            NumeroEjemplares = 3
        });

        Assert.Equal(12, resultado.Id);
        Assert.Equal(3, resultado.CantidadDisponible);
        repositorio.Verify(x => x.AgregarAsync(libro, It.IsAny<CancellationToken>()), Times.Once);
        inventario.Verify(x => x.CrearAsync(
            It.Is<CrearInventarioDto>(dto => dto.CantidadTotal == 3 && dto.LibroId == 12),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.EjecutarEnTransaccionAsync(
            It.IsAny<Func<CancellationToken, Task>>(),
            IsolationLevel.Serializable,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static void AsignarId(SIGEBI.Domain.Base.EntidadBase entity, int id) =>
        typeof(SIGEBI.Domain.Base.EntidadBase)
            .GetProperty(nameof(SIGEBI.Domain.Base.EntidadBase.Id))!
            .SetValue(entity, id);
}
