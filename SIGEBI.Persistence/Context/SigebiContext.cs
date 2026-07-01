// B.R
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Entities.Auditoria;

namespace SIGEBI.Persistence.Context
{
    public class SigebiContext : DbContext
    {
        public SigebiContext(DbContextOptions<SigebiContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Libro> Libros { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<SolicitudPrestamo> SolicitudesPrestamo { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<Prestamo> Prestamos { get; set; }
        public DbSet<Multa> Multas { get; set; }
        public DbSet<Inventario> Inventario { get; set; }
        public DbSet<Auditoria> Auditoria { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            object value = modelBuilder.Entity<Usuario>().ToTable("Usuario");
            modelBuilder.Entity<Libro>().ToTable("Libro");
            modelBuilder.Entity<Empleado>().ToTable("Empleado");
            modelBuilder.Entity<SolicitudPrestamo>().ToTable("SolicitudPrestamo");
            modelBuilder.Entity<Notificacion>().ToTable("Notificacion");
            modelBuilder.Entity<Prestamo>().ToTable("Prestamo");
            modelBuilder.Entity<Multa>().ToTable("Multa");
            modelBuilder.Entity<Inventario>().ToTable("Inventario");
            modelBuilder.Entity<Auditoria>().ToTable("Auditoria");
        }
    }
}