#nullable enable
using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace ProyectoConexionRed.Web.Data;

/// <summary>
/// Valida bases existentes antes de registrarlas y después aplica las migraciones pendientes.
/// </summary>
public static class PreparadorMigraciones
{
    // nombreTablaHistorial identifica la tabla que EF Core consulta para saber qué cambios ya se aplicaron.
    private const string NombreTablaHistorial = "__EFMigrationsHistory";

    // versionEntityFramework coincide con la versión que generó el snapshot actual del modelo.
    private const string VersionEntityFramework = "10.0.0";

    /// <summary>
    /// Registra una línea base solo si el esquema previo coincide con el modelo y luego aplica migraciones pendientes.
    /// </summary>
    /// <param name="contextoInventario">Contexto SQLite; cambiarlo dirige la validación y las migraciones a otra base.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la validación o migración.</param>
    public static async Task AplicarAsync(InventarioDbContext contextoInventario, CancellationToken cancelacion = default)
    {
        // conexionInventario permite inspeccionar SQLite sin crear ni alterar tablas durante la validación.
        var conexionInventario = contextoInventario.Database.GetDbConnection();

        // conexionYaAbierta conserva el ciclo de vida que haya configurado quien llamó este método.
        var conexionYaAbierta = conexionInventario.State == ConnectionState.Open;
        if (!conexionYaAbierta)
        {
            await conexionInventario.OpenAsync(cancelacion);
        }

        try
        {
            // migracionesDisponibles define las versiones que esta aplicación sabe administrar.
            var migracionesDisponibles = contextoInventario.Database.GetMigrations().ToArray();
            if (migracionesDisponibles.Length == 0)
            {
                throw new InvalidOperationException("El proyecto no contiene migraciones para inicializar el inventario.");
            }

            // identificadorMigracionInicial identifica el esquema base que solo se registra si ya existe completo.
            var identificadorMigracionInicial = migracionesDisponibles[0];

            // hayTablaHistorial distingue una base marcada por EF Core de una base creada por la versión anterior.
            var hayTablaHistorial = await ExisteTablaAsync(conexionInventario, NombreTablaHistorial, null, cancelacion);

            // tablasInventario reúne los nombres administrados por EF y evita baselinar una SQLite parcialmente creada.
            var tablasInventario = contextoInventario.Model.GetEntityTypes()
                .Select(tipoEntidad => tipoEntidad.GetTableName())
                .Where(nombreTabla => nombreTabla is not null)
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // hayEsquemaPrevio detecta tablas de la aplicación que existían antes de incorporar migraciones.
            var hayEsquemaPrevio = false;
            foreach (var nombreTabla in tablasInventario)
            {
                if (await ExisteTablaAsync(conexionInventario, nombreTabla, null, cancelacion))
                {
                    hayEsquemaPrevio = true;
                    break;
                }
            }

            // migracionesRegistradas evita marcar como inicial un esquema que ya tiene historial de EF Core incompatible.
            var migracionesRegistradas = hayTablaHistorial
                ? await ObtenerMigracionesRegistradasAsync(conexionInventario, cancelacion)
                : new HashSet<string>(StringComparer.Ordinal);

            // migracionesDesconocidas detiene el arranque si la base fue creada por otra versión o aplicación.
            var migracionesDesconocidas = migracionesRegistradas.Except(migracionesDisponibles, StringComparer.Ordinal).ToArray();
            if (migracionesDesconocidas.Length > 0)
            {
                throw new InvalidOperationException(
                    $"La base contiene migraciones que este proyecto no reconoce: {string.Join(", ", migracionesDesconocidas)}.");
            }

            // faltaMigracionInicial detecta el caso habitual: tablas antiguas sin la marca de EF Core.
            var faltaMigracionInicial = !migracionesRegistradas.Contains(identificadorMigracionInicial);
            if (hayEsquemaPrevio && faltaMigracionInicial)
            {
                await ValidarEsquemaExistenteAsync(contextoInventario, conexionInventario, tablasInventario, cancelacion);
                await RegistrarLineaBaseAsync(
                    contextoInventario,
                    conexionInventario,
                    identificadorMigracionInicial,
                    tablasInventario,
                    cancelacion);
            }

            // ApplyAsync conserva el esquema previo validado y crea el esquema inicial únicamente en una base vacía.
            await contextoInventario.Database.MigrateAsync(cancelacion);
        }
        finally
        {
            if (!conexionYaAbierta)
            {
                await conexionInventario.CloseAsync();
            }
        }
    }

