using AutoMapper;
using SIGEBI.Application.Dtos.Administradores;
using SIGEBI.Application.Dtos.Auditoria;
using SIGEBI.Application.Dtos.Cargos;
using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Dtos.Empleados;
using SIGEBI.Application.Dtos.Inventario;
using SIGEBI.Application.Dtos.Multas;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Dtos.Prestamos;
using SIGEBI.Application.Dtos.Roles;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Domain.Entities.Auditoria;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;

namespace SIGEBI.Application.Mappings
{
    // Mapeo entre capas: traduce entidades de dominio a DTOs para la capa de aplicación.
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Separación de capas: Catálogo se expone mediante DTOs y no mediante entidades.
            CreateMap<Libro, LibroDto>().ReverseMap();
            CreateMap<SaveLibroDto, Libro>()
                .ConstructUsing(src => new Libro(src.Titulo, src.Autor, src.ISBN, src.Genero, src.Editorial));
            CreateMap<UpdateLibroDto, Libro>()
                .AfterMap((src, dest) => dest.ActualizarDetalles(src.Titulo, src.Autor, src.Genero, src.Editorial));

            // Separación entre capas: Usuarios queda aislado de los detalles internos del dominio.
            CreateMap<Usuario, UsuarioDto>()
                .ForMember(dest => dest.TipoUsuario, opt => opt.MapFrom(src => src.TipoUsuario.ToString()))
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.Estado.ToString()))
                .ForMember(dest => dest.TieneMultasPendientes, opt => opt.Ignore());
            CreateMap<SaveUsuarioDto, Usuario>()
                .ConstructUsing(src => new Usuario(src.Nombre, src.Apellido, src.Cedula, src.Email, src.TipoUsuario));
            CreateMap<UpdateUsuarioDto, Usuario>()
                .AfterMap((src, dest) => dest.ActualizarContacto(src.Telefono, src.Email));

            // DTO Pattern: Solicitudes de préstamo viajan como datos planos entre capas.
            CreateMap<SolicitudPrestamo, SolicitudPrestamoDto>().ReverseMap();
            CreateMap<SaveSolicitudPrestamoDto, SolicitudPrestamo>()
                .ConstructUsing(src => new SolicitudPrestamo(src.UsuarioId, src.LibroId));

            // DTO Pattern: evita exponer comportamiento de Prestamo fuera de Domain.
            CreateMap<Prestamo, PrestamoDto>()
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.Estado.ToString()));

            // Comunicación entre capas: convierte enums de dominio a texto legible.
            CreateMap<Multa, MultaDto>()
                .ForMember(dest => dest.Tipo, opt => opt.MapFrom(src => src.Tipo.ToString()))
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.Estado.ToString()));

            // Separación de responsabilidades: InventarioDto representa solo estado consultable.
            CreateMap<Inventario, InventarioDto>();
            CreateMap<Ejemplar, EjemplarDto>()
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.Estado.ToString()));

            // Registro histórico de auditoría: se proyecta a DTO para consulta.
            CreateMap<Auditoria, AuditoriaDto>()
                .ForMember(dest => dest.Modulo, opt => opt.MapFrom(src => src.Modulo.ToString()))
                .ForMember(dest => dest.Accion, opt => opt.MapFrom(src => src.Accion.ToString()))
                .ForMember(dest => dest.Resultado, opt => opt.MapFrom(src => src.Resultado.ToString()));

            // Separación de capas: Notificaciones se transportan como DTOs.
            CreateMap<Notificacion, NotificacionDto>();
            CreateMap<SaveNotificacionDto, Notificacion>()
                .ConstructUsing(src => new Notificacion(src.UsuarioId, src.Mensaje));

            // DTO Pattern: Roles se comunican sin exponer colecciones internas del dominio.
            CreateMap<Rol, RolDto>();
            CreateMap<SaveRolDto, Rol>()
                .ConstructUsing(src => new Rol(src.Nombre, src.Descripcion));
            CreateMap<UpdateRolDto, Rol>()
                .AfterMap((src, dest) => dest.ActualizarDetalles(src.Nombre, src.Descripcion));

            // Separación entre capas: Cargos se comunican mediante DTOs.
            CreateMap<Cargo, CargoDto>();
            CreateMap<SaveCargoDto, Cargo>()
                .ConstructUsing(src => new Cargo(src.Nombre));
            CreateMap<UpdateCargoDto, Cargo>()
                .AfterMap((src, dest) => dest.Renombrar(src.Nombre));

            // DTO Pattern: Empleado se adapta para la capa de aplicación.
            CreateMap<Empleado, EmpleadoDto>();
            CreateMap<SaveEmpleadoDto, Empleado>()
                .ConstructUsing(src => new Empleado(src.UsuarioId, src.CargoId));
            CreateMap<UpdateEmpleadoDto, Empleado>()
                .AfterMap((src, dest) => dest.ActualizarCargo(src.CargoId));

            // DTO Pattern: Administrador se expone como contrato de datos.
            CreateMap<Administrador, AdministradorDto>().ReverseMap();
            CreateMap<SaveAdministradorDto, Administrador>();
            CreateMap<UpdateAdministradorDto, Administrador>();
        }
    }
}
