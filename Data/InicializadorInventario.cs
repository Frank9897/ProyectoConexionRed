using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Data;

/// <summary>
/// Prepara SQLite y agrega registros iniciales sin duplicarlos.
/// </summary>
public static class InicializadorInventario
{
    /// <summary>
    /// Crea el esquema necesario, completa tablas de acceso si la base existía y carga equipo y administrador inicial.
    /// </summary>
    /// <param name="serviciosAplicacion">Proveedor de servicios; cambiarlo selecciona otra instancia configurada del inventario.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la inicialización pendiente.</param>
    public static async Task InicializarAsync(IServiceProvider serviciosAplicacion, CancellationToken cancelacion = default)
    {
        // alcanceServicios limita la vida del contexto utilizado durante la preparación inicial.
        using var alcanceServicios = serviciosAplicacion.CreateScope();

        // contextoInventario representa la conexión SQLite del servidor; cambiarla dirige la carga a otra base configurada.
        var contextoInventario = alcanceServicios.ServiceProvider.GetRequiredService<InventarioDbContext>();

        // Crear la estructura funciona para la base nueva; en bases existentes no agrega tablas faltantes.
        await contextoInventario.Database.EnsureCreatedAsync(cancelacion);

        // CrearTablasDeAccesoAsync permite conservar bases antiguas que ya tenían EquiposRed.
        await CrearTablasDeAccesoAsync(contextoInventario, cancelacion);

        // CargarEquiposInicialesAsync agrega el catálogo documental una sola vez.
        await CargarEquiposInicialesAsync(contextoInventario, cancelacion);

        // IncorporarDatosV3Async completa MAC confirmadas y agrega el equipo de reserva documentado sin duplicarlo.
        await IncorporarDatosV3Async(contextoInventario, cancelacion);

        // CargarAdministradorInicialAsync utiliza variables de entorno y nunca escribe la contraseña en los registros.
        await CargarAdministradorInicialAsync(
            contextoInventario,
            alcanceServicios.ServiceProvider.GetRequiredService<IPasswordHasher<UsuarioAplicacion>>(),
            cancelacion);
    }

