using Microsoft.EntityFrameworkCore;

namespace SIGEBI.Persistence.Context
{
    // AQUI ESTA LA MAGIA: Heredamos de DbContext
    public class SigebiContext : DbContext
    {
        // Este constructor es obligatorio para que la API le pase la cadena de conexión
        public SigebiContext(DbContextOptions<SigebiContext> options) : base(options)
        {
        }
    }
}