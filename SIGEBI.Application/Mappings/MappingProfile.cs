using AutoMapper;
using SIGEBI.Application.Dtos.Administradores;
using SIGEBI.Application.Dtos.Cargos;
using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Dtos.Empleados;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Dtos.Roles;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Dtos.Usuarios;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SIGEBI.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapeos del módulo de Catálogo (Libros)
            CreateMap<Libro, LibroDto>().ReverseMap();
            CreateMap<SaveLibroDto, Libro>();
            CreateMap<UpdateLibroDto, Libro>();

            // Mapeos del módulo de Usuarios
            CreateMap<Usuario, UsuarioDto>().ReverseMap();
            CreateMap<SaveUsuarioDto, Usuario>();
            CreateMap<UpdateUsuarioDto, Usuario>();

            // Mapeos del módulo de Solicitudes de Préstamo
            CreateMap<SolicitudPrestamo, SolicitudPrestamoDto>().ReverseMap();
            CreateMap<SaveSolicitudPrestamoDto, SolicitudPrestamo>();
            CreateMap<UpdateSolicitudPrestamoDto, SolicitudPrestamo>();

            // Mapeos del módulo de Notificaciones
            CreateMap<Notificacion, NotificacionDto>().ReverseMap();
            CreateMap<SaveNotificacionDto, Notificacion>();

            // Mapeos del módulo de Roles
            CreateMap<Rol, RolDto>().ReverseMap();
            CreateMap<SaveRolDto, Rol>();
            CreateMap<UpdateRolDto, Rol>();

            // Mapeos del módulo de Cargos
            CreateMap<Cargo, CargoDto>().ReverseMap();
            CreateMap<SaveCargoDto, Cargo>();
            CreateMap<UpdateCargoDto, Cargo>();

            // Mapeos de Empleado
            CreateMap<Empleado, EmpleadoDto>().ReverseMap();
            CreateMap<SaveEmpleadoDto, Empleado>();
            CreateMap<UpdateEmpleadoDto, Empleado>();

            // Mapeos de Administrador
            CreateMap<Administrador, AdministradorDto>().ReverseMap();
            CreateMap<SaveAdministradorDto, Administrador>();
            CreateMap<UpdateAdministradorDto, Administrador>();
        }
    }
}