    /// <summary>
    /// Garantiza que existan las tablas e índices de cuentas e historial incluso si EquiposRed ya estaba creada.
    /// </summary>
    /// <param name="contextoInventario">Contexto de la base; cambiarlo selecciona dónde se crean las tablas.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene las instrucciones SQL.</param>
    private static async Task CrearTablasDeAccesoAsync(
        InventarioDbContext contextoInventario,
        CancellationToken cancelacion)
    {
        // comandosSql crea solo tablas de acceso faltantes; cambiar estas sentencias altera su estructura persistida.
        const string comandosSql = """
            CREATE TABLE IF NOT EXISTS "UsuariosAplicacion" (
                "Identificador" INTEGER NOT NULL CONSTRAINT "PK_UsuariosAplicacion" PRIMARY KEY AUTOINCREMENT,
                "NombreUsuario" TEXT NOT NULL,
                "NombreUsuarioNormalizado" TEXT NOT NULL,
                "HashContrasena" TEXT NOT NULL,
                "Rol" TEXT NOT NULL DEFAULT 'Administrador',
                "Activo" INTEGER NOT NULL,
                "FechaCreacionUtc" TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_UsuariosAplicacion_NombreUsuarioNormalizado"
                ON "UsuariosAplicacion" ("NombreUsuarioNormalizado");
            CREATE TABLE IF NOT EXISTS "EventosAcceso" (
                "Identificador" INTEGER NOT NULL CONSTRAINT "PK_EventosAcceso" PRIMARY KEY AUTOINCREMENT,
                "NombreUsuario" TEXT NOT NULL,
                "FechaUtc" TEXT NOT NULL,
                "TipoEvento" TEXT NOT NULL,
                "Resultado" TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_EventosAcceso_FechaUtc"
                ON "EventosAcceso" ("FechaUtc");
            CREATE TABLE IF NOT EXISTS "EventosModificacion" (
                "Identificador" INTEGER NOT NULL CONSTRAINT "PK_EventosModificacion" PRIMARY KEY AUTOINCREMENT,
                "IdentificadorEquipo" INTEGER NOT NULL,
                "NombreEquipo" TEXT NOT NULL,
                "NombreUsuario" TEXT NOT NULL,
                "FechaUtc" TEXT NOT NULL,
                "Campo" TEXT NOT NULL,
                "ValorAnterior" TEXT NULL,
                "ValorNuevo" TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_EventosModificacion_FechaUtc"
                ON "EventosModificacion" ("FechaUtc");
            CREATE INDEX IF NOT EXISTS "IX_EventosModificacion_IdentificadorEquipo"
                ON "EventosModificacion" ("IdentificadorEquipo");
            CREATE TABLE IF NOT EXISTS "InterfacesEquipo" (
                "Identificador" INTEGER NOT NULL CONSTRAINT "PK_InterfacesEquipo" PRIMARY KEY AUTOINCREMENT,
                "IdentificadorEquipo" INTEGER NOT NULL,
                "EtiquetaPuerto" TEXT NOT NULL,
                "TipoInterfaz" TEXT NOT NULL,
                "EstadoDatos" TEXT NOT NULL,
                "EstadoUso" TEXT NOT NULL DEFAULT 'Desconocido',
                "FechaUltimaRevision" TEXT NULL,
                "Observaciones" TEXT NULL,
                CONSTRAINT "FK_InterfacesEquipo_EquiposRed_IdentificadorEquipo"
                    FOREIGN KEY ("IdentificadorEquipo") REFERENCES "EquiposRed" ("Identificador") ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_InterfacesEquipo_IdentificadorEquipo_EtiquetaPuerto"
                ON "InterfacesEquipo" ("IdentificadorEquipo", "EtiquetaPuerto");
            CREATE TABLE IF NOT EXISTS "EnlacesFisicos" (
                "Identificador" INTEGER NOT NULL CONSTRAINT "PK_EnlacesFisicos" PRIMARY KEY AUTOINCREMENT,
                "IdentificadorPuertoOrigen" INTEGER NOT NULL,
                "IdentificadorPuertoDestino" INTEGER NOT NULL,
                "Medio" TEXT NOT NULL,
                "ModuloOrigen" TEXT NULL,
                "ModuloDestino" TEXT NULL,
                "EstadoDatos" TEXT NOT NULL,
                "FechaUltimaRevision" TEXT NULL,
                "Fuente" TEXT NULL,
                "Observaciones" TEXT NULL,
                CONSTRAINT "FK_EnlacesFisicos_InterfacesEquipo_IdentificadorPuertoOrigen"
                    FOREIGN KEY ("IdentificadorPuertoOrigen") REFERENCES "InterfacesEquipo" ("Identificador") ON DELETE RESTRICT,
                CONSTRAINT "FK_EnlacesFisicos_InterfacesEquipo_IdentificadorPuertoDestino"
                    FOREIGN KEY ("IdentificadorPuertoDestino") REFERENCES "InterfacesEquipo" ("Identificador") ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS "IX_EnlacesFisicos_IdentificadorPuertoOrigen"
                ON "EnlacesFisicos" ("IdentificadorPuertoOrigen");
            CREATE INDEX IF NOT EXISTS "IX_EnlacesFisicos_IdentificadorPuertoDestino"
                ON "EnlacesFisicos" ("IdentificadorPuertoDestino");
            """;

        await contextoInventario.Database.ExecuteSqlRawAsync(comandosSql, cancelacion);
        await AgregarColumnaRolSiFaltaAsync(contextoInventario, cancelacion);
        await AgregarColumnaSiFaltaAsync(contextoInventario, "EquiposRed", "Piso", "TEXT NULL", cancelacion);
        await AgregarColumnaSiFaltaAsync(contextoInventario, "EquiposRed", "UsuarioAsignado", "TEXT NULL", cancelacion);
        await AgregarColumnaSiFaltaAsync(contextoInventario, "EquiposRed", "FuenteDatos", "TEXT NULL", cancelacion);
        await AgregarColumnaSiFaltaAsync(contextoInventario, "EquiposRed", "CantidadPuertosRj45", "INTEGER NULL", cancelacion);
        await AgregarColumnaSiFaltaAsync(contextoInventario, "EquiposRed", "CantidadPuertosSfp", "INTEGER NULL", cancelacion);
        await AgregarColumnaSiFaltaAsync(contextoInventario, "EquiposRed", "CantidadPuertosSfpPlus", "INTEGER NULL", cancelacion);
        await AgregarColumnaSiFaltaAsync(contextoInventario, "EquiposRed", "CantidadPuertosQsfp", "INTEGER NULL", cancelacion);
        await AgregarColumnaSiFaltaAsync(contextoInventario, "EquiposRed", "CantidadPuertosQsfpPlus", "INTEGER NULL", cancelacion);
        await AgregarColumnaSiFaltaAsync(contextoInventario, "EquiposRed", "CantidadPuertosQsfp28", "INTEGER NULL", cancelacion);
        await AgregarColumnaSiFaltaAsync(contextoInventario, "InterfacesEquipo", "EstadoUso", "TEXT NOT NULL DEFAULT 'Desconocido'", cancelacion);
    }

