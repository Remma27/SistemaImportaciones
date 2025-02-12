using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema_de_Gestion_de_Importaciones.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBodega : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_bodegas",
                table: "bodegas");

            migrationBuilder.RenameTable(
                name: "bodegas",
                newName: "empresa_bodegas");

            migrationBuilder.AlterColumn<double>(
                name: "totalcargakilos",
                table: "importaciones",
                type: "double",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK_empresa_bodegas",
                table: "empresa_bodegas",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_empresa_bodegas",
                table: "empresa_bodegas");

            migrationBuilder.RenameTable(
                name: "empresa_bodegas",
                newName: "bodegas");

            migrationBuilder.AlterColumn<int>(
                name: "totalcargakilos",
                table: "importaciones",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double");

            migrationBuilder.AddPrimaryKey(
                name: "PK_bodegas",
                table: "bodegas",
                column: "id");
        }
    }
}
