using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema_de_Gestion_de_Importaciones.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "barcos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombreBarco = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    escotilla1 = table.Column<int>(type: "int", nullable: true),
                    escotilla2 = table.Column<int>(type: "int", nullable: true),
                    escotilla3 = table.Column<int>(type: "int", nullable: true),
                    escotilla4 = table.Column<int>(type: "int", nullable: true),
                    escotilla5 = table.Column<int>(type: "int", nullable: true),
                    escotilla6 = table.Column<int>(type: "int", nullable: true),
                    escotilla7 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_barcos", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "bodegas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    bodega = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bodegas", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "empresas",
                columns: table => new
                {
                    id_empresa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombreempresa = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    estatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresas", x => x.id_empresa);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_creacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ultimo_acceso = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "importaciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    fechahorasystema = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    fechahora = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    idbarco = table.Column<int>(type: "int", nullable: false),
                    totalcargakilos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_importaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_importaciones_barcos_idbarco",
                        column: x => x.idbarco,
                        principalTable: "barcos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "movimientos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    fechahora = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    fechahorasystema = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    idimportacion = table.Column<int>(type: "int", nullable: false),
                    idempresa = table.Column<int>(type: "int", nullable: false),
                    tipotransaccion = table.Column<int>(type: "int", nullable: false),
                    cantidadcamiones = table.Column<int>(type: "int", nullable: false),
                    cantidadrequerida = table.Column<int>(type: "int", nullable: false),
                    cantidadentregada = table.Column<int>(type: "int", nullable: false),
                    placa = table.Column<int>(type: "int", nullable: false),
                    placaalterna = table.Column<int>(type: "int", nullable: false),
                    guia = table.Column<int>(type: "int", nullable: false),
                    guiaalterna = table.Column<int>(type: "int", nullable: false),
                    escotilla = table.Column<int>(type: "int", nullable: false),
                    totalcarga = table.Column<int>(type: "int", nullable: false),
                    bodega = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos", x => x.id);
                    table.ForeignKey(
                        name: "FK_movimientos_empresas_idempresa",
                        column: x => x.idempresa,
                        principalTable: "empresas",
                        principalColumn: "id_empresa",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_importaciones_idimportacion",
                        column: x => x.idimportacion,
                        principalTable: "importaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_importaciones_idbarco",
                table: "importaciones",
                column: "idbarco");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_idempresa",
                table: "movimientos",
                column: "idempresa");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_idimportacion",
                table: "movimientos",
                column: "idimportacion");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bodegas");

            migrationBuilder.DropTable(
                name: "movimientos");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "empresas");

            migrationBuilder.DropTable(
                name: "importaciones");

            migrationBuilder.DropTable(
                name: "barcos");
        }
    }
}
