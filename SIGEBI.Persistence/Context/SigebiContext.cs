using Microsoft.EntityFrameworkCore;
using SIGEBI.Domain.Entities.Auditoria;
using SIGEBI.Domain.Entities.Catalogo;
using SIGEBI.Domain.Entities.Notificaciones;
using SIGEBI.Domain.Entities.Prestamos;
using SIGEBI.Domain.Entities.Usuarios;
using SIGEBI.Domain.Enums;
using SIGEBI.Persistence.Models;

namespace SIGEBI.Persistence.Context;

// El contexto adapta el modelo de dominio al esquema PostgreSQL de SIGEBI.
public class SigebiContext : DbContext
{
    public SigebiContext(DbContextOptions<SigebiContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Libro> Libros => Set<Libro>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<SolicitudPrestamo> SolicitudesPrestamo => Set<SolicitudPrestamo>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<Prestamo> Prestamos => Set<Prestamo>();
    public DbSet<Multa> Multas => Set<Multa>();
    public DbSet<Inventario> Inventario => Set<Inventario>();
    public DbSet<Ejemplar> Ejemplares => Set<Ejemplar>();
    public DbSet<Auditoria> Auditoria => Set<Auditoria>();
    public DbSet<Administrador> Administradores => Set<Administrador>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<PrestamoEjemplarRelacion> PrestamoEjemplares => Set<PrestamoEjemplarRelacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
        ConfigurarPrestamoEjemplar(modelBuilder);
        ConfigurarMulta(modelBuilder);
        ConfigurarInventario(modelBuilder);
        ConfigurarEjemplar(modelBuilder);
        ConfigurarAuditoria(modelBuilder);
    }

