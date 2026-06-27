using Microsoft.Extensions.DependencyInjection;
using SIGEBI.Application.Interfaces;
using SIGEBI.Application.Services;
using SIGEBI.Persistence.Interfaces.Usuarios;
using SIGEBI.Persistence.Repositories.UsuariosRepository;

namespace SIGEBI.IOC.Dependencies
{
    public static class UsuariosDependency
    {
        public static void AddUsuariosDependencies(this IServiceCollection services)
        {
            // Inyección del Repositorio (Persistencia)
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();

            // Inyección del Servicio (Aplicación) con RUTA ABSOLUTA
            services.AddScoped<SIGEBI.Application.Interfaces.IUsuarioService, SIGEBI.Application.Services.UsuarioService>();
        }
    }
}