    /// <summary>
    /// Agrega una columna a una tabla permitida si todavía no existe en SQLite.
    /// </summary>
    /// <param name="contextoInventario">Contexto de base; cambiarlo aplica el cambio en otra conexión.</param>
    /// <param name="nombreTabla">Tabla destino; solo se aceptan nombres internos de inventario.</param>
    /// <param name="nombreColumna">Columna destino; cambiarla modifica el dato agregado.</param>
    /// <param name="tipoColumna">Definición SQLite; cambiarla altera tipo y nulabilidad.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la revisión o el cambio.</param>
    private static async Task AgregarColumnaSiFaltaAsync(InventarioDbContext contextoInventario,
        string nombreTabla, string nombreColumna, string tipoColumna, CancellationToken cancelacion)
    {
        // tablaPermitida limita la interpolación SQL a tablas internas conocidas.
        var tablaPermitida = nombreTabla == "EquiposRed" || nombreTabla == "InterfacesEquipo";
        if (!tablaPermitida) throw new InvalidOperationException("La tabla solicitada no está permitida para la actualización.");

        // conexionBase conserva la única conexión SQLite usada por el contexto.
        var conexionBase = contextoInventario.Database.GetDbConnection();
        // cerrarAlFinal conserva el estado original de la conexión al salir de la función.
        var cerrarAlFinal = conexionBase.State != ConnectionState.Open;
        if (cerrarAlFinal) await conexionBase.OpenAsync(cancelacion);

        try
        {
            // existeColumna evita aplicar dos veces la misma actualización.
            var existeColumna = false;
            await using (var comandoEsquema = conexionBase.CreateCommand())
            {
                comandoEsquema.CommandText = $"PRAGMA table_info(\"{nombreTabla}\")";
                await using var lectorEsquema = await comandoEsquema.ExecuteReaderAsync(cancelacion);
                while (await lectorEsquema.ReadAsync(cancelacion))
                {
                    // columnaRevisada conserva el nombre inspeccionado en el esquema SQLite.
                    var columnaRevisada = lectorEsquema.GetString(1);
                    if (columnaRevisada == nombreColumna)
                    {
                        existeColumna = true;
                        break;
                    }
                }
            }

            if (!existeColumna)
            {
                // Tabla y columna llegan desde llamadas internas con identificadores constantes.
                await contextoInventario.Database.ExecuteSqlRawAsync(
                    $"ALTER TABLE \"{nombreTabla}\" ADD COLUMN \"{nombreColumna}\" {tipoColumna}", cancelacion);
            }
        }
        finally
        {
            if (cerrarAlFinal) await conexionBase.CloseAsync();
        }
    }