    /// <summary>
    /// Confirma que cada tabla, columna, índice y relación previa exista según el modelo actual.
    /// </summary>
    /// <param name="contextoInventario">Contexto que define el esquema esperado; cambiarlo cambia la validación.</param>
    /// <param name="conexionInventario">Conexión abierta a la base revisada; cambiarla inspecciona otro archivo SQLite.</param>
    /// <param name="tablasInventario">Tablas administradas; cambiar la lista cambia qué estructuras se exigen.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la inspección.</param>
    private static async Task ValidarEsquemaExistenteAsync(
        InventarioDbContext contextoInventario,
        DbConnection conexionInventario,
        IReadOnlyCollection<string> tablasInventario,
        CancellationToken cancelacion)
    {
        // entidadesInventario relaciona cada tabla física con las columnas y restricciones del modelo EF.
        var entidadesInventario = contextoInventario.Model.GetEntityTypes()
            .Where(tipoEntidad => tipoEntidad.GetTableName() is not null)
            .ToArray();

        foreach (var tipoEntidad in entidadesInventario)
        {
            // nombreTabla identifica la tabla física que se coteja en esta vuelta.
            var nombreTabla = tipoEntidad.GetTableName()!;

            // identificadorTabla permite obtener nombres de columnas según el proveedor relacional.
            var identificadorTabla = StoreObjectIdentifier.Table(nombreTabla, tipoEntidad.GetSchema());

            // columnasEsperadas contiene todas las propiedades que EF espera encontrar en la tabla.
            var columnasEsperadas = tipoEntidad.GetProperties()
                .Select(propiedad => propiedad.GetColumnName(identificadorTabla))
                .Where(nombreColumna => nombreColumna is not null)
                .Cast<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // columnasActuales guarda nulabilidad, valor por defecto y orden de clave de cada columna de SQLite.
            var columnasActuales = await ObtenerColumnasAsync(conexionInventario, nombreTabla, cancelacion);
            if (!columnasEsperadas.SetEquals(columnasActuales.Keys))
            {
                var columnasFaltantes = columnasEsperadas.Except(columnasActuales.Keys, StringComparer.OrdinalIgnoreCase);
                var columnasAdicionales = columnasActuales.Keys.Except(columnasEsperadas, StringComparer.OrdinalIgnoreCase);
                throw new InvalidOperationException(
                    $"No se registró la línea base: la tabla {nombreTabla} no coincide. " +
                    $"Faltan [{string.Join(", ", columnasFaltantes)}]; adicionales [{string.Join(", ", columnasAdicionales)}].");
            }

            foreach (var propiedad in tipoEntidad.GetProperties())
            {
                // nombreColumna conecta la propiedad de EF con el nombre real almacenado en SQLite.
                var nombreColumna = propiedad.GetColumnName(identificadorTabla);
                if (nombreColumna is null)
                {
                    continue;
                }

                // columnaActual contiene la forma física de la propiedad que se compara con EF.
                var columnaActual = columnasActuales[nombreColumna];
                if (columnaActual.EsObligatoria == propiedad.IsNullable)
                {
                    throw new InvalidOperationException(
                        $"No se registró la línea base: la nulabilidad de {nombreTabla}.{nombreColumna} no coincide.");
                }

                // valorPredeterminadoEsperado lee solo los DEFAULT explícitos, no convenciones de generación de claves.
                var valorPredeterminadoEsperado = propiedad.FindAnnotation("Relational:DefaultValue")?.Value;
                if (valorPredeterminadoEsperado is not null)
                {
                    // valorPredeterminadoActual normaliza las comillas que PRAGMA table_info devuelve para texto.
                    var valorPredeterminadoActual = NormalizarValorPredeterminado(columnaActual.ValorPredeterminado);

                    // valorPredeterminadoModelo representa el default con el formato invariable de .NET.
                    var valorPredeterminadoModelo = Convert.ToString(valorPredeterminadoEsperado, CultureInfo.InvariantCulture);
                    // Las bases históricas pueden carecer de DEFAULT; la aplicación asigna estos valores al guardar.
                    if (valorPredeterminadoActual is not null &&
                        !string.Equals(valorPredeterminadoActual, valorPredeterminadoModelo, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"No se registró la línea base: el valor predeterminado de {nombreTabla}.{nombreColumna} no coincide " +
                            $"(esperado '{valorPredeterminadoModelo}', encontrado '{valorPredeterminadoActual ?? "sin valor"}').");
                    }
                }
            }

            // indicesEsperados obtiene los índices nombrados que respaldan búsquedas y unicidad.
            var indicesEsperados = tipoEntidad.GetIndexes()
                .Select(indice => indice.GetDatabaseName())
                .Where(nombreIndice => nombreIndice is not null)
                .Cast<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // indicesActuales lee los índices definidos en la tabla, sin contar los internos de SQLite.
            var indicesActuales = await ObtenerIndicesAsync(conexionInventario, nombreTabla, cancelacion);
            if (!indicesEsperados.IsSubsetOf(indicesActuales))
            {
                var indicesFaltantes = indicesEsperados.Except(indicesActuales, StringComparer.OrdinalIgnoreCase);
                throw new InvalidOperationException(
                    $"No se registró la línea base: faltan índices en {nombreTabla}: {string.Join(", ", indicesFaltantes)}.");
            }
        }

        // clavesForaneasEsperadas agrupa las relaciones que el modelo exige entre tablas.
        var clavesForaneasEsperadas = ObtenerClavesForaneasEsperadas(entidadesInventario);

        // clavesForaneasActuales lee las relaciones físicas que SQLite conserva.
        var clavesForaneasActuales = await ObtenerClavesForaneasActualesAsync(conexionInventario, tablasInventario, cancelacion);
        if (!clavesForaneasEsperadas.SetEquals(clavesForaneasActuales))
        {
            throw new InvalidOperationException("No se registró la línea base: las relaciones entre tablas no coinciden con el modelo.");
        }

        // resultadoIntegridad confirma que SQLite puede leer todas sus páginas antes de marcar la base como migrada.
        await using var comandoIntegridad = conexionInventario.CreateCommand();
        comandoIntegridad.CommandText = "PRAGMA integrity_check;";
        var resultadoIntegridad = Convert.ToString(await comandoIntegridad.ExecuteScalarAsync(cancelacion), CultureInfo.InvariantCulture);
        if (!string.Equals(resultadoIntegridad, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"No se registró la línea base: SQLite informó {resultadoIntegridad}.");
        }

        // comandoClavesIntegridad detecta filas que ya apuntan a una clave foránea inexistente.
        await using var comandoClavesIntegridad = conexionInventario.CreateCommand();
        comandoClavesIntegridad.CommandText = "PRAGMA foreign_key_check;";
        await using var lectorClavesIntegridad = await comandoClavesIntegridad.ExecuteReaderAsync(cancelacion);
        if (lectorClavesIntegridad.HasRows)
        {
            throw new InvalidOperationException("No se registró la línea base: hay relaciones foráneas con registros inválidos.");
        }
    }

