using Microsoft.Extensions.DependencyInjection;
using SIGEBI.Application.Interfaces;
using SIGEBI.Application.Services.Prestamos;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Repositories.Catalogo;
using SIGEBI.Persistence.Repositories.Usuarios;

namespace SIGEBI.IOC.Dependencies
{
    public static class UsuariosDependency
    {
        public static void AddUsuariosDependencies(this IServiceCollection services)
        {
            // Inyección del Repositorio (Persistencia)
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();

            // Inyección del Servicio (Aplicación) con RUTA ABSOLUTA
            services.AddScoped<SIGEBI.Application.Interfaces.IUsuarioService, UsuarioService>();
        }
    }
}