    /// <summary>
    /// Agrega el rol a bases SQLite anteriores y conserva como administradoras las cuentas existentes.
    /// </summary>
    /// <param name="contextoInventario">Contexto de base; cambiarlo aplica el ajuste en otra base.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la lectura del esquema o la alteración.</param>
    private static async Task AgregarColumnaRolSiFaltaAsync(
        InventarioDbContext contextoInventario,
        CancellationToken cancelacion)
    {
        // conexionBase representa la conexión actual de EF; cambiarla inspecciona otra base.
        var conexionBase = contextoInventario.Database.GetDbConnection();
        // cerrarAlFinal indica si esta función abrió la conexión; cambiarlo altera si la conexión se cierra al terminar.
        var cerrarAlFinal = conexionBase.State != ConnectionState.Open;
        if (cerrarAlFinal)
        {
            await conexionBase.OpenAsync(cancelacion);
        }

        // existeColumnaRol evita repetir ALTER TABLE; cambiar su resultado decide si se amplía el esquema.
        var existeColumnaRol = false;
        await using (var comandoEsquema = conexionBase.CreateCommand())
        {
            comandoEsquema.CommandText = "PRAGMA table_info(\"UsuariosAplicacion\")";
            await using var lectorEsquema = await comandoEsquema.ExecuteReaderAsync(cancelacion);
            while (await lectorEsquema.ReadAsync(cancelacion))
            {
                // nombreColumna contiene el nombre leído del esquema; cambiar el índice cambia qué dato se compara.
                var nombreColumna = lectorEsquema.GetString(1);
                if (nombreColumna == "Rol")
                {
                    existeColumnaRol = true;
                    break;
                }
            }
        }

        if (!existeColumnaRol)
        {
            // ALTER TABLE asigna un rol administrador predeterminado a cuentas previas para conservar el acceso.
            await contextoInventario.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"UsuariosAplicacion\" ADD COLUMN \"Rol\" TEXT NOT NULL DEFAULT 'Administrador'",
                cancelacion);
        }