    /// <summary>
    /// Registra la migración inicial dentro de una transacción después de volver a comprobar que nadie la registró.
    /// </summary>
    /// <param name="contextoInventario">Contexto usado para la transacción; cambiarlo apunta a otra base.</param>
    /// <param name="conexionInventario">Conexión abierta de la base; cambiarla cambia dónde se guarda la marca.</param>
    /// <param name="identificadorMigracionInicial">ID de la migración base; cambiarlo altera qué esquema se declara presente.</param>
    /// <param name="tablasInventario">Tablas cuyo esquema ya fue cotejado; cambiar la lista altera la comprobación de carrera.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla revierte la transacción pendiente.</param>
    private static async Task RegistrarLineaBaseAsync(
        InventarioDbContext contextoInventario,
        DbConnection conexionInventario,
        string identificadorMigracionInicial,
        IReadOnlyCollection<string> tablasInventario,
        CancellationToken cancelacion)
    {
        // transaccionSerializada evita que dos arranques agreguen la misma marca simultáneamente.
        await using var transaccionSerializada = await contextoInventario.Database.BeginTransactionAsync(cancelacion);

        // hayTablaHistorial confirma el estado después de adquirir el bloqueo transaccional.
        var hayTablaHistorial = await ExisteTablaAsync(
            conexionInventario,
            NombreTablaHistorial,
            transaccionSerializada.GetDbTransaction(),
            cancelacion);

        // esquemaSiguePresente evita marcar una base que cambió entre la validación y la transacción.
        var esquemaSiguePresente = false;
        foreach (var nombreTabla in tablasInventario)
        {
            if (await ExisteTablaAsync(conexionInventario, nombreTabla, transaccionSerializada.GetDbTransaction(), cancelacion))
            {
                esquemaSiguePresente = true;
                break;
            }
        }

        if (esquemaSiguePresente)
        {
            if (!hayTablaHistorial)
            {
                // crearHistorial establece el formato esperado por EF; no ejecuta los CREATE de la migración inicial.
                await EjecutarSqlAsync(
                    conexionInventario,
                    transaccionSerializada.GetDbTransaction(),
                    """
                    CREATE TABLE "__EFMigrationsHistory" (
                        "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                        "ProductVersion" TEXT NOT NULL
                    );
                    """,
                    cancelacion);
            }

            // insertarMarca registra que el esquema cotejado ya representa la migración inicial.
            await using var insertarMarca = conexionInventario.CreateCommand();
            insertarMarca.Transaction = transaccionSerializada.GetDbTransaction();
            insertarMarca.CommandText = "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ($migracion, $version);";
            AgregarParametro(insertarMarca, "$migracion", identificadorMigracionInicial);
            AgregarParametro(insertarMarca, "$version", VersionEntityFramework);
            await insertarMarca.ExecuteNonQueryAsync(cancelacion);
        }

        await transaccionSerializada.CommitAsync(cancelacion);
    }

