// B.R
using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Interfaces.Repositories;
using SIGEBI.Persistence.Context;
using Microsoft.Extensions.Logging; // 1. Agregamos esta librería para poder usar ILogger
using System; // 2. Agregamos System para poder usar la clase Exception
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIGEBI.Persistence.Base
{
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly SigebiContext _context;
        protected readonly DbSet<T> _dbSet;

        // 3. Declaramos la "Caja negra" (Logger)
        protected readonly ILogger _logger;

        // 4. Pedimos el ILogger en el constructor
        protected BaseRepository(SigebiContext context, ILogger logger)
        {
            _context = context;
            _dbSet = _context.Set<T>();
            _logger = logger;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Consultando todos los registros de {Entidad}", typeof(T).Name);
                var registros = await _dbSet.ToListAsync();
                _logger.LogInformation(
                    "Consulta de {Entidad} completada con {Cantidad} registros",
                    typeof(T).Name,
                    registros.Count);
                return registros;
            }
            catch (Exception ex)
            {
                // Si la base de datos falla, anotamos el error en rojo en la consola
                _logger.LogError(ex, "Error al obtener todos los registros de la tabla {Entidad}", typeof(T).Name);
                throw; // Lanzamos el error hacia arriba para que no pase desapercibido
            }
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Consultando {Entidad} con ID {Id}", typeof(T).Name, id);
                var registro = await _dbSet.FindAsync(id);
                _logger.LogInformation(
                    "Consulta de {Entidad} ID {Id} completada. Encontrado: {Encontrado}",
                    typeof(T).Name,
                    id,
                    registro is not null);
                return registro;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar el ID {Id} en la tabla {Entidad}", id, typeof(T).Name);
                throw;
            }
        }

        public virtual async Task AgregarAsync(T entity, CancellationToken ct = default)
        {
            try
            {
                // Anotamos que estamos a punto de hacer un cambio importante
                _logger.LogInformation("Iniciando la inserción de un nuevo registro en {Entidad}", typeof(T).Name);

                await _dbSet.AddAsync(entity, ct);
                await _context.SaveChangesAsync(ct);

                _logger.LogInformation("Registro guardado exitosamente en {Entidad}", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al intentar guardar en la tabla {Entidad}", typeof(T).Name);
                throw;
            }
        }

        public virtual async Task ActualizarAsync(T entity)
        {
            try
            {
                _logger.LogInformation("Iniciando actualización de un registro en {Entidad}", typeof(T).Name);
                _dbSet.Update(entity);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Actualización completada en {Entidad}", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la actualización en la tabla {Entidad}", typeof(T).Name);
                throw;
            }
        }

        public virtual async Task EliminarAsync(T entity)
        {
            try
            {
                _logger.LogInformation("Iniciando eliminación de un registro en {Entidad}", typeof(T).Name);
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Registro eliminado de la tabla {Entidad}", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar eliminar en la tabla {Entidad}", typeof(T).Name);
                throw;
            }
        }
    }
}
