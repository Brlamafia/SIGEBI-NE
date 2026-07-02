using SIGEBI.Persistence.Context;

namespace SIGEBI.Persistence.Base
{
    // Segregación de Interfaces (ISP): solo los repositorios mutables exponen actualización.
    public abstract class MutableRepository<T> : BaseRepository<T> where T : class
    {
        protected MutableRepository(SigebiContext context) : base(context) { }

        public void Actualizar(T entidad)
            => _dbSet.Update(entidad);
    }
}
