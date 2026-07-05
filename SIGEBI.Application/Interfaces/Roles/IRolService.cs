using SIGEBI.Application.Base;
using SIGEBI.Application.Dtos.Roles;
using System.Threading.Tasks;

namespace SIGEBI.Application.Interfaces.Roles
{
    public interface IRolService : IBaseService<RolDto>
    {
        // El CRUD básico (GetAll, GetById) ya viene heredado de IBaseService.
        // Aquí podemos agregar métodos específicos si la arquitectura lo requiere más adelante, 
        // por ejemplo: Task<RolDto> ObtenerPorNombreAsync(string nombre);
    }
}