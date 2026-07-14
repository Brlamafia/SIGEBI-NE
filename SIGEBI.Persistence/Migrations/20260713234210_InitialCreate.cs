using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGEBI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cargo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Libro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Autor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ISBN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Genero = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Editorial = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Libro", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permiso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permiso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rol",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rol", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cedula = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TipoUsuario = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ejemplar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LibroId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ejemplar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ejemplar_Libro_LibroId",
                        column: x => x.LibroId,
                        principalTable: "Libro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Inventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LibroId = table.Column<int>(type: "int", nullable: false),
                    CantidadTotal = table.Column<int>(type: "int", nullable: false),
                    CantidadDisponible = table.Column<int>(type: "int", nullable: false),
                    CantidadPrestada = table.Column<int>(type: "int", nullable: false),
                    CantidadReservada = table.Column<int>(type: "int", nullable: false),
                    CantidadFueraServicio = table.Column<int>(type: "int", nullable: false),
                    CantidadPerdida = table.Column<int>(type: "int", nullable: false),
                    CantidadDanada = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventario", x => x.Id);
                    table.CheckConstraint("CK_Inventario_CantidadDanada", "[CantidadDanada] >= 0");
                    table.CheckConstraint("CK_Inventario_CantidadDisponible", "[CantidadDisponible] >= 0");
                    table.CheckConstraint("CK_Inventario_CantidadesConsistentes", "[CantidadTotal] = [CantidadDisponible] + [CantidadPrestada] + [CantidadReservada] + [CantidadFueraServicio] + [CantidadPerdida] + [CantidadDanada]");
                    table.CheckConstraint("CK_Inventario_CantidadFueraServicio", "[CantidadFueraServicio] >= 0");
                    table.CheckConstraint("CK_Inventario_CantidadPerdida", "[CantidadPerdida] >= 0");
                    table.CheckConstraint("CK_Inventario_CantidadPrestada", "[CantidadPrestada] >= 0");
                    table.CheckConstraint("CK_Inventario_CantidadReservada", "[CantidadReservada] >= 0");
                    table.CheckConstraint("CK_Inventario_CantidadTotal", "[CantidadTotal] >= 0");
                    table.ForeignKey(
                        name: "FK_Inventario_Libro_LibroId",
                        column: x => x.LibroId,
                        principalTable: "Libro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolPermiso",
                columns: table => new
                {
                    RolId = table.Column<int>(type: "int", nullable: false),
                    PermisoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolPermiso", x => new { x.RolId, x.PermisoId });
                    table.ForeignKey(
                        name: "FK_RolPermiso_Permiso_PermisoId",
                        column: x => x.PermisoId,
                        principalTable: "Permiso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolPermiso_Rol_RolId",
                        column: x => x.RolId,
                        principalTable: "Rol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Administrador",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CargoId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Administrador", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Administrador_Cargo_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Administrador_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Auditoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioResponsableId = table.Column<int>(type: "int", nullable: false),
                    Modulo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Auditoria_Usuario_UsuarioResponsableId",
                        column: x => x.UsuarioResponsableId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Empleado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CargoId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Empleado_Cargo_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Empleado_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notificacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Leida = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacion_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudPrestamo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    LibroId = table.Column<int>(type: "int", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudPrestamo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudPrestamo_Libro_LibroId",
                        column: x => x.LibroId,
                        principalTable: "Libro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudPrestamo_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioRol",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRol", x => new { x.UsuarioId, x.RolId });
                    table.ForeignKey(
                        name: "FK_UsuarioRol_Rol_RolId",
                        column: x => x.RolId,
                        principalTable: "Rol",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuarioRol_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prestamo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    LibroId = table.Column<int>(type: "int", nullable: false),
                    EjemplarId = table.Column<int>(type: "int", nullable: false),
                    SolicitudPrestamoId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoPrestamoId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoDevolucionId = table.Column<int>(type: "int", nullable: true),
                    FechaPrestamo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEsperadaDevolucion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRealDevolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prestamo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prestamo_Ejemplar_EjemplarId",
                        column: x => x.EjemplarId,
                        principalTable: "Ejemplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prestamo_Empleado_EmpleadoDevolucionId",
                        column: x => x.EmpleadoDevolucionId,
                        principalTable: "Empleado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prestamo_Empleado_EmpleadoPrestamoId",
                        column: x => x.EmpleadoPrestamoId,
                        principalTable: "Empleado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prestamo_Libro_LibroId",
                        column: x => x.LibroId,
                        principalTable: "Libro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prestamo_SolicitudPrestamo_SolicitudPrestamoId",
                        column: x => x.SolicitudPrestamoId,
                        principalTable: "SolicitudPrestamo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prestamo_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Multa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    PrestamoId = table.Column<int>(type: "int", nullable: true),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmpleadoResolucionId = table.Column<int>(type: "int", nullable: true),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ObservacionResolucion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Multa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Multa_Empleado_EmpleadoResolucionId",
                        column: x => x.EmpleadoResolucionId,
                        principalTable: "Empleado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Multa_Prestamo_PrestamoId",
                        column: x => x.PrestamoId,
                        principalTable: "Prestamo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Multa_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Administrador_CargoId",
                table: "Administrador",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_Administrador_UsuarioId",
                table: "Administrador",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Modulo_Fecha",
                table: "Auditoria",
                columns: new[] { "Modulo", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_UsuarioResponsableId_Fecha",
                table: "Auditoria",
                columns: new[] { "UsuarioResponsableId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Cargo_Nombre",
                table: "Cargo",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ejemplar_Codigo",
                table: "Ejemplar",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ejemplar_LibroId_Estado",
                table: "Ejemplar",
                columns: new[] { "LibroId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Empleado_CargoId",
                table: "Empleado",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleado_UsuarioId",
                table: "Empleado",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventario_LibroId",
                table: "Inventario",
                column: "LibroId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Libro_ISBN",
                table: "Libro",
                column: "ISBN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Libro_Titulo_Autor",
                table: "Libro",
                columns: new[] { "Titulo", "Autor" });

            migrationBuilder.CreateIndex(
                name: "IX_Multa_EmpleadoResolucionId",
                table: "Multa",
                column: "EmpleadoResolucionId");

            migrationBuilder.CreateIndex(
                name: "IX_Multa_PrestamoId_Tipo",
                table: "Multa",
                columns: new[] { "PrestamoId", "Tipo" },
                unique: true,
                filter: "[PrestamoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Multa_UsuarioId_Estado",
                table: "Multa",
                columns: new[] { "UsuarioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacion_UsuarioId_Leida_FechaEnvio",
                table: "Notificacion",
                columns: new[] { "UsuarioId", "Leida", "FechaEnvio" });

            migrationBuilder.CreateIndex(
                name: "IX_Permiso_Codigo",
                table: "Permiso",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prestamo_EjemplarId",
                table: "Prestamo",
                column: "EjemplarId");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamo_EmpleadoDevolucionId",
                table: "Prestamo",
                column: "EmpleadoDevolucionId");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamo_EmpleadoPrestamoId",
                table: "Prestamo",
                column: "EmpleadoPrestamoId");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamo_FechaEsperadaDevolucion",
                table: "Prestamo",
                column: "FechaEsperadaDevolucion");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamo_LibroId",
                table: "Prestamo",
                column: "LibroId");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamo_SolicitudPrestamoId",
                table: "Prestamo",
                column: "SolicitudPrestamoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prestamo_UsuarioId_Estado",
                table: "Prestamo",
                columns: new[] { "UsuarioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Rol_Nombre",
                table: "Rol",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolPermiso_PermisoId",
                table: "RolPermiso",
                column: "PermisoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudPrestamo_LibroId_Estado",
                table: "SolicitudPrestamo",
                columns: new[] { "LibroId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudPrestamo_UsuarioId_Estado",
                table: "SolicitudPrestamo",
                columns: new[] { "UsuarioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_Cedula",
                table: "Usuario",
                column: "Cedula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_Email",
                table: "Usuario",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRol_RolId",
                table: "UsuarioRol",
                column: "RolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Administrador");

            migrationBuilder.DropTable(
                name: "Auditoria");

            migrationBuilder.DropTable(
                name: "Inventario");

            migrationBuilder.DropTable(
                name: "Multa");

            migrationBuilder.DropTable(
                name: "Notificacion");

            migrationBuilder.DropTable(
                name: "RolPermiso");

            migrationBuilder.DropTable(
                name: "UsuarioRol");

            migrationBuilder.DropTable(
                name: "Prestamo");

            migrationBuilder.DropTable(
                name: "Permiso");

            migrationBuilder.DropTable(
                name: "Rol");

            migrationBuilder.DropTable(
                name: "Ejemplar");

            migrationBuilder.DropTable(
                name: "Empleado");

            migrationBuilder.DropTable(
                name: "SolicitudPrestamo");

            migrationBuilder.DropTable(
                name: "Cargo");

            migrationBuilder.DropTable(
                name: "Libro");

            migrationBuilder.DropTable(
                name: "Usuario");
        }
    }
}
