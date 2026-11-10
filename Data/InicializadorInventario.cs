using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Models;
using ProyectoConexionRed.Web.Services;

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
            CREATE TABLE IF NOT EXISTS "ConexionesDocumentadas" (
                "Identificador" INTEGER NOT NULL CONSTRAINT "PK_ConexionesDocumentadas" PRIMARY KEY AUTOINCREMENT,
                "IdentificadorPuertoLocal" INTEGER NOT NULL,
                "EquipoRemoto" TEXT NULL,
                "IpRemota" TEXT NULL,
                "PuertoRemoto" TEXT NULL,
                "Medio" TEXT NOT NULL DEFAULT 'No confirmado',
                "EstadoDatos" TEXT NOT NULL DEFAULT 'A verificar',
                "FechaUltimaRevision" TEXT NULL,
                "FuentePagina" TEXT NOT NULL,
                "FuenteLibro" TEXT NULL,
                "FuenteFila" TEXT NULL,
                "Observaciones" TEXT NULL,
                CONSTRAINT "FK_ConexionesDocumentadas_InterfacesEquipo_IdentificadorPuertoLocal"
                    FOREIGN KEY ("IdentificadorPuertoLocal") REFERENCES "InterfacesEquipo" ("Identificador") ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ConexionesDocumentadas_IdentificadorPuertoLocal"
                ON "ConexionesDocumentadas" ("IdentificadorPuertoLocal");
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

        // AplicarRolesYTiposV3 corrige la función y el tipo de los switches según el Visio y evita la ficha duplicada de Mesa.
        await AplicarRolesYTiposV3Async(contextoInventario, cambiosAuditados, usuarioImportacion, cancelacion);

        // ImportarPuertosAreasV3 agrega solo posiciones ausentes de las trece tablas de área y conserva cada fuente.
        await ImportarPuertosAreasV3Async(contextoInventario, cambiosAuditados, usuarioImportacion, cancelacion);

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
    /// Importa puertos de área y destinos escritos en el V3 sin inferir tipos físicos ni extremos remotos.
    /// </summary>
    /// <param name="contextoInventario">Base documental; cambiarla determina dónde se agregan las posiciones.</param>
    /// <param name="cambiosAuditados">Eventos de auditoría; cambiar la lista modifica qué altas quedan registradas.</param>
    /// <param name="usuarioImportacion">Origen del alta; cambiarlo modifica el autor visible del historial.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene las consultas y escrituras.</param>
    private static async Task ImportarPuertosAreasV3Async(
        InventarioDbContext contextoInventario,
        List<EventoModificacion> cambiosAuditados,
        string usuarioImportacion,
        CancellationToken cancelacion)
    {
        // filasV3 contiene filas literales de las trece hojas de área seleccionadas; cambiar el catálogo altera la carga.
        var filasV3 = CatalogoPuertosAreasV3.ObtenerFilas();
        // equiposRegistrados permite mapear las etiquetas del catálogo a fichas existentes sin crear equipos remotos.
        var equiposRegistrados = await contextoInventario.Equipos.AsNoTracking().ToListAsync(cancelacion);
        // equiposPorNombre resuelve nombres canónicos ignorando mayúsculas; cambiarlo modifica la asignación de hojas.
        var equiposPorNombre = equiposRegistrados.ToDictionary(
            equipo => equipo.Nombre.Trim(), StringComparer.OrdinalIgnoreCase);
        // interfacesRegistradas permite conservar puertos que ya existen y evitar duplicados al reiniciar.
        var interfacesRegistradas = await contextoInventario.InterfacesEquipo.ToListAsync(cancelacion);
        // interfacesPorClave identifica una interfaz por switch y etiqueta local.
        var interfacesPorClave = interfacesRegistradas.ToDictionary(
            interfaz => (interfaz.IdentificadorEquipo, NormalizarEtiquetaPuertoV3(interfaz.EtiquetaPuerto)));
        // interfacesNuevas agrupa únicamente posiciones que aún no existen en SQLite.
        var interfacesNuevas = new List<InterfazEquipo>();

        foreach (var filaPuerto in filasV3)
        {
            // equipoLocal resuelve la ficha de switch declarada por la página y no crea un propietario supuesto.
            if (!equiposPorNombre.TryGetValue(filaPuerto.EquipoLocal.Trim(), out var equipoLocal))
                continue;
            // etiquetaPuertoNormalizada hace coincidir el número del puerto tras quitar espacios externos.
            var etiquetaPuertoNormalizada = NormalizarEtiquetaPuertoV3(filaPuerto.EtiquetaPuerto);
            // claveInterfaz mantiene juntas la ficha local y su etiqueta exacta.
            var claveInterfaz = (equipoLocal.Identificador, etiquetaPuertoNormalizada);
            if (interfacesPorClave.ContainsKey(claveInterfaz))
                continue;

            // filaLibre y filaDesconocida conservan estados explícitos sin tratarlos como conexión de usuario.
            var filaLibre = string.Equals(filaPuerto.Destino, "LIBRE", StringComparison.OrdinalIgnoreCase);
            var filaDesconocida = string.Equals(filaPuerto.Destino, "DESCONOCIDO", StringComparison.OrdinalIgnoreCase);
            // destinoEsSwitch reserva las relaciones entre switches para revisión de rack/enlace.
            var destinoEsSwitch = !filaLibre && !filaDesconocida && EsDestinoSwitchV3(filaPuerto.Destino);
            // estadoFuente documenta si el texto de la fila está vigente o requiere comprobar un uplink.
            var estadoFuente = destinoEsSwitch ? "A verificar" : "Confirmado";
            // observacionFuente mantiene referencia también para puertos libres o con destino desconocido.
            var observacionFuente = $"Fuente V3: {filaPuerto.Pagina} · {filaPuerto.Libro} · fila {filaPuerto.FilaFuente}; valor: {filaPuerto.Destino}.";
            // interfazNueva registra solo la etiqueta y el estado fuente; no presume RJ45, SFP ni velocidad.
            var interfazNueva = new InterfazEquipo
            {
                IdentificadorEquipo = equipoLocal.Identificador,
                EtiquetaPuerto = etiquetaPuertoNormalizada,
                TipoInterfaz = "No confirmado",
                EstadoDatos = estadoFuente,
                EstadoUso = filaLibre ? "Libre confirmado" : "Desconocido",
                Observaciones = observacionFuente
            };
            interfacesNuevas.Add(interfazNueva);
            interfacesPorClave.Add(claveInterfaz, interfazNueva);

            // eventoInterfaz deja rastro del puerto y su fila fuente sin asignar fecha de revisión no documentada.
            cambiosAuditados.Add(new EventoModificacion
            {
                IdentificadorEquipo = equipoLocal.Identificador,
                NombreEquipo = equipoLocal.Nombre,
                NombreUsuario = usuarioImportacion,
                FechaUtc = DateTime.UtcNow,
                Campo = "Puerto V3 incorporado",
                ValorNuevo = $"{etiquetaPuertoNormalizada} · {estadoFuente} · {filaPuerto.Pagina}/{filaPuerto.Libro}/fila {filaPuerto.FilaFuente}"
            });
        }

        if (interfacesNuevas.Count > 0)
        {
            // SaveChanges asigna IDs locales antes de crear las relaciones opcionales.
            await contextoInventario.InterfacesEquipo.AddRangeAsync(interfacesNuevas, cancelacion);
            await contextoInventario.SaveChangesAsync(cancelacion);
        }

        // enlacesExistentes carga extremos ya completos para no duplicar ni contradecir una relación guardada.
        var enlacesExistentes = await contextoInventario.EnlacesFisicos.AsNoTracking()
            .Select(enlace => new { enlace.IdentificadorPuertoOrigen, enlace.IdentificadorPuertoDestino })
            .ToListAsync(cancelacion);
        // puertosConEnlaceCompleto impide crear un segundo registro parcial en un extremo ya vinculado.
        var puertosConEnlaceCompleto = enlacesExistentes
            .SelectMany(enlace => new[] { enlace.IdentificadorPuertoOrigen, enlace.IdentificadorPuertoDestino })
            .ToHashSet();
        // relacionesExistentes evita repetir vínculos parciales cuando se reinicia la aplicación.
        var relacionesExistentes = await contextoInventario.ConexionesDocumentadas.AsNoTracking()
            .Select(relacion => relacion.IdentificadorPuertoLocal)
            .ToListAsync(cancelacion);
        // puertosConRelacionParcial reserva los extremos documentales que ya tienen fila remota opcional.
        var puertosConRelacionParcial = relacionesExistentes.ToHashSet();
        // relacionesNuevas agrupa solo datos de destino escritos en las tablas del Visio.
        var relacionesNuevas = new List<ConexionDocumentada>();

        foreach (var filaPuerto in filasV3)
        {
            // filaLibre y filaDesconocida no describen un equipo remoto y no generan una relación.
            if (string.Equals(filaPuerto.Destino, "LIBRE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(filaPuerto.Destino, "DESCONOCIDO", StringComparison.OrdinalIgnoreCase))
                continue;
            // equipoLocal resuelve la interfaz de origen sin buscar equipos por proximidad o nombre remoto.
            if (!equiposPorNombre.TryGetValue(filaPuerto.EquipoLocal.Trim(), out var equipoLocal))
                continue;
            // claveInterfaz encuentra la etiqueta concreta guardada para esta fila.
            var claveInterfaz = (equipoLocal.Identificador, NormalizarEtiquetaPuertoV3(filaPuerto.EtiquetaPuerto));
            if (!interfacesPorClave.TryGetValue(claveInterfaz, out var interfazLocal))
                continue;
            // interfazLocal.EstadoUso protege una corrección manual de puerto libre frente a una fuente antigua en conflicto.
            if (puertosConEnlaceCompleto.Contains(interfazLocal.Identificador)
                || puertosConRelacionParcial.Contains(interfazLocal.Identificador)
                || interfazLocal.EstadoUso == "Libre confirmado")
                continue;

            // ipRemota conserva el texto original salvo la corrección expresa del ProCurve confirmada por el usuario.
            var ipRemota = filaPuerto.IpRemota?.Trim();
            // observacionesRelacion registra la discrepancia de fuente cuando se aplica la IP canónica confirmada.
            string? observacionesRelacion = null;
            var esFilaProCurveCorregida = filaPuerto.EquipoLocal == "Puerta Agrimensura"
                && filaPuerto.EtiquetaPuerto == "1"
                && filaPuerto.Destino.Contains("ProCurve", StringComparison.OrdinalIgnoreCase)
                && ipRemota == "10.0.1.0";
            if (esFilaProCurveCorregida)
            {
                ipRemota = "10.0.1.1";
                observacionesRelacion = "La fila del Visio muestra 10.0.1.0; el usuario confirmó 10.0.1.1. El enlace queda a verificar.";
            }

            // destinoEsSwitch evita tratar uplinks pendientes como conexiones confirmadas a puestos.
            var destinoEsSwitch = EsDestinoSwitchV3(filaPuerto.Destino);
            // enlaceMesaConfirmado aplica la aclaración directa del usuario sobre el principal de Mesa de Entradas.
            var enlaceMesaConfirmado = filaPuerto.EquipoLocal == "Mesa de Entrada (No principal)"
                && filaPuerto.EtiquetaPuerto == "24"
                && filaPuerto.Destino.Contains("Mentrada", StringComparison.OrdinalIgnoreCase)
                && ipRemota == "192.168.1.1";
            // estadoRelacion confirma las relaciones de usuario y deja uplinks como pendientes, salvo la aclaración de Mesa.
            var estadoRelacion = destinoEsSwitch && !enlaceMesaConfirmado ? "A verificar" : "Confirmado";
            if (destinoEsSwitch && !enlaceMesaConfirmado && observacionesRelacion is null)
                observacionesRelacion = "La tabla identifica otro switch; verificar la vigencia física del enlace y su extremo remoto.";

            // nuevaRelacion conserva el extremo local exacto y deja sin completar el puerto remoto no documentado.
            var nuevaRelacion = new ConexionDocumentada
            {
                IdentificadorPuertoLocal = interfazLocal.Identificador,
                EquipoRemoto = filaPuerto.Destino.Trim(),
                IpRemota = ipRemota,
                PuertoRemoto = null,
                Medio = "No confirmado",
                EstadoDatos = estadoRelacion,
                FuentePagina = filaPuerto.Pagina,
                FuenteLibro = filaPuerto.Libro,
                FuenteFila = filaPuerto.FilaFuente,
                Observaciones = observacionesRelacion
            };
            relacionesNuevas.Add(nuevaRelacion);
            puertosConRelacionParcial.Add(interfazLocal.Identificador);

            // eventoRelacion registra el destino literal y su nivel de revisión desde la hoja origen.
            cambiosAuditados.Add(new EventoModificacion
            {
                IdentificadorEquipo = equipoLocal.Identificador,
                NombreEquipo = equipoLocal.Nombre,
                NombreUsuario = usuarioImportacion,
                FechaUtc = DateTime.UtcNow,
                Campo = "Conexión V3 incorporada",
                ValorNuevo = $"{interfazLocal.EtiquetaPuerto} → {nuevaRelacion.EquipoRemoto} · {nuevaRelacion.IpRemota ?? "IP no indicada"} · {estadoRelacion} · {filaPuerto.Pagina}/{filaPuerto.Libro}/fila {filaPuerto.FilaFuente}"
            });
        }

        if (relacionesNuevas.Count > 0)
            await contextoInventario.ConexionesDocumentadas.AddRangeAsync(relacionesNuevas, cancelacion);
    }

    /// <summary>
    /// Normaliza etiquetas de puerto numéricas conservando su representación y evitando diferencias por espacios.
    /// </summary>
    /// <param name="etiqueta">Etiqueta V3; cambiarla altera la clave con la que se busca la interfaz.</param>
    /// <returns>Etiqueta sin espacios externos.</returns>
    private static string NormalizarEtiquetaPuertoV3(string etiqueta)
    {
        // etiquetaNormalizada conserva los ceros y separadores del Visio y recorta solo espacios externos.
        var etiquetaNormalizada = etiqueta.Trim();
        return etiquetaNormalizada;
    }

    /// <summary>
    /// Identifica rótulos que nombran un switch para dejarlos pendientes de revisión física.
    /// </summary>
    /// <param name="destino">Rótulo remoto; cambiarlo puede clasificar una relación como uplink.</param>
    /// <returns>Verdadero cuando el texto hace referencia explícita a una familia o modelo de switch.</returns>
    private static bool EsDestinoSwitchV3(string destino)
    {
        // terminosSwitch reúne rótulos del Visio que identifican equipos de switching sin nombre uniforme.
        var terminosSwitch = new[] { "switch", "swtich", "procurve", "s4148", "1920", "3com", "aruba" };
        return terminosSwitch.Any(termino => destino.Contains(termino, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Corrige roles y tipos confirmados y consolida el registro de Mesa de Entradas principal.
    /// </summary>
    /// <param name="contextoInventario">Base documental; cambiarla aplica la corrección en otro inventario.</param>
    /// <param name="cambiosAuditados">Historial de cambios; cambiar la lista altera qué correcciones se registran.</param>
    /// <param name="usuarioImportacion">Origen de la corrección; cambiarlo modifica el autor del historial.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe las consultas y escrituras.</param>
    private static async Task AplicarRolesYTiposV3Async(
        InventarioDbContext contextoInventario,
        List<EventoModificacion> cambiosAuditados,
        string usuarioImportacion,
        CancellationToken cancelacion)
    {
        // equipoMesaPrincipal conserva una sola ficha canónica para el Aruba 1930 de IP 192.168.1.1.
        var equipoMesaPrincipal = await contextoInventario.Equipos
            .SingleOrDefaultAsync(equipo => equipo.Nombre == "Switch principal de Mesa de Entradas", cancelacion);
        // equipoMesaAnterior busca la ficha genérica que el catálogo inicial había creado para la misma IP.
        var equipoMesaAnterior = await contextoInventario.Equipos
            .SingleOrDefaultAsync(equipo => equipo.Nombre == "Switch de área sin identificar (1)", cancelacion);

        if (equipoMesaPrincipal is null && equipoMesaAnterior is not null)
        {
            // Renombrar la ficha con IP conocida evita duplicar el equipo y conserva su identificador.
            equipoMesaAnterior.Nombre = "Switch principal de Mesa de Entradas";
            equipoMesaPrincipal = equipoMesaAnterior;
            equipoMesaAnterior = null;
        }

        if (equipoMesaPrincipal is not null)
        {
            // ipAnterior conserva el valor para registrar la corrección de IP en auditoría.
            var ipAnterior = equipoMesaPrincipal.IpAdministracion;
            if (ipAnterior != "192.168.1.1")
            {
                equipoMesaPrincipal.IpAdministracion = "192.168.1.1";
                UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoMesaPrincipal.Identificador,
                    equipoMesaPrincipal.Nombre, usuarioImportacion, "IP de administración", ipAnterior,
                    equipoMesaPrincipal.IpAdministracion);
            }

            // fabricanteAnterior y modeloAnterior conservan los datos previos para no ocultar correcciones.
            var fabricanteAnterior = equipoMesaPrincipal.Fabricante;
            var modeloAnterior = equipoMesaPrincipal.Modelo;
            if (string.IsNullOrWhiteSpace(fabricanteAnterior)) equipoMesaPrincipal.Fabricante = "Aruba";
            if (string.IsNullOrWhiteSpace(modeloAnterior)) equipoMesaPrincipal.Modelo = "1930";
            UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoMesaPrincipal.Identificador,
                equipoMesaPrincipal.Nombre, usuarioImportacion, "Fabricante", fabricanteAnterior, equipoMesaPrincipal.Fabricante);
            UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoMesaPrincipal.Identificador,
                equipoMesaPrincipal.Nombre, usuarioImportacion, "Modelo", modeloAnterior, equipoMesaPrincipal.Modelo);

            // tipoAnterior identifica equipos que aún conservan la clasificación genérica del catálogo previo.
            var tipoAnterior = equipoMesaPrincipal.TipoDispositivo;
            if (tipoAnterior == "Equipo de red" || tipoAnterior == "No especificado")
                equipoMesaPrincipal.TipoDispositivo = "Switch";
            UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoMesaPrincipal.Identificador,
                equipoMesaPrincipal.Nombre, usuarioImportacion, "Tipo de dispositivo", tipoAnterior, equipoMesaPrincipal.TipoDispositivo);

            // rolAnterior conserva la etiqueta previa antes de aplicar el rol principal confirmado.
            var rolAnterior = equipoMesaPrincipal.RolEnArea;
            if (rolAnterior is "No indicado" or "No aplica" or "Secundario")
                equipoMesaPrincipal.RolEnArea = "Principal";
            UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoMesaPrincipal.Identificador,
                equipoMesaPrincipal.Nombre, usuarioImportacion, "Rol del equipo", rolAnterior, equipoMesaPrincipal.RolEnArea);

            // areaAnterior conserva la ubicación previa y estadoAnterior preserva el dato de acceso.
            var areaAnterior = equipoMesaPrincipal.Area;
            if (string.IsNullOrWhiteSpace(areaAnterior)) equipoMesaPrincipal.Area = "Mesa de Entradas";
            UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoMesaPrincipal.Identificador,
                equipoMesaPrincipal.Nombre, usuarioImportacion, "Área", areaAnterior, equipoMesaPrincipal.Area);
            var estadoAnterior = equipoMesaPrincipal.EstadoAdministrativo;
            if (estadoAnterior == "Sin información") equipoMesaPrincipal.EstadoAdministrativo = "Inaccesible: falta de credenciales";
            UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoMesaPrincipal.Identificador,
                equipoMesaPrincipal.Nombre, usuarioImportacion, "Acceso administrativo", estadoAnterior, equipoMesaPrincipal.EstadoAdministrativo);
        }

        if (equipoMesaAnterior is not null && equipoMesaPrincipal is not null
            && equipoMesaAnterior.Identificador != equipoMesaPrincipal.Identificador)
        {
            // interfacesDeFichaAnterior evita borrar una ficha si conserva puertos o enlaces documentados.
            var interfacesDeFichaAnterior = await contextoInventario.InterfacesEquipo
                .Where(interfaz => interfaz.IdentificadorEquipo == equipoMesaAnterior.Identificador)
                .ToListAsync(cancelacion);
            // etiquetasCanonicas evita colisiones del índice único al transferir interfaces al registro principal.
            var etiquetasCanonicas = await contextoInventario.InterfacesEquipo
                .Where(interfaz => interfaz.IdentificadorEquipo == equipoMesaPrincipal.Identificador)
                .Select(interfaz => interfaz.EtiquetaPuerto)
                .ToListAsync(cancelacion);
            // hayEtiquetasDuplicadas impide fusionar puertos con la misma etiqueta pero posiblemente distinta conexión.
            var hayEtiquetasDuplicadas = interfacesDeFichaAnterior.Any(interfaz =>
                etiquetasCanonicas.Contains(interfaz.EtiquetaPuerto, StringComparer.OrdinalIgnoreCase));

            if (!hayEtiquetasDuplicadas)
            {
                // Mover las interfaces mantiene sus IDs, por lo que los enlaces existentes conservan sus extremos.
                foreach (var interfazMovida in interfacesDeFichaAnterior)
                    interfazMovida.IdentificadorEquipo = equipoMesaPrincipal.Identificador;

                // eventoFusion conserva el motivo de la unificación aunque la ficha duplicada se quite del inventario.
                cambiosAuditados.Add(new EventoModificacion
                {
                    IdentificadorEquipo = equipoMesaPrincipal.Identificador,
                    NombreEquipo = equipoMesaPrincipal.Nombre,
                    NombreUsuario = usuarioImportacion,
                    FechaUtc = DateTime.UtcNow,
                    Campo = "Ficha duplicada unificada",
                    ValorAnterior = "Switch de área sin identificar (1)",
                    ValorNuevo = equipoMesaPrincipal.Nombre
                });
                contextoInventario.Equipos.Remove(equipoMesaAnterior);
            }
            else
            {
                // La ficha con etiquetas duplicadas queda separada y marcada para evitar unir puertos ambiguos.
                var nombreAnterior = equipoMesaAnterior.Nombre;
                equipoMesaAnterior.Nombre = "Registro previo de Mesa de Entradas (revisar)";
                equipoMesaAnterior.IpAdministracion = null;
                equipoMesaAnterior.TipoDispositivo = "No especificado";
                equipoMesaAnterior.RolEnArea = "No aplica";
                equipoMesaAnterior.EstadoDatos = "A verificar";
                equipoMesaAnterior.Observaciones = "Posible ficha duplicada del switch principal; conserva etiquetas de puerto que colisionan con la ficha canónica.";
                UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoMesaAnterior.Identificador,
                    equipoMesaAnterior.Nombre, usuarioImportacion, "Nombre anterior", nombreAnterior, equipoMesaAnterior.Nombre);
            }
        }

        // clasificacionesSwitches separa función y tipo de switch para no confundirlos con marca o modelo.
        var clasificacionesSwitches = CrearCatalogoRolesSwitchV3();
        foreach (var clasificacion in clasificacionesSwitches)
        {
            // equipoClasificado requiere nombre e IP para no aplicar el rol a otra ficha homónima.
            var equipoClasificado = await contextoInventario.Equipos.SingleOrDefaultAsync(
                equipo => equipo.Nombre == clasificacion.Nombre
                    && (clasificacion.IpAdministracion == null || equipo.IpAdministracion == clasificacion.IpAdministracion),
                cancelacion);
            if (equipoClasificado is null) continue;

            // tipoAnterior conserva la categoría documentada antes de identificar el equipo como switch.
            var tipoAnterior = equipoClasificado.TipoDispositivo;
            if (tipoAnterior is "Equipo de red" or "No especificado")
                equipoClasificado.TipoDispositivo = "Switch";
            UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoClasificado.Identificador,
                equipoClasificado.Nombre, usuarioImportacion, "Tipo de dispositivo", tipoAnterior, equipoClasificado.TipoDispositivo);

            // rolAnterior conserva la función previa antes de aplicar el tipo confirmado por el usuario.
            var rolAnterior = equipoClasificado.RolEnArea;
            if (string.IsNullOrWhiteSpace(rolAnterior) || rolAnterior is "No indicado" or "No aplica"
                || (clasificacion.Rol == "No principal" && rolAnterior == "Secundario"))
                equipoClasificado.RolEnArea = clasificacion.Rol;
            UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoClasificado.Identificador,
                equipoClasificado.Nombre, usuarioImportacion, "Rol del equipo", rolAnterior, equipoClasificado.RolEnArea);
        }
    }

    /// <summary>
    /// Devuelve tipos y roles operativos que el usuario confirmó para los switches del V3.
    /// </summary>
    /// <returns>Clasificaciones exactas; cambiar una fila altera la ficha que puede recibir ese rol.</returns>
    private static List<(string Nombre, string? IpAdministracion, string Rol)> CrearCatalogoRolesSwitchV3()
    {
        return
        [
            ("Switch HP 5500", "192.168.1.218", "Switch de servidores"),
            ("Switch HP 1920", "192.168.1.115", "Distribución de switches de área"),
            ("Dell S4148U-ON 48G", "192.168.1.44", "Puente hacia Dell R770 principal previsto"),
            ("Switch Agrimensura 1 HP", "192.168.1.23", "Switch de área"),
            ("Switch Agrimensura 3Com", "192.168.1.24", "Switch de área"),
            ("Asuntos Jurídicos", "192.168.1.30", "Switch de área"),
            ("Cartografía", "192.168.1.51", "Switch de área"),
            ("Inmuebles", "192.168.1.26", "Switch de área"),
            ("Mesa de Entrada (No principal)", "192.168.1.18", "No principal"),
            ("Puerta Agrimensura", "192.168.1.19", "Switch de área"),
            ("Regularización Dominial", "192.168.1.40", "Switch de área"),
            ("Secretaría", "192.168.1.28", "Switch de área"),
            ("Sistemas", "192.168.1.216", "Switch de área"),
            ("Valuación R0", "192.168.1.32", "Switch de área"),
            ("Valuación R1", "192.168.1.29", "Switch de área"),
            ("Valuación R2", "192.168.1.20", "Switch de área"),
            ("Switch de Régimen", null, "Switch de área"),
            ("Switch de área sin identificar (2)", "192.168.1.17", "Switch de área")
        ];
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
            ("Switch principal de Mesa de Entradas", "192.168.1.1", "Aruba"),
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
            new() { Nombre = "Switch HP 5500", Fabricante = "HP", Modelo = "5500", IpAdministracion = "192.168.1.218", Area = "Rack", TipoDispositivo = "Switch", RolEnArea = "Switch de servidores", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "Algunas conexiones del rack requieren revisión." },
            new() { Nombre = "Switch HP 1920", Fabricante = "HP", Modelo = "1920", IpAdministracion = "192.168.1.115", Area = "Rack", TipoDispositivo = "Switch", RolEnArea = "Distribución de switches de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "Algunas conexiones del rack requieren revisión." },
            new() { Nombre = "Dell S4148U-ON 48G", Fabricante = "Dell", Modelo = "S4148U-ON", IpAdministracion = "192.168.1.44", Area = "Rack", TipoDispositivo = "Switch", RolEnArea = "Puente hacia Dell R770 principal previsto", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "El Visio indica acceso SSH; algunas conexiones del rack requieren revisión." },
            new() { Nombre = "Switch HP 1920S", Fabricante = "HP", Modelo = "1920S", IpAdministracion = "192.168.1.217", Area = "Rack", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "Algunas conexiones del rack requieren revisión." },
            new() { Nombre = "Aruba 1930 (reserva)", Fabricante = "Aruba", Modelo = "Instant On 1930", IpAdministracion = "192.168.1.200", Area = "Reserva", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Cisco Catalyst 3560", Fabricante = "Cisco", Modelo = "Catalyst 3560", Area = "Rack", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "El Visio no muestra una IP para este registro." },
            new() { Nombre = "Switch Agrimensura 1 HP", Fabricante = "HP", IpAdministracion = "192.168.1.23", Area = "Agrimensura", TipoDispositivo = "Switch", RolEnArea = "Switch de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Switch Agrimensura 3Com", Fabricante = "3Com", IpAdministracion = "192.168.1.24", Area = "Agrimensura", TipoDispositivo = "Switch", RolEnArea = "Switch de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Asuntos Jurídicos", IpAdministracion = "192.168.1.30", Area = "Asuntos Jurídicos", TipoDispositivo = "Switch", RolEnArea = "Switch de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Cartografía", IpAdministracion = "192.168.1.51", Area = "Cartografía", TipoDispositivo = "Switch", RolEnArea = "Switch de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Inmuebles", IpAdministracion = "192.168.1.26", Area = "Inmuebles", TipoDispositivo = "Switch", RolEnArea = "Switch de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Mesa de Entrada (No principal)", IpAdministracion = "192.168.1.18", Area = "Mesa de Entradas", RolEnArea = "No principal", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "El usuario confirma su función de switch secundario; enlaza al principal del área y da conexión a tres equipos." },
            new() { Nombre = "Puerta Agrimensura", IpAdministracion = "192.168.1.19", Area = "Agrimensura", TipoDispositivo = "Switch", RolEnArea = "Switch de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Regularización Dominial", IpAdministracion = "192.168.1.40", Area = "Regularización Dominial", TipoDispositivo = "Switch", RolEnArea = "Switch de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Secretaría", IpAdministracion = "192.168.1.28", Area = "Secretaría", TipoDispositivo = "Switch", RolEnArea = "Switch de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Sistemas", IpAdministracion = "192.168.1.216", Area = "Sistemas", TipoDispositivo = "Switch", RolEnArea = "Switch de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Valuación R0", IpAdministracion = "192.168.1.32", Area = "Valuación", TipoDispositivo = "Switch", RolEnArea = "Switch de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Valuación R1", IpAdministracion = "192.168.1.29", Area = "Valuación", TipoDispositivo = "Switch", RolEnArea = "Switch de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Valuación R2", IpAdministracion = "192.168.1.20", Area = "Valuación", TipoDispositivo = "Switch", RolEnArea = "Switch de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Switch de Régimen", Area = "Régimen", TipoDispositivo = "Switch", RolEnArea = "Switch de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Inaccesible: falta de credenciales", Observaciones = "Equipo e inaccesibilidad confirmados por el usuario; IP no indicada en el registro base." },
            new() { Nombre = "Switch principal de Mesa de Entradas", Fabricante = "Aruba", Modelo = "1930", IpAdministracion = "192.168.1.1", Area = "Mesa de Entradas", TipoDispositivo = "Switch", RolEnArea = "Principal", EstadoDatos = "Vigente", EstadoAdministrativo = "Inaccesible: falta de credenciales", Observaciones = "Aruba 1930 principal del área; inaccesible por falta de credenciales según el usuario." },
            new() { Nombre = "Switch de área sin identificar (2)", IpAdministracion = "192.168.1.17", TipoDispositivo = "Switch", RolEnArea = "Switch de área", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" }
        ];
    }
}
