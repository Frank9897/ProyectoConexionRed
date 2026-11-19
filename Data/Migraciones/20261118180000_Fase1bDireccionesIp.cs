using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoConexionRed.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class Fase1bDireccionesIp : Migration
    {
        /// <summary>
        /// Crea la tabla de direcciones IP con su estado y su equipo asociado.
        /// </summary>
        /// <param name="migrationBuilder">Constructor de cambios; modificarlo altera el esquema resultante.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DireccionesIp",
                columns: table => new
                {
                    Identificador = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 45, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Libre"),
                    IdentificadorEquipo = table.Column<int>(type: "INTEGER", nullable: true),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    FechaActualizacionUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DireccionesIp", x => x.Identificador);
                    table.ForeignKey(
                        name: "FK_DireccionesIp_EquiposRed_IdentificadorEquipo",
                        column: x => x.IdentificadorEquipo,
                        principalTable: "EquiposRed",
                        principalColumn: "Identificador",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DireccionesIp_Direccion",
                table: "DireccionesIp",
                column: "Direccion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DireccionesIp_IdentificadorEquipo",
                table: "DireccionesIp",
                column: "IdentificadorEquipo");
        }

        /// <summary>
        /// Elimina la tabla de direcciones IP creada por esta migración.
        /// </summary>
        /// <param name="migrationBuilder">Constructor de cambios; modificarlo altera lo que se revierte.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DireccionesIp");
        }
    }
}
