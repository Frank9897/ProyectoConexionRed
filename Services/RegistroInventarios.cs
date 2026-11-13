using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Services;

/// <summary>
/// Describe un inventario disponible para seleccionar en el sitio.
/// </summary>
public sealed record FichaInventario
{
    /// <summary>Identificador de archivo; cambiarlo cambia la ruta que selecciona el servidor.</summary>
    public string Identificador { get; }

    /// <summary>Nombre mostrado; cambiarlo solo altera la etiqueta de la interfaz.</summary>
    public string Nombre { get; }

    /// <summary>Indica si se trata de la base preexistente; cambiarlo modifica las opciones de gestión disponibles.</summary>
    public bool EsPrincipal { get; }

    /// <summary>Ruta resuelta del archivo; cambiarla dirige las consultas a otra base SQLite.</summary>
    public string Ruta { get; }

    /// <summary>
    /// Crea una ficha de inventario disponible.
    /// </summary>
    /// <param name="identificador">Nombre seguro de base; modificarlo cambia la selección que abre el sitio.</param>
    /// <param name="nombre">Nombre visible; modificarlo cambia la etiqueta mostrada al usuario.</param>
    /// <param name="esPrincipal">Marca la base existente; cambiarla altera su tratamiento como base protegida.</param>
    /// <param name="ruta">Archivo SQLite; cambiarlo dirige las solicitudes a otra base.</param>
    public FichaInventario(string identificador, string nombre, bool esPrincipal, string ruta)
    {
        Identificador = identificador;
        Nombre = nombre;
        EsPrincipal = esPrincipal;
        Ruta = ruta;
    }
}

/// <summary>
/// Descubre y crea archivos SQLite independientes sin guardar inventario de equipos en catálogos externos.
/// </summary>
public sealed class RegistroInventarios
{
    /// <summary>Nombre de la cookie de preferencia; modificarlo cambia qué inventario se conserva entre visitas.</summary>
    public const string NombreCookieInventario = "ProyectoConexionRed.Inventario";

    // entornoAplicacion resuelve rutas bajo la carpeta de publicación; cambiarlo cambia dónde se guardan nuevas sedes.
    private readonly IWebHostEnvironment entornoAplicacion;

    // configuracionAplicacion aporta el nombre y la cadena del inventario actual; cambiarla apunta a otra base principal.
    private readonly IConfiguration configuracionAplicacion;

    // bloqueoCreacion serializa solicitudes; cambiar su alcance puede permitir superar el límite de tres bases.
    private readonly SemaphoreSlim bloqueoCreacion = new(1, 1);

    /// <summary>
    /// Crea el registro de inventarios a partir del entorno y configuración del sitio.
    /// </summary>
    /// <param name="entornoAplicacion">Entorno de ASP.NET; cambiarlo cambia la carpeta raíz de los archivos.</param>
    /// <param name="configuracionAplicacion">Configuración de bases; cambiarla cambia el inventario principal.</param>
    public RegistroInventarios(IWebHostEnvironment entornoAplicacion, IConfiguration configuracionAplicacion)
    {
        this.entornoAplicacion = entornoAplicacion;
        this.configuracionAplicacion = configuracionAplicacion;
    }

    /// <summary>
    /// Devuelve el inventario elegido por la cookie si todavía existe, y Catastro como valor predeterminado.
    /// </summary>
    /// <param name="identificadorSolicitado">Preferencia del navegador; cambiarla selecciona otro archivo registrado.</param>
    /// <returns>Identificador válido; cambiar el resultado cambia la conexión de la solicitud.</returns>
    public string ObtenerIdentificadorActivo(string? identificadorSolicitado)
    {
        // inventariosDisponibles limita la selección a bases SQLite que el servidor reconoce.
        var inventariosDisponibles = Listar();
        return inventariosDisponibles.FirstOrDefault(inventario =>
            string.Equals(inventario.Identificador, identificadorSolicitado, StringComparison.OrdinalIgnoreCase))
            ?.Identificador ?? inventariosDisponibles[0].Identificador;
    }

