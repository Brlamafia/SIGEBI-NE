using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGEBI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SincronizacionFinalDelModelo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrestamoEjemplar",
                columns: table => new
                {
                    id_prestamo = table.Column<int>(type: "integer", nullable: false),
                    id_ejemplar = table.Column<int>(type: "integer", nullable: false),
                    fecha_asignacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrestamoEjemplar", x => new { x.id_prestamo, x.id_ejemplar });
                    table.ForeignKey(
                        name: "FK_PrestamoEjemplar_Ejemplares_id_ejemplar",
                        column: x => x.id_ejemplar,
                        principalTable: "Ejemplares",
                        principalColumn: "id_ejemplar",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrestamoEjemplar_Prestamos_id_prestamo",
                        column: x => x.id_prestamo,
                        principalTable: "Prestamos",
                        principalColumn: "id_prestamo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrestamoEjemplar_id_ejemplar",
                table: "PrestamoEjemplar",
                column: "id_ejemplar");

            migrationBuilder.Sql(
                """
                INSERT INTO "PrestamoEjemplar"
                    (id_prestamo, id_ejemplar, fecha_asignacion)
                SELECT id_prestamo, id_ejemplar, fecha_registro
                FROM "Prestamos";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Prestamos_Ejemplares_id_ejemplar",
                table: "Prestamos");

            migrationBuilder.DropIndex(
                name: "IX_Prestamos_id_ejemplar",
                table: "Prestamos");

            migrationBuilder.DropColumn(
                name: "fecha_registro",
                table: "Prestamos");

            migrationBuilder.DropColumn(
                name: "id_ejemplar",
                table: "Prestamos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_registro",
                table: "Prestamos",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "id_ejemplar",
                table: "Prestamos",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Prestamos" AS prestamo
                SET id_ejemplar = relacion.id_ejemplar,
                    fecha_registro = relacion.fecha_asignacion
                FROM (
                    SELECT DISTINCT ON (id_prestamo)
                        id_prestamo,
                        id_ejemplar,
                        fecha_asignacion
                    FROM "PrestamoEjemplar"
                    ORDER BY id_prestamo, fecha_asignacion
                ) AS relacion
                WHERE prestamo.id_prestamo = relacion.id_prestamo;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "id_ejemplar",
                table: "Prestamos",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_id_ejemplar",
                table: "Prestamos",
                column: "id_ejemplar");

            migrationBuilder.AddForeignKey(
                name: "FK_Prestamos_Ejemplares_id_ejemplar",
                table: "Prestamos",
                column: "id_ejemplar",
                principalTable: "Ejemplares",
                principalColumn: "id_ejemplar",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(
                name: "PrestamoEjemplar");
        }
    }
}