    /// <summary>
    /// Lee si SQLite contiene una tabla con el nombre especificado.
    /// </summary>
    /// <param name="conexionInventario">Conexión abierta; cambiarla consulta otra base.</param>
    /// <param name="nombreTabla">Nombre de tabla; cambiarlo cambia qué objeto se busca.</param>
    /// <param name="transaccionActiva">Transacción opcional; cambiarla altera el contexto de lectura.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la consulta.</param>
    /// <returns>Verdadero si existe la tabla; cambiar el resultado decide si se considera una base previa.</returns>
    private static async Task<bool> ExisteTablaAsync(
        DbConnection conexionInventario,
        string nombreTabla,
        DbTransaction? transaccionActiva,
        CancellationToken cancelacion)
    {
        // comandoTabla consulta el catálogo SQLite sin interpolar el nombre recibido.
        await using var comandoTabla = conexionInventario.CreateCommand();
        comandoTabla.Transaction = transaccionActiva;
        comandoTabla.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $nombre LIMIT 1;";
        AgregarParametro(comandoTabla, "$nombre", nombreTabla);
        return await comandoTabla.ExecuteScalarAsync(cancelacion) is not null;
    }

    /// <summary>
    /// Obtiene las migraciones que SQLite registra como ya aplicadas.
    /// </summary>
    /// <param name="conexionInventario">Conexión abierta; cambiarla consulta otro historial.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la lectura.</param>
    /// <returns>IDs registrados; cambiar sus valores altera qué operaciones EF considera pendientes.</returns>
    private static async Task<HashSet<string>> ObtenerMigracionesRegistradasAsync(
        DbConnection conexionInventario,
        CancellationToken cancelacion)
    {
        // migracionesRegistradas evita volver a aplicar una versión ya asentada.
        var migracionesRegistradas = new HashSet<string>(StringComparer.Ordinal);
        await using var comandoHistorial = conexionInventario.CreateCommand();
        comandoHistorial.CommandText = "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\";";
        await using var lectorHistorial = await comandoHistorial.ExecuteReaderAsync(cancelacion);
        while (await lectorHistorial.ReadAsync(cancelacion))
        {
            migracionesRegistradas.Add(lectorHistorial.GetString(0));
        }

        return migracionesRegistradas;
    }

