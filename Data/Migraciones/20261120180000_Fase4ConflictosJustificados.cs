using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoConexionRed.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class Fase4ConflictosJustificados : Migration
    {
        /// <summary>
        /// Crea la tabla de conflictos justificados por el técnico.
        /// </summary>
        /// <param name="migrationBuilder">Constructor de cambios; modificarlo altera el esquema resultante.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConflictosJustificados",
                columns: table => new
                {
                    Identificador = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Clave = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Detalle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NombreUsuario = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConflictosJustificados", x => x.Identificador);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConflictosJustificados_Tipo_Clave",
                table: "ConflictosJustificados",
                columns: new[] { "Tipo", "Clave" },
                unique: true);
        }

        /// <summary>
        /// Elimina la tabla creada por esta migración.
        /// </summary>
        /// <param name="migrationBuilder">Constructor de cambios; modificarlo altera lo que se revierte.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConflictosJustificados");
        }
    }
}
