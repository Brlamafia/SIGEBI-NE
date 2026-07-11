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
            CreateMap<SaveLibroDto, Libro>();
            CreateMap<UpdateLibroDto, Libro>();

            // Separación entre capas: Usuarios queda aislado de los detalles internos del dominio.
            CreateMap<Usuario, UsuarioDto>().ReverseMap();
            CreateMap<SaveUsuarioDto, Usuario>();
            CreateMap<UpdateUsuarioDto, Usuario>();

            // DTO Pattern: Solicitudes de préstamo viajan como datos planos entre capas.
            CreateMap<SolicitudPrestamo, SolicitudPrestamoDto>().ReverseMap();
            CreateMap<SaveSolicitudPrestamoDto, SolicitudPrestamo>();
            CreateMap<UpdateSolicitudPrestamoDto, SolicitudPrestamo>();

            // DTO Pattern: evita exponer comportamiento de Prestamo fuera de Domain.
            CreateMap<Prestamo, PrestamoDto>()
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.Estado.ToString()));

            // Comunicación entre capas: convierte enums de dominio a texto legible.
            CreateMap<Multa, MultaDto>()
                .ForMember(dest => dest.Tipo, opt => opt.MapFrom(src => src.Tipo.ToString()))
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.Estado.ToString()));

            // Separación de responsabilidades: InventarioDto representa solo estado consultable.
            CreateMap<Inventario, InventarioDto>();

            // Registro histórico de auditoría: se proyecta a DTO para consulta.
            CreateMap<Auditoria, AuditoriaDto>()
                .ForMember(dest => dest.Modulo, opt => opt.MapFrom(src => src.Modulo.ToString()))
                .ForMember(dest => dest.Accion, opt => opt.MapFrom(src => src.Accion.ToString()))
                .ForMember(dest => dest.Resultado, opt => opt.MapFrom(src => src.Resultado.ToString()));

            // Separación de capas: Notificaciones se transportan como DTOs.
            CreateMap<Notificacion, NotificacionDto>().ReverseMap();
            CreateMap<SaveNotificacionDto, Notificacion>();

            // DTO Pattern: Roles se comunican sin exponer colecciones internas del dominio.
            CreateMap<Rol, RolDto>().ReverseMap();
            CreateMap<SaveRolDto, Rol>();
            CreateMap<UpdateRolDto, Rol>();

            // Separación entre capas: Cargos se comunican mediante DTOs.
            CreateMap<Cargo, CargoDto>().ReverseMap();
            CreateMap<SaveCargoDto, Cargo>();
            CreateMap<UpdateCargoDto, Cargo>();

            // DTO Pattern: Empleado se adapta para la capa de aplicación.
            CreateMap<Empleado, EmpleadoDto>().ReverseMap();
            CreateMap<SaveEmpleadoDto, Empleado>();
            CreateMap<UpdateEmpleadoDto, Empleado>();

            // DTO Pattern: Administrador se expone como contrato de datos.
            CreateMap<Administrador, AdministradorDto>().ReverseMap();
            CreateMap<SaveAdministradorDto, Administrador>();
            CreateMap<UpdateAdministradorDto, Administrador>();
        }
    }
}