    /// <summary>
    /// Devuelve las columnas y metadatos físicos de una tabla SQLite.
    /// </summary>
    /// <param name="conexionInventario">Conexión abierta; cambiarla inspecciona otro archivo.</param>
    /// <param name="nombreTabla">Tabla inspeccionada; cambiarla selecciona otro conjunto de columnas.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la consulta.</param>
    /// <returns>Mapa de columnas con nulabilidad y default; cambiarlo altera el cotejo previo.</returns>
    private static async Task<Dictionary<string, (bool EsObligatoria, string? ValorPredeterminado)>> ObtenerColumnasAsync(
        DbConnection conexionInventario,
        string nombreTabla,
        CancellationToken cancelacion)
    {
        // columnasActuales conserva metadatos por nombre para cotejar el esquema sin leer filas de inventario.
        var columnasActuales = new Dictionary<string, (bool, string?)>(StringComparer.OrdinalIgnoreCase);

        // nombreTablaSeguro escapa comillas dobles antes de formar la instrucción PRAGMA.
        var nombreTablaSeguro = nombreTabla.Replace("\"", "\"\"", StringComparison.Ordinal);
        await using var comandoColumnas = conexionInventario.CreateCommand();
        comandoColumnas.CommandText = $"PRAGMA table_info(\"{nombreTablaSeguro}\");";
        await using var lectorColumnas = await comandoColumnas.ExecuteReaderAsync(cancelacion);
        while (await lectorColumnas.ReadAsync(cancelacion))
        {
            // nombreColumna identifica el registro actual de PRAGMA.
            var nombreColumna = lectorColumnas.GetString(1);

            // esObligatoria expresa el indicador notnull de SQLite.
            var esObligatoria = lectorColumnas.GetInt32(3) == 1;

            // valorPredeterminado conserva el SQL literal declarado para la columna, si existe.
            var valorPredeterminado = lectorColumnas.IsDBNull(4) ? null : lectorColumnas.GetString(4);
            columnasActuales[nombreColumna] = (esObligatoria, valorPredeterminado);
        }

        return columnasActuales;
    }

    /// <summary>
    /// Devuelve los índices definidos por nombre para una tabla.
    /// </summary>
    /// <param name="conexionInventario">Conexión abierta; cambiarla consulta otro catálogo SQLite.</param>
    /// <param name="nombreTabla">Tabla revisada; cambiarla selecciona otro grupo de índices.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la consulta.</param>
    /// <returns>Nombres de índices no internos; cambiar el conjunto altera la validación de unicidad y búsqueda.</returns>
    private static async Task<HashSet<string>> ObtenerIndicesAsync(
        DbConnection conexionInventario,
        string nombreTabla,
        CancellationToken cancelacion)
    {
        // indicesActuales excluye los índices automáticos de claves primarias generados por SQLite.
        var indicesActuales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var comandoIndices = conexionInventario.CreateCommand();
        comandoIndices.CommandText = "SELECT name FROM sqlite_master WHERE type = 'index' AND tbl_name = $nombre AND name NOT LIKE 'sqlite_%';";
        AgregarParametro(comandoIndices, "$nombre", nombreTabla);
        await using var lectorIndices = await comandoIndices.ExecuteReaderAsync(cancelacion);
        while (await lectorIndices.ReadAsync(cancelacion))
        {
            indicesActuales.Add(lectorIndices.GetString(0));
        }

        return indicesActuales;
    }

