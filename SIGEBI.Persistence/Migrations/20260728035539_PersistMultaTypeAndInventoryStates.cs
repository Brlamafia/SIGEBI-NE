using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGEBI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistMultaTypeAndInventoryStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tipo",
                table: "Multas",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Otra");

            migrationBuilder.AddColumn<int>(
                name: "cantidad_danada",
                table: "Inventario",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "cantidad_fuera_servicio",
                table: "Inventario",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "cantidad_perdida",
                table: "Inventario",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "cantidad_reservada",
                table: "Inventario",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tipo",
                table: "Multas");

            migrationBuilder.DropColumn(
                name: "cantidad_danada",
                table: "Inventario");

            migrationBuilder.DropColumn(
                name: "cantidad_fuera_servicio",
                table: "Inventario");

            migrationBuilder.DropColumn(
                name: "cantidad_perdida",
                table: "Inventario");

            migrationBuilder.DropColumn(
                name: "cantidad_reservada",
                table: "Inventario");
        }
    }
}