    /// <summary>
    /// Devuelve los inventarios del directorio dedicado junto con el archivo principal actual.
    /// </summary>
    /// <returns>Inventarios disponibles; crear o retirar un archivo SQLite modifica la lista devuelta.</returns>
    public IReadOnlyList<FichaInventario> Listar()
    {
        // rutaPrincipal conserva el archivo que ya usa la aplicación como sede inicial.
        var rutaPrincipal = ObtenerRutaPrincipal();
        // nombrePrincipal identifica la base existente en el selector; cambiar la configuración cambia esta etiqueta.
        var nombrePrincipal = configuracionAplicacion["InventarioInicial"]?.Trim();
        if (string.IsNullOrWhiteSpace(nombrePrincipal))
        {
            nombrePrincipal = "Catastro";
        }

        // resultado reúne la base inicial y los archivos adicionales encontrados.
        var resultado = new List<FichaInventario>
        {
            new("catastro", nombrePrincipal, true, rutaPrincipal)
        };

        // carpetaInventarios contiene solamente las nuevas bases creadas desde la interfaz.
        var carpetaInventarios = ObtenerCarpetaInventarios();
        if (!Directory.Exists(carpetaInventarios))
        {
            return resultado;
        }

        // rutaBase recorre archivos SQLite para detectar inventarios adicionales persistentes.
        foreach (var rutaBase in Directory.EnumerateFiles(carpetaInventarios, "*.db", SearchOption.TopDirectoryOnly)
                     .OrderBy(ruta => ruta, StringComparer.OrdinalIgnoreCase))
        {
            // identificador procede del nombre del archivo y nunca de una ruta recibida por formulario.
            var identificador = Path.GetFileNameWithoutExtension(rutaBase);
            if (string.IsNullOrWhiteSpace(identificador) ||
                string.Equals(rutaBase, rutaPrincipal, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            resultado.Add(new FichaInventario(identificador, FormatearNombre(identificador), false, rutaBase));
        }

        return resultado;
    }

    /// <summary>
    /// Obtiene la ruta SQLite permitida para el identificador seleccionado.
    /// </summary>
    /// <param name="identificadorSolicitado">Identificador del selector; cambiarlo dirige el contexto a otro inventario permitido.</param>
    /// <returns>Ruta de una ficha registrada; cambiar el identificador cambia el archivo consultado.</returns>
    public string ObtenerRuta(string? identificadorSolicitado)
    {
        // identificadorActivo descarta valores de cookie que no correspondan a una base conocida.
        var identificadorActivo = ObtenerIdentificadorActivo(identificadorSolicitado);
        return Listar().Single(inventario => inventario.Identificador == identificadorActivo).Ruta;
    }

    /// <summary>
    /// Crea una sede nueva y guarda dentro de ella las fichas CSV ya validadas.
    /// </summary>
    /// <param name="nombreSolicitado">Nombre de la sede; cambiarlo define la etiqueta y el archivo SQLite.</param>
    /// <param name="equiposImportados">Fichas candidatas; cambiar la lista cambia los registros y puertos provisionales creados.</param>
    /// <param name="usuarioEditor">Autor de la carga; cambiarlo altera la atribución en el historial de la sede.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla cancela la creación o escritura.</param>
    /// <returns>Base nueva, cantidad de equipos y puertos guardados.</returns>
    public async Task<(FichaInventario Inventario, int CantidadEquipos, int CantidadInterfaces)> CrearConEquiposAsync(
        string nombreSolicitado,
        IReadOnlyCollection<EquipoRed> equiposImportados,
        string usuarioEditor,
        CancellationToken cancelacion = default)
    {
        if (equiposImportados.Count == 0)
        {
            throw new InvalidOperationException("No hay equipos válidos para importar.");
        }

        // fichaNueva identifica el archivo recién creado; cambiarla dirigiría la carga a otra sede.
        var fichaNueva = await CrearAsync(nombreSolicitado, cancelacion);
        var cantidadInterfaces = 0;
        try
        {
            // opcionesImportacion conecta solo el archivo nuevo y mantiene Catastro fuera de esta operación.
            var opcionesImportacion = new DbContextOptionsBuilder<InventarioDbContext>()
                .UseSqlite(new SqliteConnectionStringBuilder { DataSource = fichaNueva.Ruta }.ToString())
                .Options;
            // contextoImportacion limita la transacción al inventario recién creado.
            await using var contextoImportacion = new InventarioDbContext(opcionesImportacion);
            await using var transaccionImportacion = await contextoImportacion.Database.BeginTransactionAsync(cancelacion);

            // normalizarIds impide trasladar identificadores internos de otra base de datos.
            foreach (var equipoImportado in equiposImportados)
            {
                equipoImportado.Identificador = 0;
            }

            await contextoImportacion.Equipos.AddRangeAsync(equiposImportados, cancelacion);
            await contextoImportacion.SaveChangesAsync(cancelacion);

            // interfacesGeneradas crea solo las posiciones derivadas de capacidades indicadas en el CSV.
            var interfacesGeneradas = equiposImportados
                .SelectMany(equipoImportado => GeneradorInterfacesSwitch.CrearInterfacesFaltantes(equipoImportado, []))
                .ToList();
            cantidadInterfaces = interfacesGeneradas.Count;
            if (interfacesGeneradas.Count > 0)
            {
                await contextoImportacion.InterfacesEquipo.AddRangeAsync(interfacesGeneradas, cancelacion);
            }

            // eventosCarga deja una nota por ficha, sin registrar una línea por cada puerto generado.
            var eventosCarga = equiposImportados.Select(equipoImportado => new EventoModificacion
            {
                IdentificadorEquipo = equipoImportado.Identificador,
                NombreEquipo = equipoImportado.Nombre,
                NombreUsuario = usuarioEditor,
                FechaUtc = DateTime.UtcNow,
                Campo = "Alta por importación CSV",
                ValorNuevo = GeneradorInterfacesSwitch.DescribirInterfacesCreadas(
                    interfacesGeneradas.Where(interfaz => interfaz.IdentificadorEquipo == equipoImportado.Identificador).ToList())
            }).ToList();
            foreach (var eventoCarga in eventosCarga.Where(eventoCarga => string.IsNullOrWhiteSpace(eventoCarga.ValorNuevo)))
            {
                eventoCarga.ValorNuevo = "Ficha cargada; sin puertos provisionales por capacidad.";
            }

            await contextoImportacion.EventosModificacion.AddRangeAsync(eventosCarga, cancelacion);
            await contextoImportacion.SaveChangesAsync(cancelacion);
            await transaccionImportacion.CommitAsync(cancelacion);
            return (fichaNueva, equiposImportados.Count, cantidadInterfaces);
        }
        catch
        {
            // LimpiarBaseParcial retira únicamente el archivo creado por esta llamada si la carga no finaliza.
            try
            {
                File.Delete(fichaNueva.Ruta);
                File.Delete(fichaNueva.Ruta + "-wal");
                File.Delete(fichaNueva.Ruta + "-shm");
            }
            catch (IOException)
            {
                // La excepción original conserva el motivo; el archivo podría requerir limpieza manual.
            }
            catch (UnauthorizedAccessException)
            {
                // La excepción original se conserva aunque el hosting no permita limpiar la base parcial.
            }

            throw;
        }
    }

    /// <summary>
    /// Crea un inventario SQLite vacío y aplica el esquema versionado sin importar datos de Catastro.
    /// </summary>
    /// <param name="nombreSolicitado">Nombre visible; cambiarlo modifica el nombre del nuevo archivo.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la creación y la migración.</param>
    /// <returns>Ficha del inventario creado; cambiar el nombre solicitado cambia su identificador y nombre visible.</returns>
    public async Task<FichaInventario> CrearAsync(string nombreSolicitado, CancellationToken cancelacion = default)
    {
        // nombreNormalizado se conserva como nombre visible y no permite nombres vacíos o desproporcionados.
        var nombreNormalizado = nombreSolicitado.Trim();
        if (nombreNormalizado.Length is < 2 or > 60)
        {
            throw new InvalidOperationException("El nombre debe tener entre 2 y 60 caracteres.");
        }

        await bloqueoCreacion.WaitAsync(cancelacion);
        try
        {
            // identificadorNuevo convierte el nombre a una ruta segura y estable para el archivo.
            var identificadorNuevo = CrearIdentificador(nombreNormalizado);
            // inventariosActuales se vuelve a contar dentro del bloqueo para respetar el límite incluso con dos solicitudes.
            var inventariosActuales = Listar();
            if (inventariosActuales.Count >= 3)
            {
                throw new InvalidOperationException("Ya existen tres inventarios. El límite acordado es tres.");
            }

            if (identificadorNuevo.Length == 0)
            {
                throw new InvalidOperationException("El nombre debe incluir al menos una letra o un número.");
            }

            if (inventariosActuales.Any(inventario =>
                    string.Equals(inventario.Identificador, identificadorNuevo, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Ya existe un inventario con ese nombre o identificador.");
            }

            // rutaNueva queda confinada al directorio dedicado de inventarios adicionales.
            var rutaNueva = Path.Combine(ObtenerCarpetaInventarios(), identificadorNuevo + ".db");
            // rutaPrincipalProtegida evita sobrescribir la base actual si la carpeta de nuevas sedes coincide con su ubicación.
            var rutaPrincipalProtegida = ObtenerRutaPrincipal();
            if (string.Equals(Path.GetFullPath(rutaNueva), Path.GetFullPath(rutaPrincipalProtegida), StringComparison.OrdinalIgnoreCase) ||
                File.Exists(rutaNueva))
            {
                throw new InvalidOperationException("Ya existe un archivo SQLite con ese nombre y no se sobrescribirá.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(rutaNueva)!);
            await using (File.Create(rutaNueva))
            {
            }

            try
            {
                // opcionesBase aplica la misma configuración de SQLite y migraciones que usa la aplicación.
                var opcionesBase = new DbContextOptionsBuilder<InventarioDbContext>()
                    .UseSqlite(new SqliteConnectionStringBuilder { DataSource = rutaNueva }.ToString())
                    .Options;
                // contextoInventario migra únicamente el archivo recién creado y no carga fichas de Catastro.
                await using var contextoInventario = new InventarioDbContext(opcionesBase);
                await PreparadorMigraciones.AplicarAsync(contextoInventario, cancelacion);
            }
            catch
            {
                File.Delete(rutaNueva);
                File.Delete(rutaNueva + "-wal");
                File.Delete(rutaNueva + "-shm");
                throw;
            }

            return new FichaInventario(identificadorNuevo, nombreNormalizado, false, rutaNueva);
        }
        finally
        {
            bloqueoCreacion.Release();
        }
    }

    /// <summary>
    /// Resuelve el directorio exclusivo de inventarios adicionales.
    /// </summary>
    /// <returns>Carpeta configurada; cambiarla cambia dónde se buscan y crean inventarios nuevos.</returns>
    private string ObtenerCarpetaInventarios()
    {
        // rutaConfigurada permite alojar archivos fuera del directorio de publicación cuando el hosting lo requiera.
        var rutaConfigurada = configuracionAplicacion["RutaInventarios"];
        if (string.IsNullOrWhiteSpace(rutaConfigurada))
        {
            rutaConfigurada = "inventarios";
        }

        return Path.IsPathRooted(rutaConfigurada)
            ? rutaConfigurada
            : Path.GetFullPath(Path.Combine(entornoAplicacion.ContentRootPath, rutaConfigurada));
    }

    /// <summary>
    /// Obtiene la ruta del archivo SQLite configurado para el inventario principal existente.
    /// </summary>
    /// <returns>Ruta absoluta; cambiar la cadena `ConexionRed` de configuración cambia el archivo principal utilizado.</returns>
    private string ObtenerRutaPrincipal()
    {
        // cadenaConexion mantiene el archivo ya configurado por la aplicación.
        var cadenaConexion = configuracionAplicacion.GetConnectionString("ConexionRed");
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException("Falta configurar la conexión SQLite ConexionRed.");
        }

        // rutaConfigurada interpreta el origen SQLite antes de resolver rutas relativas al directorio de publicación.
        var rutaConfigurada = new SqliteConnectionStringBuilder(cadenaConexion).DataSource;
        return Path.IsPathRooted(rutaConfigurada)
            ? rutaConfigurada
            : Path.GetFullPath(Path.Combine(entornoAplicacion.ContentRootPath, rutaConfigurada));
    }

    /// <summary>
    /// Normaliza un nombre para usarlo como identificador seguro de archivo.
    /// </summary>
    /// <param name="nombre">Nombre visible; cambiarlo produce otro identificador de inventario.</param>
    /// <returns>Slug alfanumérico con guiones; cambiar las reglas altera nombres de archivo y enlaces.</returns>
    private static string CrearIdentificador(string nombre)
    {
        // textoDescompuesto separa acentos para formar un nombre de archivo portable.
        var textoDescompuesto = nombre.Normalize(System.Text.NormalizationForm.FormD);
        // caracteresValidos conserva letras y números y reemplaza signos por separadores.
        var caracteresValidos = textoDescompuesto
            .Where(caracter => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(caracter) !=
                               System.Globalization.UnicodeCategory.NonSpacingMark)
            .Select(caracter => char.IsLetterOrDigit(caracter) ? char.ToLowerInvariant(caracter) : '-');
        // identificador resultante se limita a minúsculas, números y guiones consecutivos reducidos.
        var identificador = new string(caracteresValidos.ToArray());
        while (identificador.Contains("--", StringComparison.Ordinal))
        {
            identificador = identificador.Replace("--", "-", StringComparison.Ordinal);
        }

        return identificador.Trim('-');
    }

    /// <summary>
    /// Convierte el identificador de archivo a un nombre legible para el selector.
    /// </summary>
    /// <param name="identificador">Slug persistido en el nombre del archivo; cambiarlo altera el texto mostrado.</param>
    /// <returns>Nombre con palabras separadas; cambiar el formato solo afecta la presentación del selector.</returns>
    private static string FormatearNombre(string identificador)
    {
        // palabras separa el slug para presentarlo como nombre legible.
        return string.Join(" ", identificador.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(palabra => char.ToUpperInvariant(palabra[0]) + palabra[1..]));
    }
}