    /// <summary>
    /// Obtiene las relaciones foráneas que el modelo declara para sus tablas.
    /// </summary>
    /// <param name="entidadesInventario">Entidades mapeadas; cambiar la lista altera las relaciones esperadas.</param>
    /// <returns>Firmas de tabla, columnas y acción de borrado; cambiar el conjunto cambia la comparación.</returns>
    private static HashSet<string> ObtenerClavesForaneasEsperadas(IEnumerable<IEntityType> entidadesInventario)
    {
        // clavesForaneasEsperadas guarda una firma estable por cada par de columnas relacionadas.
        var clavesForaneasEsperadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tipoEntidad in entidadesInventario)
        {
            // nombreTablaLocal identifica dónde se declara cada relación.
            var nombreTablaLocal = tipoEntidad.GetTableName()!;

            // identificadorTablaLocal traduce las propiedades locales a columnas SQLite.
            var identificadorTablaLocal = StoreObjectIdentifier.Table(nombreTablaLocal, tipoEntidad.GetSchema());

            foreach (var claveForanea in tipoEntidad.GetForeignKeys())
            {
                // nombreTablaRemota identifica la tabla de destino de la relación.
                var nombreTablaRemota = claveForanea.PrincipalEntityType.GetTableName()!;

                // identificadorTablaRemota traduce las claves principales a columnas SQLite.
                var identificadorTablaRemota = StoreObjectIdentifier.Table(
                    nombreTablaRemota,
                    claveForanea.PrincipalEntityType.GetSchema());

                for (var indiceColumna = 0; indiceColumna < claveForanea.Properties.Count; indiceColumna++)
                {
                    // columnaLocal identifica el lado que almacena la referencia.
                    var columnaLocal = claveForanea.Properties[indiceColumna].GetColumnName(identificadorTablaLocal)!;

                    // columnaRemota identifica la clave principal a la que apunta la columna local.
                    var columnaRemota = claveForanea.PrincipalKey.Properties[indiceColumna]
                        .GetColumnName(identificadorTablaRemota)!;

                    // accionBorrado expresa cómo se comporta la relación al borrar su destino.
                    var accionBorrado = ObtenerAccionBorrado(claveForanea.DeleteBehavior);
                    clavesForaneasEsperadas.Add($"{nombreTablaLocal}|{nombreTablaRemota}|{columnaLocal}|{columnaRemota}|{accionBorrado}");
                }
            }
        }

