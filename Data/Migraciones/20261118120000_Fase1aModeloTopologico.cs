using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoConexionRed.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class Fase1aModeloTopologico : Migration
    {
        /// <summary>
        /// Agrega administrabilidad, velocidad, medio, modo y VLAN (valor 1), el estado STP de los enlaces y la tabla de posiciones del mapa.
        /// </summary>
        /// <param name="migrationBuilder">Constructor de cambios; modificarlo altera el esquema resultante.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsAdministrable",
                table: "EquiposRed",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstadoStp",
                table: "EnlacesFisicos",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "No documentado");

            migrationBuilder.AddColumn<string>(
                name: "MedioFisico",
                table: "InterfacesEquipo",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModoPuerto",
                table: "InterfacesEquipo",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Acceso");

            migrationBuilder.AddColumn<int>(
                name: "VelocidadMbps",
                table: "InterfacesEquipo",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Vlan",
                table: "InterfacesEquipo",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "PosicionesNodoMapa",
                columns: table => new
                {
                    Identificador = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdentificadorEquipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Vista = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    X = table.Column<double>(type: "REAL", nullable: false),
                    Y = table.Column<double>(type: "REAL", nullable: false),
                    FechaActualizacionUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NombreUsuario = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosicionesNodoMapa", x => x.Identificador);
                    table.ForeignKey(
                        name: "FK_PosicionesNodoMapa_EquiposRed_IdentificadorEquipo",
                        column: x => x.IdentificadorEquipo,
                        principalTable: "EquiposRed",
                        principalColumn: "Identificador",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PosicionesNodoMapa_IdentificadorEquipo_Vista",
                table: "PosicionesNodoMapa",
                columns: new[] { "IdentificadorEquipo", "Vista" },
                unique: true);
        }

        /// <summary>
        /// Revierte los campos y la tabla agregados por esta migración.
        /// </summary>
        /// <param name="migrationBuilder">Constructor de cambios; modificarlo altera lo que se revierte.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PosicionesNodoMapa");

            migrationBuilder.DropColumn(
                name: "EsAdministrable",
                table: "EquiposRed");

            migrationBuilder.DropColumn(
                name: "EstadoStp",
                table: "EnlacesFisicos");

            migrationBuilder.DropColumn(
                name: "MedioFisico",
                table: "InterfacesEquipo");

            migrationBuilder.DropColumn(
                name: "ModoPuerto",
                table: "InterfacesEquipo");

            migrationBuilder.DropColumn(
                name: "VelocidadMbps",
                table: "InterfacesEquipo");

            migrationBuilder.DropColumn(
                name: "Vlan",
                table: "InterfacesEquipo");
        }
    }
}
