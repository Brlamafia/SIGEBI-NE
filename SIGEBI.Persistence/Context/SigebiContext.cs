// B.R
// Separación de Responsabilidades: este contexto se ocupa únicamente del mapeo relacional.
using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Entities.Auditoria;

namespace SIGEBI.Persistence.Context
{
    // Patrón Unit of Work de EF Core: DbContext rastrea y confirma cambios como una unidad.
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
        public DbSet<Ejemplar> Ejemplares { get; set; }
        public DbSet<Auditoria> Auditoria { get; set; }
        public DbSet<Administrador> Administradores { get; set; }
        public DbSet<Cargo> Cargos { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Permiso> Permisos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Fluent API: mantiene las reglas de persistencia fuera de las entidades de Domain.
            modelBuilder.Entity<Usuario>().ToTable("Usuario");
            modelBuilder.Entity<Libro>().ToTable("Libro");
            modelBuilder.Entity<Empleado>().ToTable("Empleado");
            modelBuilder.Entity<SolicitudPrestamo>().ToTable("SolicitudPrestamo");
            modelBuilder.Entity<Notificacion>().ToTable("Notificacion");
            modelBuilder.Entity<Prestamo>().ToTable("Prestamo");
            modelBuilder.Entity<Multa>().ToTable("Multa");
            modelBuilder.Entity<Inventario>().ToTable("Inventario");
            modelBuilder.Entity<Ejemplar>().ToTable("Ejemplar");
            modelBuilder.Entity<Auditoria>().ToTable("Auditoria");
            modelBuilder.Entity<Administrador>().ToTable("Administrador");
            modelBuilder.Entity<Cargo>().ToTable("Cargo");
            modelBuilder.Entity<Rol>().ToTable("Rol");
            modelBuilder.Entity<Permiso>().ToTable("Permiso");

            ConfigurarUsuario(modelBuilder);
            ConfigurarLibro(modelBuilder);
            ConfigurarSolicitudPrestamo(modelBuilder);
            ConfigurarNotificacion(modelBuilder);
            ConfigurarCargo(modelBuilder);
            ConfigurarRol(modelBuilder);
            ConfigurarPermiso(modelBuilder);
            ConfigurarEmpleado(modelBuilder);
            ConfigurarAdministrador(modelBuilder);
            ConfigurarPrestamo(modelBuilder);
            ConfigurarMulta(modelBuilder);
            ConfigurarInventario(modelBuilder);
            ConfigurarEjemplar(modelBuilder);
            ConfigurarAuditoria(modelBuilder);
        }

        private static void ConfigurarUsuario(ModelBuilder modelBuilder)
        {
            var usuario = modelBuilder.Entity<Usuario>();

            usuario.Property(u => u.Nombre).HasMaxLength(100).IsRequired();
            usuario.Property(u => u.Apellido).HasMaxLength(100).IsRequired();
            usuario.Property(u => u.Cedula).HasMaxLength(20).IsRequired();
            usuario.Property(u => u.Telefono).HasMaxLength(30);
            usuario.Property(u => u.Email).HasMaxLength(150).IsRequired();
            usuario.HasIndex(u => u.Cedula).IsUnique();
            usuario.HasIndex(u => u.Email).IsUnique();

            usuario.HasMany(u => u.Roles)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "UsuarioRol",
                    relacion => relacion.HasOne<Rol>()
                        .WithMany()
                        .HasForeignKey("RolId")
                        .OnDelete(DeleteBehavior.Restrict),
                    relacion => relacion.HasOne<Usuario>()
                        .WithMany()
                        .HasForeignKey("UsuarioId")
                        .OnDelete(DeleteBehavior.Cascade),
                    relacion =>
                    {
                        relacion.ToTable("UsuarioRol");
                        relacion.HasKey("UsuarioId", "RolId");
                    });

