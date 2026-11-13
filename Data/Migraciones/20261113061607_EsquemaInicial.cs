using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoConexionRed.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class EsquemaInicial : Migration
    {
    /// <summary>
    /// Crea las tablas, índices, claves y valores predeterminados de un inventario nuevo.
    /// </summary>
    /// <param name="migrationBuilder">Constructor de cambios; modificarlo altera el esquema creado.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EquiposRed",
                columns: table => new
                {
                    Identificador = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Fabricante = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Modelo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    IpAdministracion = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    DireccionMac = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Area = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Piso = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    UsuarioAsignado = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    FuenteDatos = table.Column<string>(type: "TEXT", maxLength: 240, nullable: true),
                    RolEnArea = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    TipoDispositivo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CantidadPuertosRj45 = table.Column<int>(type: "INTEGER", nullable: true),
                    CantidadPuertosSfp = table.Column<int>(type: "INTEGER", nullable: true),
                    CantidadPuertosSfpPlus = table.Column<int>(type: "INTEGER", nullable: true),
                    CantidadPuertosQsfp = table.Column<int>(type: "INTEGER", nullable: true),
                    CantidadPuertosQsfpPlus = table.Column<int>(type: "INTEGER", nullable: true),
                    CantidadPuertosQsfp28 = table.Column<int>(type: "INTEGER", nullable: true),
                    EstadoAdministrativo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EstadoDatos = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    FechaUltimaRevision = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquiposRed", x => x.Identificador);
                });

            migrationBuilder.CreateTable(
                name: "EventosAcceso",
                columns: table => new
                {
                    Identificador = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreUsuario = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TipoEvento = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Resultado = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosAcceso", x => x.Identificador);
                });

            migrationBuilder.CreateTable(
                name: "EventosModificacion",
                columns: table => new
                {
                    Identificador = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdentificadorEquipo = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreEquipo = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    NombreUsuario = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Campo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ValorAnterior = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ValorNuevo = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosModificacion", x => x.Identificador);
                });

            migrationBuilder.CreateTable(
                name: "MantenimientosEjecutados",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    FechaUtc = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MantenimientosEjecutados", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosAplicacion",
                columns: table => new
                {
                    Identificador = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreUsuario = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NombreUsuarioNormalizado = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HashContrasena = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Rol = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false, defaultValue: "Administrador"),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosAplicacion", x => x.Identificador);
                });

            migrationBuilder.CreateTable(
                name: "InterfacesEquipo",
                columns: table => new
                {
                    Identificador = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdentificadorEquipo = table.Column<int>(type: "INTEGER", nullable: false),
                    EtiquetaPuerto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TipoInterfaz = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EstadoDatos = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    EstadoUso = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, defaultValue: "Desconocido"),
                    FechaUltimaRevision = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterfacesEquipo", x => x.Identificador);
                    table.ForeignKey(
                        name: "FK_InterfacesEquipo_EquiposRed_IdentificadorEquipo",
                        column: x => x.IdentificadorEquipo,
                        principalTable: "EquiposRed",
                        principalColumn: "Identificador",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConexionesDocumentadas",
                columns: table => new
                {
                    Identificador = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdentificadorPuertoLocal = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipoRemoto = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    IpRemota = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    PuertoRemoto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Medio = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false, defaultValue: "No confirmado"),
                    EstadoDatos = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false, defaultValue: "A verificar"),
                    FechaUltimaRevision = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    FuentePagina = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    FuenteLibro = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    FuenteFila = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConexionesDocumentadas", x => x.Identificador);
                    table.ForeignKey(
                        name: "FK_ConexionesDocumentadas_InterfacesEquipo_IdentificadorPuertoLocal",
                        column: x => x.IdentificadorPuertoLocal,
                        principalTable: "InterfacesEquipo",
                        principalColumn: "Identificador",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EnlacesFisicos",
                columns: table => new
                {
                    Identificador = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdentificadorPuertoOrigen = table.Column<int>(type: "INTEGER", nullable: false),
                    IdentificadorPuertoDestino = table.Column<int>(type: "INTEGER", nullable: false),
                    Medio = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    ModuloOrigen = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ModuloDestino = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    EstadoDatos = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    FechaUltimaRevision = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Fuente = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnlacesFisicos", x => x.Identificador);
                    table.ForeignKey(
                        name: "FK_EnlacesFisicos_InterfacesEquipo_IdentificadorPuertoDestino",
                        column: x => x.IdentificadorPuertoDestino,
                        principalTable: "InterfacesEquipo",
                        principalColumn: "Identificador",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnlacesFisicos_InterfacesEquipo_IdentificadorPuertoOrigen",
                        column: x => x.IdentificadorPuertoOrigen,
                        principalTable: "InterfacesEquipo",
                        principalColumn: "Identificador",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConexionesDocumentadas_IdentificadorPuertoLocal",
                table: "ConexionesDocumentadas",
                column: "IdentificadorPuertoLocal",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnlacesFisicos_IdentificadorPuertoDestino",
                table: "EnlacesFisicos",
                column: "IdentificadorPuertoDestino");

            migrationBuilder.CreateIndex(
                name: "IX_EnlacesFisicos_IdentificadorPuertoOrigen",
                table: "EnlacesFisicos",
                column: "IdentificadorPuertoOrigen");

            migrationBuilder.CreateIndex(
                name: "IX_EquiposRed_IpAdministracion",
                table: "EquiposRed",
                column: "IpAdministracion");

            migrationBuilder.CreateIndex(
                name: "IX_EquiposRed_Nombre",
                table: "EquiposRed",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_EventosAcceso_FechaUtc",
                table: "EventosAcceso",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EventosModificacion_FechaUtc",
                table: "EventosModificacion",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EventosModificacion_IdentificadorEquipo",
                table: "EventosModificacion",
                column: "IdentificadorEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_InterfacesEquipo_IdentificadorEquipo_EtiquetaPuerto",
                table: "InterfacesEquipo",
                columns: new[] { "IdentificadorEquipo", "EtiquetaPuerto" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosAplicacion_NombreUsuarioNormalizado",
                table: "UsuariosAplicacion",
                column: "NombreUsuarioNormalizado",
                unique: true);
        }

        /// <summary>
        /// Elimina el esquema inicial si se revierte esta migración.
        /// </summary>
        /// <param name="migrationBuilder">Constructor de cambios; modificarlo altera las tablas eliminadas.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConexionesDocumentadas");

            migrationBuilder.DropTable(
                name: "EnlacesFisicos");

            migrationBuilder.DropTable(
                name: "EventosAcceso");

            migrationBuilder.DropTable(
                name: "EventosModificacion");

            migrationBuilder.DropTable(
                name: "MantenimientosEjecutados");

            migrationBuilder.DropTable(
                name: "UsuariosAplicacion");

            migrationBuilder.DropTable(
                name: "InterfacesEquipo");

            migrationBuilder.DropTable(
                name: "EquiposRed");
        }
    }
}