        return clavesForaneasEsperadas;
    }

    /// <summary>
    /// Lee las relaciones foráneas declaradas físicamente en las tablas SQLite.
    /// </summary>
    /// <param name="conexionInventario">Conexión abierta; cambiarla inspecciona otra base.</param>
    /// <param name="tablasInventario">Tablas que contienen las relaciones; cambiar la lista cambia el alcance.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la inspección.</param>
    /// <returns>Firmas físicas de relaciones; cambiar el conjunto altera el cotejo antes del baseline.</returns>
    private static async Task<HashSet<string>> ObtenerClavesForaneasActualesAsync(
        DbConnection conexionInventario,
        IEnumerable<string> tablasInventario,
        CancellationToken cancelacion)
    {
        // clavesForaneasActuales conserva las relaciones observadas en PRAGMA foreign_key_list.
        var clavesForaneasActuales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var nombreTabla in tablasInventario)
        {
            // nombreTablaSeguro escapa caracteres antes de formar el PRAGMA.
            var nombreTablaSeguro = nombreTabla.Replace("\"", "\"\"", StringComparison.Ordinal);
            await using var comandoClaves = conexionInventario.CreateCommand();
            comandoClaves.CommandText = $"PRAGMA foreign_key_list(\"{nombreTablaSeguro}\");";
            await using var lectorClaves = await comandoClaves.ExecuteReaderAsync(cancelacion);
            while (await lectorClaves.ReadAsync(cancelacion))
            {
                // tablaRemota identifica el destino indicado por SQLite.
                var tablaRemota = lectorClaves.GetString(2);

                // columnaLocal identifica la columna de origen indicada por SQLite.
                var columnaLocal = lectorClaves.GetString(3);

                // columnaRemota identifica la clave de destino indicada por SQLite.
                var columnaRemota = lectorClaves.GetString(4);

                // accionBorrado conserva la regla ON DELETE de la relación.
                var accionBorrado = lectorClaves.GetString(6).ToUpperInvariant();
                clavesForaneasActuales.Add($"{nombreTabla}|{tablaRemota}|{columnaLocal}|{columnaRemota}|{accionBorrado}");
            }
        }

        return clavesForaneasActuales;
    }

    /// <summary>
    /// Convierte la acción de borrado de EF Core al texto que usa SQLite.
    /// </summary>
    /// <param name="comportamientoBorrado">Regla del modelo; cambiarla altera el comportamiento esperado.</param>
    /// <returns>Texto SQL de la acción; cambiarlo altera el cotejo de claves foráneas.</returns>
    private static string ObtenerAccionBorrado(DeleteBehavior comportamientoBorrado)
    {
        return comportamientoBorrado switch
        {
            DeleteBehavior.Cascade => "CASCADE",
            DeleteBehavior.Restrict => "RESTRICT",
            DeleteBehavior.SetNull => "SET NULL",
            _ => "NO ACTION"
        };
    }

    /// <summary>
    /// Normaliza un valor default que SQLite entrega como literal SQL.
    /// </summary>
    /// <param name="valorPredeterminado">Literal almacenado; cambiarlo altera la comparación de defaults.</param>
    /// <returns>Valor sin comillas externas; cambiar el resultado altera la validación de la columna.</returns>
    private static string? NormalizarValorPredeterminado(string? valorPredeterminado)
    {
        // valorNormalizado elimina envolturas SQL simples sin modificar el contenido interno.
        var valorNormalizado = valorPredeterminado?.Trim();
        if (valorNormalizado is { Length: >= 2 } && valorNormalizado[0] == '\'' && valorNormalizado[^1] == '\'')
        {
            valorNormalizado = valorNormalizado[1..^1].Replace("''", "'", StringComparison.Ordinal);
        }

        return valorNormalizado;
    }

    /// <summary>
    /// Ejecuta SQL parametrizado en la conexión y transacción elegidas.
    /// </summary>
    /// <param name="conexionInventario">Conexión que ejecutará la sentencia; cambiarla modifica la base objetivo.</param>
    /// <param name="transaccionActiva">Transacción que agrupa el cambio; cambiarla altera su atomicidad.</param>
    /// <param name="sentenciaSql">Sentencia fija de esquema; cambiarla modifica las tablas afectadas.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la instrucción.</param>
    private static async Task EjecutarSqlAsync(
        DbConnection conexionInventario,
        DbTransaction transaccionActiva,
        string sentenciaSql,
        CancellationToken cancelacion)
    {
        // comandoSql ejecuta únicamente el SQL interno definido por esta clase.
        await using var comandoSql = conexionInventario.CreateCommand();
        comandoSql.Transaction = transaccionActiva;
        comandoSql.CommandText = sentenciaSql;
        await comandoSql.ExecuteNonQueryAsync(cancelacion);
    }

    /// <summary>
    /// Agrega un parámetro tipado a un comando sin concatenar datos variables al SQL.
    /// </summary>
    /// <param name="comandoSql">Comando que recibirá el parámetro; cambiarlo altera dónde se usa.</param>
    /// <param name="nombreParametro">Nombre enlazado al marcador SQL; cambiarlo modifica la sentencia asociada.</param>
    /// <param name="valorParametro">Valor seguro; cambiarlo altera el dato consultado o guardado.</param>
    private static void AgregarParametro(DbCommand comandoSql, string nombreParametro, string valorParametro)
    {
        // parametroSql enlaza un valor separado del texto SQL para evitar interpolación de datos.
        var parametroSql = comandoSql.CreateParameter();
        parametroSql.ParameterName = nombreParametro;
        parametroSql.Value = valorParametro;
        comandoSql.Parameters.Add(parametroSql);
    }
}