            usuario.Navigation(u => u.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);
        }

        private static void ConfigurarLibro(ModelBuilder modelBuilder)
        {
            var libro = modelBuilder.Entity<Libro>();

            libro.Property(l => l.Titulo).HasMaxLength(250).IsRequired();
            libro.Property(l => l.Autor).HasMaxLength(200).IsRequired();
            libro.Property(l => l.ISBN).HasMaxLength(20).IsRequired();
            libro.Property(l => l.Genero).HasMaxLength(100);
            libro.Property(l => l.Editorial).HasMaxLength(150);
            libro.Property(l => l.Estado).HasMaxLength(30).IsRequired();
            libro.HasIndex(l => l.ISBN).IsUnique();
            libro.HasIndex(l => new { l.Titulo, l.Autor });
        }

        private static void ConfigurarSolicitudPrestamo(ModelBuilder modelBuilder)
        {
            var solicitud = modelBuilder.Entity<SolicitudPrestamo>();

            solicitud.Property(s => s.MotivoRechazo).HasMaxLength(500);
            solicitud.HasIndex(s => new { s.UsuarioId, s.Estado });
            solicitud.HasIndex(s => new { s.LibroId, s.Estado });
            solicitud.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            solicitud.HasOne<Libro>()
                .WithMany()
                .HasForeignKey(s => s.LibroId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigurarNotificacion(ModelBuilder modelBuilder)
        {
            var notificacion = modelBuilder.Entity<Notificacion>();

            notificacion.Property(n => n.Mensaje).HasMaxLength(1000).IsRequired();
            notificacion.HasIndex(n => new { n.UsuarioId, n.Leida, n.FechaEnvio });
            notificacion.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(n => n.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigurarCargo(ModelBuilder modelBuilder)
        {
            var cargo = modelBuilder.Entity<Cargo>();

            cargo.Property(c => c.Nombre).HasMaxLength(100).IsRequired();
            cargo.HasIndex(c => c.Nombre).IsUnique();
        }

        private static void ConfigurarRol(ModelBuilder modelBuilder)
        {
            var rol = modelBuilder.Entity<Rol>();

            rol.Property(r => r.Nombre).HasMaxLength(100).IsRequired();
            rol.Property(r => r.Descripcion).HasMaxLength(500);
            rol.HasIndex(r => r.Nombre).IsUnique();
            rol.HasMany(r => r.Permisos)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "RolPermiso",
                    relacion => relacion.HasOne<Permiso>()
                        .WithMany()
                        .HasForeignKey("PermisoId")
                        .OnDelete(DeleteBehavior.Restrict),
                    relacion => relacion.HasOne<Rol>()
                        .WithMany()
                        .HasForeignKey("RolId")
                        .OnDelete(DeleteBehavior.Cascade),
                    relacion =>
                    {
                        relacion.ToTable("RolPermiso");
                        relacion.HasKey("RolId", "PermisoId");
                    });

            rol.Navigation(r => r.Permisos).UsePropertyAccessMode(PropertyAccessMode.Field);
        }

        private static void ConfigurarPermiso(ModelBuilder modelBuilder)
        {
            var permiso = modelBuilder.Entity<Permiso>();

            permiso.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
            permiso.Property(p => p.Codigo).HasMaxLength(100).IsRequired();
            permiso.HasIndex(p => p.Codigo).IsUnique();
        }

        private static void ConfigurarEmpleado(ModelBuilder modelBuilder)
        {
            var empleado = modelBuilder.Entity<Empleado>();

            empleado.HasIndex(e => e.UsuarioId).IsUnique();
            empleado.HasOne(e => e.Usuario)
                .WithOne()
                .HasForeignKey<Empleado>(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            empleado.HasOne(e => e.Cargo)
                .WithMany()
                .HasForeignKey(e => e.CargoId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigurarAdministrador(ModelBuilder modelBuilder)
        {
            var administrador = modelBuilder.Entity<Administrador>();

            administrador.HasIndex(a => a.UsuarioId).IsUnique();
            administrador.HasOne(a => a.Usuario)
                .WithOne()
                .HasForeignKey<Administrador>(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            administrador.HasOne(a => a.Cargo)
                .WithMany()
                .HasForeignKey(a => a.CargoId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigurarPrestamo(ModelBuilder modelBuilder)
        {
            var prestamo = modelBuilder.Entity<Prestamo>();

            prestamo.HasIndex(p => p.SolicitudPrestamoId).IsUnique();
            prestamo.HasIndex(p => new { p.UsuarioId, p.Estado });
            prestamo.HasIndex(p => p.FechaEsperadaDevolucion);
            prestamo.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            prestamo.HasOne<Libro>()
                .WithMany()
                .HasForeignKey(p => p.LibroId)
                .OnDelete(DeleteBehavior.Restrict);
            prestamo.HasOne<Ejemplar>()
                .WithMany()
                .HasForeignKey(p => p.EjemplarId)
                .OnDelete(DeleteBehavior.Restrict);
            prestamo.HasOne<SolicitudPrestamo>()
                .WithOne()
                .HasForeignKey<Prestamo>(p => p.SolicitudPrestamoId)
                .OnDelete(DeleteBehavior.Restrict);
            prestamo.HasOne<Empleado>()
                .WithMany()
                .HasForeignKey(p => p.EmpleadoPrestamoId)
                .OnDelete(DeleteBehavior.Restrict);
            prestamo.HasOne<Empleado>()
                .WithMany()
                .HasForeignKey(p => p.EmpleadoDevolucionId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigurarMulta(ModelBuilder modelBuilder)
        {
            var multa = modelBuilder.Entity<Multa>();

            multa.Property(m => m.Monto).HasPrecision(18, 2);
            multa.Property(m => m.Motivo).HasMaxLength(500).IsRequired();
            multa.Property(m => m.ObservacionResolucion).HasMaxLength(500);
            multa.HasIndex(m => new { m.UsuarioId, m.Estado });
            multa.HasIndex(m => new { m.PrestamoId, m.Tipo })
                .IsUnique()
                .HasFilter("[PrestamoId] IS NOT NULL");
            multa.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            multa.HasOne<Prestamo>()
                .WithMany()
                .HasForeignKey(m => m.PrestamoId)
                .OnDelete(DeleteBehavior.Restrict);
            multa.HasOne<Empleado>()
                .WithMany()
                .HasForeignKey(m => m.EmpleadoResolucionId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigurarInventario(ModelBuilder modelBuilder)
        {
            var inventario = modelBuilder.Entity<Inventario>();

            inventario.ToTable("Inventario", tabla =>
            {
                tabla.HasCheckConstraint("CK_Inventario_CantidadTotal", "[CantidadTotal] >= 0");
                tabla.HasCheckConstraint("CK_Inventario_CantidadDisponible", "[CantidadDisponible] >= 0");
                tabla.HasCheckConstraint("CK_Inventario_CantidadPrestada", "[CantidadPrestada] >= 0");
                tabla.HasCheckConstraint("CK_Inventario_CantidadReservada", "[CantidadReservada] >= 0");
                tabla.HasCheckConstraint("CK_Inventario_CantidadFueraServicio", "[CantidadFueraServicio] >= 0");
                tabla.HasCheckConstraint("CK_Inventario_CantidadPerdida", "[CantidadPerdida] >= 0");
                tabla.HasCheckConstraint("CK_Inventario_CantidadDanada", "[CantidadDanada] >= 0");
                tabla.HasCheckConstraint(
                    "CK_Inventario_CantidadesConsistentes",
                    "[CantidadTotal] = [CantidadDisponible] + [CantidadPrestada] + [CantidadReservada] + [CantidadFueraServicio] + [CantidadPerdida] + [CantidadDanada]");
            });

            // Integridad de datos: cada libro posee un único registro de inventario.
            inventario.HasIndex(i => i.LibroId).IsUnique();
            // Concurrencia optimista: evita sobrescribir cantidades modificadas por otra operación.
            inventario.Property(i => i.CantidadDisponible).IsConcurrencyToken();
            inventario.Property(i => i.CantidadPrestada).IsConcurrencyToken();
            inventario.Property(i => i.CantidadReservada).IsConcurrencyToken();
            inventario.Property(i => i.CantidadFueraServicio).IsConcurrencyToken();
            inventario.Property(i => i.CantidadPerdida).IsConcurrencyToken();
            inventario.Property(i => i.CantidadDanada).IsConcurrencyToken();
            inventario.HasOne<Libro>()
                .WithOne()
                .HasForeignKey<Inventario>(i => i.LibroId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigurarEjemplar(ModelBuilder modelBuilder)
        {
            var ejemplar = modelBuilder.Entity<Ejemplar>();

            ejemplar.Property(e => e.Codigo).HasMaxLength(80).IsRequired();
            ejemplar.Property(e => e.Estado).HasConversion<string>().HasMaxLength(30).IsRequired();
            ejemplar.HasIndex(e => e.Codigo).IsUnique();
            ejemplar.HasIndex(e => new { e.LibroId, e.Estado });
            ejemplar.HasOne<Libro>()
                .WithMany()
                .HasForeignKey(e => e.LibroId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigurarAuditoria(ModelBuilder modelBuilder)
        {
            var auditoria = modelBuilder.Entity<Auditoria>();

            auditoria.Property(a => a.Modulo).HasConversion<string>().HasMaxLength(50).IsRequired();
            auditoria.Property(a => a.Accion).HasConversion<string>().HasMaxLength(50).IsRequired();
            auditoria.Property(a => a.Descripcion).HasMaxLength(1000).IsRequired();
            auditoria.Property(a => a.Resultado).HasConversion<string>().HasMaxLength(20).IsRequired();
            auditoria.HasIndex(a => new { a.Modulo, a.Fecha });
            auditoria.HasIndex(a => new { a.UsuarioResponsableId, a.Fecha });
            auditoria.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(a => a.UsuarioResponsableId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