    public override int SaveChanges()
    {
        PrepararNotificacionesLegadas();
        PrepararCargosLegados();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        PrepararNotificacionesLegadas();
        await PrepararCargosLegadosAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void PrepararNotificacionesLegadas()
    {
        foreach (var entry in ChangeTracker.Entries<Notificacion>()
                     .Where(e => e.State == EntityState.Added))
        {
            if (!Enum.IsDefined(entry.Entity.TipoEvento))
                entry.Property(n => n.TipoEvento).CurrentValue = TipoNotificacion.Informacion;
        }
    }

    private void PrepararCargosLegados()
    {
        var entries = ChangeTracker.Entries<Empleado>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Where(RequiereResolverCargo)
            .ToArray();
        var idsPendientes = entries
            .Where(entry => ObtenerNombreCargoRastreado(entry.Entity) is null)
            .Select(entry => entry.Entity.CargoId)
            .Distinct()
            .ToArray();
        var cargos = idsPendientes.Length == 0
            ? new Dictionary<int, string>()
            : Cargos.AsNoTracking()
                .Where(cargo => idsPendientes.Contains(cargo.Id))
                .ToDictionary(cargo => cargo.Id, cargo => cargo.Nombre);

        foreach (var entry in entries)
        {
            var nombreCargo = ObtenerNombreCargoRastreado(entry.Entity)
                ?? cargos.GetValueOrDefault(entry.Entity.CargoId);

            AsignarCargoLegado(entry, nombreCargo);
        }
    }

    private async Task PrepararCargosLegadosAsync(CancellationToken cancellationToken)
    {
        var entries = ChangeTracker.Entries<Empleado>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Where(RequiereResolverCargo)
            .ToArray();
        var idsPendientes = entries
            .Where(entry => ObtenerNombreCargoRastreado(entry.Entity) is null)
            .Select(entry => entry.Entity.CargoId)
            .Distinct()
            .ToArray();
        var cargos = idsPendientes.Length == 0
            ? new Dictionary<int, string>()
            : await Cargos.AsNoTracking()
                .Where(cargo => idsPendientes.Contains(cargo.Id))
                .ToDictionaryAsync(cargo => cargo.Id, cargo => cargo.Nombre, cancellationToken);

        foreach (var entry in entries)
        {
            var nombreCargo = ObtenerNombreCargoRastreado(entry.Entity)
                ?? cargos.GetValueOrDefault(entry.Entity.CargoId);

            AsignarCargoLegado(entry, nombreCargo);
        }
    }

    private string? ObtenerNombreCargoRastreado(Empleado empleado) =>
        empleado.Cargo?.Nombre
        ?? ChangeTracker.Entries<Cargo>()
            .FirstOrDefault(c => c.Entity.Id == empleado.CargoId)
            ?.Entity.Nombre;

    private static bool RequiereResolverCargo(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Empleado> entry)
    {
        var cargoLegado = entry.Property<string>("cargo").CurrentValue;
        return entry.State == EntityState.Added
            || entry.Property(e => e.CargoId).IsModified
            || string.IsNullOrWhiteSpace(cargoLegado);
    }

    private static void AsignarCargoLegado(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Empleado> entry,
        string? nombreCargo)
    {
        if (string.IsNullOrWhiteSpace(nombreCargo))
            throw new InvalidOperationException(
                $"No se encontró el cargo {entry.Entity.CargoId} asignado al empleado.");

        entry.Property<string>("cargo").CurrentValue = nombreCargo;
    }

    private static void ConfigurarUsuario(ModelBuilder modelBuilder)
    {
        var usuario = modelBuilder.Entity<Usuario>();
        usuario.ToTable("Usuarios");
        usuario.HasKey(u => u.Id);
        usuario.Property(u => u.Id).HasColumnName("id_usuario");
        usuario.Property(u => u.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        usuario.Property(u => u.Apellido).HasColumnName("apellido").HasMaxLength(100).IsRequired();
        usuario.Property(u => u.Cedula).HasColumnName("cedula").HasMaxLength(20).IsRequired();
        usuario.Property(u => u.Telefono).HasColumnName("numero_telefono").HasMaxLength(20);
        usuario.Property(u => u.Email).HasColumnName("email").HasMaxLength(100).IsRequired();
        usuario.Property(u => u.TipoUsuario).HasColumnName("tipo_usuario").HasConversion<string>().HasMaxLength(50);
        usuario.Property(u => u.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(20);
        usuario.Property(u => u.IntentosAccesoFallidos).HasColumnName("intentos_acceso_fallidos");
        usuario.Property(u => u.BloqueadoHasta).HasColumnName("bloqueado_hasta");
        usuario.Property(u => u.FechaCreacion).HasColumnName("fecha_registro");
        usuario.Ignore(u => u.FechaModificacion);
        usuario.Property(u => u.ContrasenaHash)
            .HasColumnName("contrasena_hash")
            .HasMaxLength(255)
            .IsRequired();
        usuario.HasIndex(u => u.Cedula).IsUnique();
        usuario.HasIndex(u => u.Email).IsUnique();

        usuario.HasMany(u => u.Roles)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "UsuarioRol",
                relacion => relacion.HasOne<Rol>().WithMany()
                    .HasForeignKey("id_rol").OnDelete(DeleteBehavior.Restrict),
                relacion => relacion.HasOne<Usuario>().WithMany()
                    .HasForeignKey("id_usuario").OnDelete(DeleteBehavior.Restrict),
                relacion =>
                {
                    relacion.ToTable("UsuarioRol");
                    relacion.HasKey("id_usuario", "id_rol");
                });

        usuario.Navigation(u => u.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigurarLibro(ModelBuilder modelBuilder)
    {
        var libro = modelBuilder.Entity<Libro>();
        libro.ToTable("Libros");
        libro.HasKey(l => l.Id);
        libro.Property(l => l.Id).HasColumnName("id_libro");
        libro.Property(l => l.ISBN).HasColumnName("isbn").HasMaxLength(50).IsRequired();
        libro.Property(l => l.Titulo).HasColumnName("nombre_libro").HasMaxLength(255).IsRequired();
        libro.Property(l => l.Autor).HasColumnName("autor").HasMaxLength(150).IsRequired();
        libro.Property(l => l.Genero).HasColumnName("genero").HasMaxLength(100);
        libro.Property(l => l.Editorial).HasColumnName("editorial").HasMaxLength(100);
        libro.Property(l => l.Estado).HasColumnName("estado").HasMaxLength(50);
        libro.Ignore(l => l.FechaCreacion);
        libro.Ignore(l => l.FechaModificacion);
        libro.HasIndex(l => l.ISBN).IsUnique();
    }

    private static void ConfigurarSolicitudPrestamo(ModelBuilder modelBuilder)
    {
        var solicitud = modelBuilder.Entity<SolicitudPrestamo>();
        solicitud.ToTable("SolicitudPrestamo");
        solicitud.HasKey(s => s.Id);
        solicitud.Property(s => s.Id).HasColumnName("id_solicitud");
        solicitud.Property(s => s.UsuarioId).HasColumnName("id_usuario");
        solicitud.Property(s => s.LibroId).HasColumnName("id_libro");
        solicitud.Property(s => s.FechaSolicitud).HasColumnName("fecha_solicitud");
        solicitud.Property(s => s.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(50);
        solicitud.Property(s => s.MotivoRechazo).HasColumnName("motivo_rechazo").HasMaxLength(255);
        solicitud.Ignore(s => s.FechaCreacion);
        solicitud.Ignore(s => s.FechaModificacion);
        solicitud.HasOne<Usuario>().WithMany().HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        solicitud.HasOne<Libro>().WithMany().HasForeignKey(s => s.LibroId).OnDelete(DeleteBehavior.Restrict);
        solicitud.HasIndex(s => new { s.Estado, s.FechaSolicitud });
        solicitud.HasIndex(s => new { s.UsuarioId, s.FechaSolicitud });
        solicitud.HasIndex(s => new { s.LibroId, s.Estado });
    }

    private static void ConfigurarNotificacion(ModelBuilder modelBuilder)
    {
        var notificacion = modelBuilder.Entity<Notificacion>();
        notificacion.ToTable("Notificaciones");
        notificacion.HasKey(n => n.Id);
        notificacion.Property(n => n.Id).HasColumnName("id_notificacion");
        notificacion.Property(n => n.UsuarioId).HasColumnName("id_usuario");
        notificacion.Property(n => n.Mensaje).HasColumnName("mensaje").HasMaxLength(500).IsRequired();
        notificacion.Property(n => n.FechaEnvio).HasColumnName("fecha_envio");
        notificacion.Property(n => n.Leida).HasColumnName("leida");
        notificacion.Property(n => n.TipoEvento)
            .HasColumnName("tipo_evento")
            .HasConversion(
                tipo => tipo.ToString(),
                valor => ParsearTipoNotificacionPersistida(valor))
            .HasMaxLength(100)
            .IsRequired();
        notificacion.HasOne<Usuario>().WithMany().HasForeignKey(n => n.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        notificacion.HasIndex(n => new { n.UsuarioId, n.FechaEnvio });
        notificacion.HasIndex(n => new { n.UsuarioId, n.Leida });
    }

    private static TipoNotificacion ParsearTipoNotificacionPersistida(string valor)
    {
        if (Enum.TryParse<TipoNotificacion>(valor, true, out var tipo) &&
            Enum.IsDefined(tipo))
        {
            return tipo;
        }

        // El esquema inicial guardaba nombres funcionales como "Prestamo".
        // Se interpretan como información general para conservar compatibilidad
        // con los registros existentes sin requerir migraciones de base de datos.
        return TipoNotificacion.Informacion;
    }

    private static void ConfigurarCargo(ModelBuilder modelBuilder)
    {
        var cargo = modelBuilder.Entity<Cargo>();
        cargo.ToTable("Cargos");
        cargo.HasKey(c => c.Id);
        cargo.Property(c => c.Id).HasColumnName("id_cargo");
        cargo.Property(c => c.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        cargo.Property(c => c.FechaCreacion).HasColumnName("fecha_registro");
        cargo.Ignore(c => c.FechaModificacion);
        cargo.HasIndex(c => c.Nombre).IsUnique();
    }

    private static void ConfigurarRol(ModelBuilder modelBuilder)
    {
        var rol = modelBuilder.Entity<Rol>();
        rol.ToTable("Roles");
        rol.HasKey(r => r.Id);
        rol.Property(r => r.Id).HasColumnName("id_rol");
        rol.Property(r => r.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        rol.Property(r => r.Descripcion).HasColumnName("descripcion").HasMaxLength(500);
        rol.Property(r => r.FechaCreacion).HasColumnName("fecha_registro");
        rol.Ignore(r => r.FechaModificacion);
        rol.HasIndex(r => r.Nombre).IsUnique();
        rol.HasMany(r => r.Permisos)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "RolPermiso",
                relacion => relacion.HasOne<Permiso>().WithMany()
                    .HasForeignKey("id_permiso").OnDelete(DeleteBehavior.Restrict),
                relacion => relacion.HasOne<Rol>().WithMany()
                    .HasForeignKey("id_rol").OnDelete(DeleteBehavior.Restrict),
                relacion =>
                {
                    relacion.ToTable("RolPermiso");
                    relacion.HasKey("id_rol", "id_permiso");
                });
        rol.Navigation(r => r.Permisos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigurarPermiso(ModelBuilder modelBuilder)
    {
        var permiso = modelBuilder.Entity<Permiso>();
        permiso.ToTable("Permisos");
        permiso.HasKey(p => p.Id);
        permiso.Property(p => p.Id).HasColumnName("id_permiso");
        permiso.Property(p => p.Codigo).HasColumnName("codigo").HasMaxLength(100).IsRequired();
        permiso.Property(p => p.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        permiso.Ignore(p => p.FechaCreacion);
        permiso.Ignore(p => p.FechaModificacion);
        permiso.HasIndex(p => p.Codigo).IsUnique();
    }

    private static void ConfigurarEmpleado(ModelBuilder modelBuilder)
    {
        var empleado = modelBuilder.Entity<Empleado>();
        empleado.ToTable("Empleados");
        empleado.HasKey(e => e.Id);
        empleado.Ignore(e => e.FechaModificacion);
        empleado.Property(e => e.Id).HasColumnName("id_empleado");
        empleado.Property(e => e.UsuarioId).HasColumnName("id_usuario");
        empleado.Property(e => e.CargoId).HasColumnName("id_cargo");
        empleado.Property<string>("cargo").HasColumnName("cargo").HasMaxLength(100).IsRequired();
        empleado.Property(e => e.FechaCreacion).HasColumnName("fecha_registro");
        empleado.HasIndex(e => e.UsuarioId).IsUnique();
        empleado.HasOne(e => e.Usuario).WithOne().HasForeignKey<Empleado>(e => e.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        empleado.HasOne(e => e.Cargo).WithMany().HasForeignKey(e => e.CargoId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarAdministrador(ModelBuilder modelBuilder)
    {
        var administrador = modelBuilder.Entity<Administrador>();
        administrador.ToTable("Administradores");
        administrador.HasKey(a => a.Id);
        administrador.Ignore(a => a.FechaModificacion);
        administrador.Property(a => a.Id).HasColumnName("id_administrador");
        administrador.Property(a => a.UsuarioId).HasColumnName("id_usuario");
        administrador.Property(a => a.CargoId).HasColumnName("id_cargo");
        administrador.Property(a => a.FechaCreacion).HasColumnName("fecha_registro");
        administrador.HasIndex(a => a.UsuarioId).IsUnique();
        administrador.HasOne(a => a.Usuario).WithOne().HasForeignKey<Administrador>(a => a.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        administrador.HasOne(a => a.Cargo).WithMany().HasForeignKey(a => a.CargoId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarPrestamo(ModelBuilder modelBuilder)
    {
        var prestamo = modelBuilder.Entity<Prestamo>();
        prestamo.ToTable("Prestamos");
        prestamo.HasKey(p => p.Id);
        prestamo.Ignore(p => p.FechaModificacion);
        prestamo.Property(p => p.Id).HasColumnName("id_prestamo");
        prestamo.Property(p => p.UsuarioId).HasColumnName("id_usuario");
        prestamo.Property(p => p.LibroId).HasColumnName("id_libro");
        prestamo.Ignore(p => p.EjemplarId);
        prestamo.Property(p => p.SolicitudPrestamoId).HasColumnName("id_solicitud");
        prestamo.Property(p => p.EmpleadoPrestamoId).HasColumnName("id_empleado_prestamo");
        prestamo.Property(p => p.EmpleadoDevolucionId).HasColumnName("id_empleado_devolucion");
        prestamo.Property(p => p.FechaPrestamo).HasColumnName("fecha_prestamo");
        prestamo.Property(p => p.FechaEsperadaDevolucion).HasColumnName("fecha_devolucion_esperada");
        prestamo.Property(p => p.FechaRealDevolucion).HasColumnName("fecha_devolucion");
        prestamo.Property(p => p.Estado).HasColumnName("estado");
        prestamo.Ignore(p => p.FechaCreacion);
        prestamo.Property(p => p.Estado).HasConversion<string>().HasMaxLength(50);
        prestamo.HasIndex(p => p.SolicitudPrestamoId).IsUnique();
        prestamo.HasIndex(p => new { p.UsuarioId, p.Estado });
        prestamo.HasIndex(p => new { p.LibroId, p.Estado });
        prestamo.HasIndex(p => new { p.Estado, p.FechaEsperadaDevolucion });
        prestamo.HasOne<Usuario>().WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        prestamo.HasOne<Libro>().WithMany().HasForeignKey(p => p.LibroId).OnDelete(DeleteBehavior.Restrict);
        prestamo.HasOne<SolicitudPrestamo>().WithOne().HasForeignKey<Prestamo>(p => p.SolicitudPrestamoId).OnDelete(DeleteBehavior.Restrict);
        prestamo.HasOne<Empleado>().WithMany().HasForeignKey(p => p.EmpleadoPrestamoId).OnDelete(DeleteBehavior.Restrict);
        prestamo.HasOne<Empleado>().WithMany().HasForeignKey(p => p.EmpleadoDevolucionId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarPrestamoEjemplar(ModelBuilder modelBuilder)
    {
        var relacion = modelBuilder.Entity<PrestamoEjemplarRelacion>();
        relacion.ToTable("PrestamoEjemplar");
        relacion.HasKey(item => new { item.PrestamoId, item.EjemplarId });
        relacion.Property(item => item.PrestamoId).HasColumnName("id_prestamo");
        relacion.Property(item => item.EjemplarId).HasColumnName("id_ejemplar");
        relacion.Property(item => item.FechaAsignacion).HasColumnName("fecha_asignacion");
        relacion.HasOne(item => item.Prestamo)
            .WithMany()
            .HasForeignKey(item => item.PrestamoId)
            .OnDelete(DeleteBehavior.Cascade);
        relacion.HasOne<Ejemplar>()
            .WithMany()
            .HasForeignKey(item => item.EjemplarId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarMulta(ModelBuilder modelBuilder)
    {
        var multa = modelBuilder.Entity<Multa>();
        multa.ToTable("Multas");
        multa.HasKey(m => m.Id);
        multa.Property(m => m.Id).HasColumnName("id_multa");
        multa.Property(m => m.PrestamoId).HasColumnName("id_prestamo");
        multa.Property(m => m.UsuarioId).HasColumnName("id_usuario");
        multa.Property(m => m.Monto).HasColumnName("monto").HasPrecision(10, 2);
        multa.Property(m => m.Motivo).HasColumnName("motivo").HasMaxLength(255).IsRequired();
        multa.Property(m => m.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(50).IsRequired();
        multa.Property(m => m.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(50);
        multa.Property(m => m.FechaGeneracion).HasColumnName("fecha_generacion");
        multa.Property(m => m.FechaResolucion).HasColumnName("fecha_resolucion");
        multa.Property(m => m.EmpleadoResolucionId).HasColumnName("id_empleado_resuelve");
        multa.Property(m => m.ObservacionResolucion).HasColumnName("observacion_resolucion").HasMaxLength(255);
        multa.Ignore(m => m.FechaCreacion);
        multa.Ignore(m => m.FechaModificacion);
        multa.HasOne<Usuario>().WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        multa.HasOne<Prestamo>().WithMany().HasForeignKey(m => m.PrestamoId).OnDelete(DeleteBehavior.Restrict);
        multa.HasOne<Empleado>().WithMany().HasForeignKey(m => m.EmpleadoResolucionId).OnDelete(DeleteBehavior.Restrict);
        multa.HasIndex(m => new { m.UsuarioId, m.Estado });
        multa.HasIndex(m => new { m.Estado, m.FechaGeneracion });
    }

    private static void ConfigurarInventario(ModelBuilder modelBuilder)
    {
        var inventario = modelBuilder.Entity<Inventario>();
        inventario.ToTable("Inventario");
        inventario.HasKey(i => i.Id);
        inventario.Property(i => i.Id).HasColumnName("id_inventario");
        inventario.Property(i => i.LibroId).HasColumnName("id_libro");
        inventario.Property(i => i.CantidadTotal).HasColumnName("cantidad_total");
        inventario.Property(i => i.CantidadDisponible).HasColumnName("cantidad_disponible").IsConcurrencyToken();
        inventario.Property(i => i.CantidadPrestada).HasColumnName("cantidad_prestada").IsConcurrencyToken();
        inventario.Property(i => i.CantidadReservada).HasColumnName("cantidad_reservada").IsConcurrencyToken();
        inventario.Property(i => i.CantidadFueraServicio).HasColumnName("cantidad_fuera_servicio").IsConcurrencyToken();
        inventario.Property(i => i.CantidadPerdida).HasColumnName("cantidad_perdida").IsConcurrencyToken();
        inventario.Property(i => i.CantidadDanada).HasColumnName("cantidad_danada").IsConcurrencyToken();
        inventario.Ignore(i => i.TieneDisponibilidad);
        inventario.Ignore(i => i.FechaCreacion);
        inventario.Ignore(i => i.FechaModificacion);
        inventario.HasIndex(i => i.LibroId).IsUnique();
        inventario.HasOne<Libro>().WithOne().HasForeignKey<Inventario>(i => i.LibroId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarEjemplar(ModelBuilder modelBuilder)
    {
        var ejemplar = modelBuilder.Entity<Ejemplar>();
        ejemplar.ToTable("Ejemplares");
        ejemplar.HasKey(e => e.Id);
        ejemplar.Property(e => e.Id).HasColumnName("id_ejemplar");
        ejemplar.Property(e => e.LibroId).HasColumnName("id_libro");
        ejemplar.Property(e => e.Codigo).HasColumnName("codigo").HasMaxLength(80).IsRequired();
        ejemplar.Property(e => e.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(30).IsRequired();
        ejemplar.Property(e => e.FechaCreacion).HasColumnName("fecha_registro");
        ejemplar.Ignore(e => e.FechaModificacion);
        ejemplar.HasIndex(e => e.Codigo).IsUnique();
        ejemplar.HasIndex(e => new { e.LibroId, e.Estado });
        ejemplar.HasOne<Libro>().WithMany().HasForeignKey(e => e.LibroId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarAuditoria(ModelBuilder modelBuilder)
    {
        var auditoria = modelBuilder.Entity<Auditoria>();
        auditoria.ToTable("Auditoria");
        auditoria.HasKey(a => a.Id);
        auditoria.Property(a => a.Id).HasColumnName("id_auditoria");
        auditoria.Property(a => a.UsuarioResponsableId).HasColumnName("id_usuario");
        auditoria.Property(a => a.Modulo).HasColumnName("modulo").HasConversion<string>().HasMaxLength(100);
        auditoria.Property(a => a.Accion).HasColumnName("accion").HasConversion<string>().HasMaxLength(100);
        auditoria.Property(a => a.Descripcion).HasColumnName("descripcion").HasMaxLength(500).IsRequired();
        auditoria.Property(a => a.Resultado).HasColumnName("resultado").HasConversion<string>().HasMaxLength(50);
        auditoria.Property(a => a.Fecha).HasColumnName("fecha");
        auditoria.HasOne<Usuario>().WithMany().HasForeignKey(a => a.UsuarioResponsableId).OnDelete(DeleteBehavior.Restrict);
        auditoria.HasIndex(a => a.Fecha);
        auditoria.HasIndex(a => new { a.UsuarioResponsableId, a.Fecha });
        auditoria.HasIndex(a => new { a.Modulo, a.Fecha });
    }
}
