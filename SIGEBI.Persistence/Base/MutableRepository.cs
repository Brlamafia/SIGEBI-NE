using Microsoft.Extensions.Logging; // B.R: Agregamos el Logger
using SIGEBI.Persistence.Base; // Asegúrate que esto apunte a donde está BaseRepository
using SIGEBI.Persistence.Context;
using System; // B.R: Agregamos System para manejar las excepciones

namespace SIGEBI.Persistence.Base
{
    // Segregación de Interfaces (ISP): solo los repositorios mutables exponen actualización.
    public abstract class MutableRepository<T> : BaseRepository<T> where T : class
    {
        // B.R: Recibimos el logger aquí y se lo pasamos al padre (BaseRepository) mediante "base(...)"
        protected MutableRepository(SigebiContext context, ILogger<BaseRepository<T>> logger)
            : base(context, logger) { }

        public void Actualizar(T entidad)
        {
            try
            {
                // B.R: Dejamos registro de que se intentó actualizar
                _logger.LogInformation("Actualizando registro en la tabla {Entidad}", typeof(T).Name);

                _dbSet.Update(entidad);
            }
            catch (Exception ex)
            {
                // B.R: Si falla, atrapamos el error, lo logueamos y lo lanzamos hacia arriba
                _logger.LogError(ex, "Error al intentar actualizar en la tabla {Entidad}", typeof(T).Name);
                throw;
            }
        }
    }
}