        if (cerrarAlFinal)
        {
            await conexionBase.CloseAsync();
        }
    }

    /// <summary>
    /// Carga equipos iniciales si no hay registros en la tabla de inventario.
    /// </summary>
    /// <param name="contextoInventario">Contexto de datos; cambiarlo selecciona qué inventario recibe el catálogo.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la consulta o escritura.</param>
    private static async Task CargarEquiposInicialesAsync(
        InventarioDbContext contextoInventario,
        CancellationToken cancelacion)
    {
        // inventarioExistente evita repetir los registros semilla; cambiar el criterio altera cuándo se carga el catálogo.
        var inventarioExistente = await contextoInventario.Equipos.AnyAsync(cancelacion);
        if (inventarioExistente)
        {
            return;
        }

        // equiposIniciales contiene dispositivos e IP confirmados, sin datos de credenciales.
        var equiposIniciales = CrearEquiposIniciales();

        // Guardar esta lista carga el inventario documental; no cambia ningún dispositivo de la red real.
        await contextoInventario.Equipos.AddRangeAsync(equiposIniciales, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);
    }

    /// <summary>
    /// Integra MAC confirmadas del V3 solo cuando la IP coincide y agrega el Aruba de reserva 2 si todavía no existe.
    /// </summary>
    /// <param name="contextoInventario">Contexto SQLite; cambiarlo determina qué inventario recibe la actualización documental.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla revierte la importación pendiente.</param>
    private static async Task IncorporarDatosV3Async(
        InventarioDbContext contextoInventario,
        CancellationToken cancelacion)
    {
        // datosMacConfirmados reúne pares nombre/IP/MAC transcritos de V3; cambiar una fila modifica el dato candidato de ese equipo.
        var datosMacConfirmados = CrearCatalogoMacV3();
        // cambiosAuditados reúne los datos que se agregarán al historial dentro de la misma transacción.
        var cambiosAuditados = new List<EventoModificacion>();
        // usuarioImportacion identifica el origen automático de la carga; cambiarlo modifica la atribución visible en auditoría.
        const string usuarioImportacion = "Carga documental DIAGRAMA RED V3.0";

        // transaccionImportacion asegura que los datos y su historial se confirmen o reviertan juntos.
        await using var transaccionImportacion = await contextoInventario.Database.BeginTransactionAsync(cancelacion);

        // datoMac recorre los pares documentados; cambiar el catálogo cambia qué MAC pueden completarse.
        foreach (var datoMac in datosMacConfirmados)
        {
            // equipoExistente busca la etiqueta exacta; cambiar el nombre consulta otro registro del inventario.
            var equipoExistente = await contextoInventario.Equipos
                .SingleOrDefaultAsync(equipo => equipo.Nombre == datoMac.Nombre, cancelacion);

            if (equipoExistente is null
                || !string.Equals(equipoExistente.IpAdministracion, datoMac.IpAdministracion, StringComparison.Ordinal)
                || !string.IsNullOrWhiteSpace(equipoExistente.DireccionMac))
            {
                continue;
            }

            // macAnterior conserva el valor previo para mostrar el antes en el historial.
            var macAnterior = equipoExistente.DireccionMac;
            equipoExistente.DireccionMac = datoMac.DireccionMac;

            // eventoMac registra solo una MAC incorporada desde la fuente; cambiar sus campos altera la línea de auditoría.
            cambiosAuditados.Add(new EventoModificacion
            {
                IdentificadorEquipo = equipoExistente.Identificador,
                NombreEquipo = equipoExistente.Nombre,
                NombreUsuario = usuarioImportacion,
                FechaUtc = DateTime.UtcNow,
                Campo = "Dirección MAC",
                ValorAnterior = macAnterior,
                ValorNuevo = datoMac.DireccionMac
            });
        }

        // datosFabricanteV3 reúne fabricantes explícitos; cambiar sus filas modifica qué fabricantes vacíos pueden completarse.
        var datosFabricanteV3 = CrearCatalogoFabricantesV3();

        // datoFabricante recorre etiquetas de fabricante identificadas en V3 sin asignar un modelo no indicado.
        foreach (var datoFabricante in datosFabricanteV3)
        {
            // equipoDeArea busca la etiqueta existente; cambiar el nombre selecciona otra ficha candidata.
            var equipoDeArea = await contextoInventario.Equipos
                .SingleOrDefaultAsync(equipo => equipo.Nombre == datoFabricante.Nombre, cancelacion);

            if (equipoDeArea is null
                || equipoDeArea.IpAdministracion != datoFabricante.IpAdministracion
                || !string.IsNullOrWhiteSpace(equipoDeArea.Fabricante))
            {
                continue;
            }

            // fabricanteAnterior conserva el campo vacío o previo para registrar el antes en auditoría.
            var fabricanteAnterior = equipoDeArea.Fabricante;
            equipoDeArea.Fabricante = datoFabricante.Fabricante;

            // eventoFabricante documenta el fabricante añadido desde V3 sin inferir el modelo.
            cambiosAuditados.Add(new EventoModificacion
            {
                IdentificadorEquipo = equipoDeArea.Identificador,
                NombreEquipo = equipoDeArea.Nombre,
                NombreUsuario = usuarioImportacion,
                FechaUtc = DateTime.UtcNow,
                Campo = "Fabricante",
                ValorAnterior = fabricanteAnterior,
                ValorNuevo = datoFabricante.Fabricante
            });
        }

        // equipoReservaDos identifica el equipo nuevo del V3; cambiar estos valores actualiza solo la ficha documental.
        var equipoReservaDos = await contextoInventario.Equipos
            .SingleOrDefaultAsync(equipo => equipo.Nombre == "Switch Aruba 1930 (Reserva 2)", cancelacion);

        if (equipoReservaDos is null)
        {
            equipoReservaDos = new EquipoRed
            {
                Nombre = "Switch Aruba 1930 (Reserva 2)",
                Fabricante = "Aruba",
                Modelo = "Aruba 1930",
                IpAdministracion = "192.168.1.201",
                DireccionMac = "94:60:D5:36:DF:4C",
                Area = "Reserva",
                RolEnArea = "Reserva",
                TipoDispositivo = "Switch",
                EstadoAdministrativo = "Sin información",
                EstadoDatos = "Vigente",
                Observaciones = "DIAGRAMA RED V3.0 indica cuatro puertos SFP+ de 10 Gbps; esta nota describe puertos del equipo y no confirma módulos instalados."
            };
            await contextoInventario.Equipos.AddAsync(equipoReservaDos, cancelacion);
            await contextoInventario.SaveChangesAsync(cancelacion);

            // eventoAltaReserva deja constancia del alta basada en V3 sin incluir ningún dato de credenciales.
            cambiosAuditados.Add(new EventoModificacion
            {
                IdentificadorEquipo = equipoReservaDos.Identificador,
                NombreEquipo = equipoReservaDos.Nombre,
                NombreUsuario = usuarioImportacion,
                FechaUtc = DateTime.UtcNow,
                Campo = "Alta de equipo",
                ValorAnterior = null,
                ValorNuevo = "IP 192.168.1.201; MAC 94:60:D5:36:DF:4C; Aruba 1930 de reserva"
            });
        }
        else if (equipoReservaDos.IpAdministracion == "192.168.1.201"
            && string.IsNullOrWhiteSpace(equipoReservaDos.DireccionMac))
        {
            // macAnteriorReserva conserva la MAC previa para documentar la incorporación desde la fuente.
            var macAnteriorReserva = equipoReservaDos.DireccionMac;
            equipoReservaDos.DireccionMac = "94:60:D5:36:DF:4C";

            // eventoMacReserva audita la MAC de Reserva 2 sin reemplazar valores ya cargados.
            cambiosAuditados.Add(new EventoModificacion
            {
                IdentificadorEquipo = equipoReservaDos.Identificador,
                NombreEquipo = equipoReservaDos.Nombre,
                NombreUsuario = usuarioImportacion,
                FechaUtc = DateTime.UtcNow,
                Campo = "Dirección MAC",
                ValorAnterior = macAnteriorReserva,
                ValorNuevo = equipoReservaDos.DireccionMac
            });
        }

        if (cambiosAuditados.Count > 0)
        {
            // Guardar en conjunto mantiene los eventos de la importación agrupados en la transacción.
            await contextoInventario.EventosModificacion.AddRangeAsync(cambiosAuditados, cancelacion);
            await contextoInventario.SaveChangesAsync(cancelacion);
        }

        await transaccionImportacion.CommitAsync(cancelacion);
    }

    /// <summary>
    /// Devuelve las MAC del V3 asociadas a sus etiquetas e IP explícitas.
    /// </summary>
    /// <returns>Pares documentados; modificar una fila altera qué ficha puede completarse si aún no tiene MAC.</returns>
    private static List<(string Nombre, string IpAdministracion, string DireccionMac)> CrearCatalogoMacV3()
    {
        return
        [
            ("Switch HP 5500", "192.168.1.218", "2C:23:3A:28:1C:25"),
            ("Switch HP 1920", "192.168.1.115", "40:B9:3C:2A:C0:7D"),
            ("Switch HP 1920S", "192.168.1.217", "20:67:7C:4E:9E:54"),
            ("Dell S4148U-ON 48G", "192.168.1.44", "E4:F0:04:A4:83:BD"),
            ("Switch Agrimensura 1 HP", "192.168.1.23", "5C:8A:38:9F:19:92"),
            ("Switch Agrimensura 3Com", "192.168.1.24", "00:24:73:CA:75:04"),
            ("Asuntos Jurídicos", "192.168.1.30", "78:48:59:72:6C:C5"),
            ("Cartografía", "192.168.1.51", "44:5B:ED:12:3E:25"),
            ("Inmuebles", "192.168.1.26", "B8:AF:67:9B:8E:DD"),
            ("Mesa de Entrada (No principal)", "192.168.1.18", "5C:8A:38:9F:1A:32"),
            ("Puerta Agrimensura", "192.168.1.19", "5C:8A:38:9E:80:52"),
            ("Regularización Dominial", "192.168.1.40", "B8:AF:67:9A:C5:9E"),
            ("Secretaría", "192.168.1.28", "40:B9:3C:2A:BF:7D"),
            ("Sistemas", "192.168.1.216", "20:67:7C:4C:44:A8"),
            ("Valuación R0", "192.168.1.32", "78:48:59:73:47:83"),
            ("Valuación R1", "192.168.1.29", "78:48:59:7C:E6:33"),
            ("Valuación R2", "192.168.1.20", "44:31:92:9E:54:20")
        ];
    }

    /// <summary>
    /// Devuelve fabricantes expresamente indicados junto a las IP de switches de área.
    /// </summary>
    /// <returns>Pares documentados; cambiar una fila modifica qué ficha puede recibir fabricante si aún está vacío.</returns>
    private static List<(string Nombre, string IpAdministracion, string Fabricante)> CrearCatalogoFabricantesV3()
    {
        return
        [
            ("Switch de área sin identificar (1)", "192.168.1.1", "Aruba"),
            ("Switch de área sin identificar (2)", "192.168.1.17", "Aruba")
        ];
    }

    /// <summary>
    /// Crea la cuenta administradora inicial solo cuando aún no hay cuentas y se configuraron ambas variables de entorno.
    /// </summary>
    /// <param name="contextoInventario">Contexto de usuarios; cambiarlo dirige la creación a otra base.</param>
    /// <param name="codificadorContrasenas">Servicio de hash; cambiarlo modifica el mecanismo de almacenamiento de contraseñas.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la consulta o creación.</param>
    private static async Task CargarAdministradorInicialAsync(
        InventarioDbContext contextoInventario,
        IPasswordHasher<UsuarioAplicacion> codificadorContrasenas,
        CancellationToken cancelacion)
    {
        // usuarioInicial contiene el nombre configurado fuera del repositorio; cambiarlo selecciona otra cuenta administradora.
        var usuarioInicial = Environment.GetEnvironmentVariable("INVENTARIO_ADMIN_USUARIO");

        // contrasenaInicial solo existe durante la creación; cambiarla modifica la clave verificada, que se guarda únicamente como hash.
        var contrasenaInicial = Environment.GetEnvironmentVariable("INVENTARIO_ADMIN_CLAVE");

        if (string.IsNullOrWhiteSpace(usuarioInicial) || string.IsNullOrWhiteSpace(contrasenaInicial))
        {
            return;
        }

        // nombreNormalizado permite una comparación estable; cambiarlo altera la clave única de búsqueda de la cuenta.
        var nombreNormalizado = usuarioInicial.Trim().ToUpperInvariant();

        // hayUsuarios evita reemplazar cuentas ya configuradas; cambiar el resultado habilita o detiene el alta inicial.
        var hayUsuarios = await contextoInventario.Usuarios.AnyAsync(cancelacion);
        if (hayUsuarios)
        {
            return;
        }

        // nuevoUsuario guarda el nombre y la clave derivada; cambiar sus campos altera la cuenta inicial creada.
        var nuevoUsuario = new UsuarioAplicacion
        {
            NombreUsuario = usuarioInicial.Trim(),
            NombreUsuarioNormalizado = nombreNormalizado,
            Rol = "Administrador",
            Activo = true,
            FechaCreacionUtc = DateTime.UtcNow
        };
        nuevoUsuario.HashContrasena = codificadorContrasenas.HashPassword(nuevoUsuario, contrasenaInicial);

        await contextoInventario.Usuarios.AddAsync(nuevoUsuario, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);
    }

    /// <summary>
    /// Devuelve los equipos iniciales sin inventar direcciones ni modelos ausentes.
    /// </summary>
    /// <returns>Lista de equipos documentales; cambiar sus valores modifica la carga inicial en bases vacías.</returns>
    private static List<EquipoRed> CrearEquiposIniciales()
    {
        return
        [
            new() { Nombre = "Switch Aruba 1930 (WDGC)", Fabricante = "Aruba", Modelo = "Instant On 1930", IpAdministracion = "192.168.1.19", Area = "Rack", TipoDispositivo = "Switch", RolEnArea = "Principal", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "El equipo y la IP están confirmados por el usuario; algunas conexiones del rack requieren revisión." },
            new() { Nombre = "HP ProCurve 29100I-24G", Fabricante = "HP", Modelo = "ProCurve 29100I-24G", IpAdministracion = "10.0.1.1", Area = "Rack", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "El Visio indica acceso SSH; algunas conexiones del rack requieren revisión." },
            new() { Nombre = "Switch HP 5500", Fabricante = "HP", Modelo = "5500", IpAdministracion = "192.168.1.218", Area = "Rack", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "Algunas conexiones del rack requieren revisión." },
            new() { Nombre = "Switch HP 1920", Fabricante = "HP", Modelo = "1920", IpAdministracion = "192.168.1.115", Area = "Rack", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "Algunas conexiones del rack requieren revisión." },
            new() { Nombre = "Dell S4148U-ON 48G", Fabricante = "Dell", Modelo = "S4148U-ON", IpAdministracion = "192.168.1.44", Area = "Rack", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "El Visio indica acceso SSH; algunas conexiones del rack requieren revisión." },
            new() { Nombre = "Switch HP 1920S", Fabricante = "HP", Modelo = "1920S", IpAdministracion = "192.168.1.217", Area = "Rack", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "Algunas conexiones del rack requieren revisión." },
            new() { Nombre = "Aruba 1930 (reserva)", Fabricante = "Aruba", Modelo = "Instant On 1930", IpAdministracion = "192.168.1.200", Area = "Reserva", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Cisco Catalyst 3560", Fabricante = "Cisco", Modelo = "Catalyst 3560", Area = "Rack", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "El Visio no muestra una IP para este registro." },
            new() { Nombre = "Switch Agrimensura 1 HP", Fabricante = "HP", IpAdministracion = "192.168.1.23", Area = "Agrimensura", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Switch Agrimensura 3Com", Fabricante = "3Com", IpAdministracion = "192.168.1.24", Area = "Agrimensura", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Asuntos Jurídicos", IpAdministracion = "192.168.1.30", Area = "Asuntos Jurídicos", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Cartografía", IpAdministracion = "192.168.1.51", Area = "Cartografía", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Inmuebles", IpAdministracion = "192.168.1.26", Area = "Inmuebles", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Mesa de Entrada (No principal)", IpAdministracion = "192.168.1.18", Area = "Mesa de Entradas", RolEnArea = "Secundario", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "El usuario confirma su función de switch secundario; enlaza al principal del área y da conexión a tres equipos." },
            new() { Nombre = "Puerta Agrimensura", IpAdministracion = "192.168.1.19", Area = "Agrimensura", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Regularización Dominial", IpAdministracion = "192.168.1.40", Area = "Regularización Dominial", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Secretaría", IpAdministracion = "192.168.1.28", Area = "Secretaría", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Sistemas", IpAdministracion = "192.168.1.216", Area = "Sistemas", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Valuación R0", IpAdministracion = "192.168.1.32", Area = "Valuación", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Valuación R1", IpAdministracion = "192.168.1.29", Area = "Valuación", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Valuación R2", IpAdministracion = "192.168.1.20", Area = "Valuación", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Switch de Régimen", Area = "Régimen", TipoDispositivo = "Switch", RolEnArea = "No indicado", EstadoDatos = "Vigente", EstadoAdministrativo = "Inaccesible: falta de credenciales", Observaciones = "Equipo e inaccesibilidad confirmados por el usuario; IP no indicada en el registro base." },
            new() { Nombre = "Switch principal de Mesa de Entradas", Area = "Mesa de Entradas", TipoDispositivo = "Switch", RolEnArea = "Principal", EstadoDatos = "Vigente", EstadoAdministrativo = "Inaccesible: falta de credenciales", Observaciones = "Equipo e inaccesibilidad confirmados por el usuario; IP no indicada en el registro base." },
            new() { Nombre = "Switch de área sin identificar (1)", IpAdministracion = "192.168.1.1", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Switch de área sin identificar (2)", IpAdministracion = "192.168.1.17", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" }
        ];
    }
}
