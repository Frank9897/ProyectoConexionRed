using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProyectoConexionRed.Web.Models;
using ProyectoConexionRed.Web.Services;

namespace ProyectoConexionRed.Web.Controllers;

/// <summary>
/// Permite consultar, crear y seleccionar bases de inventario independientes.
/// </summary>
public sealed class InventariosController : Controller
{
    // registroInventarios resuelve rutas y límites; reemplazarlo cambia las bases que este controlador administra.
    private readonly RegistroInventarios registroInventarios;

    /// <summary>
    /// Crea el controlador con el registro de bases disponibles.
    /// </summary>
    /// <param name="registroInventarios">Registro SQLite; cambiarlo dirige las acciones a otra colección de bases.</param>
    public InventariosController(RegistroInventarios registroInventarios)
    {
        this.registroInventarios = registroInventarios;
    }

    /// <summary>
    /// Muestra los inventarios registrados y cuál usa el navegador actual.
    /// </summary>
    /// <returns>Vista con hasta tres inventarios; cambiar sus archivos modifica las opciones visibles.</returns>
    [HttpGet]
    public IActionResult Index()
    {
        // inventariosDisponibles limita la pantalla a bases encontradas por el registro.
        var inventariosDisponibles = registroInventarios.Listar();
        // identificadorActivo refleja la preferencia vigente del navegador sin cambiar la base.
        var identificadorActivo = registroInventarios.ObtenerIdentificadorActivo(
            Request.Cookies[RegistroInventarios.NombreCookieInventario]);
        ViewBag.IdentificadorActivo = identificadorActivo;
        return View("~/Views/Home/Inventarios.cshtml", inventariosDisponibles);
    }

    /// <summary>
    /// Crea una sede vacía, aplica el esquema y la selecciona en el navegador actual.
    /// </summary>
    /// <param name="nombre">Nombre que identifica la nueva base; cambiarlo determina su nombre visible y archivo SQLite.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la creación.</param>
    /// <returns>Redirección al inventario recién creado o a la página de selección si hay error.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(string nombre, CancellationToken cancelacion)
    {
        try
        {
            // inventarioCreado identifica la nueva base migrada; cambiar su nombre afecta su archivo y etiqueta.
            var inventarioCreado = await registroInventarios.CrearAsync(nombre ?? string.Empty, cancelacion);
            GuardarSeleccion(inventarioCreado.Identificador);
            TempData["AvisoInventario"] = $"Se creó el inventario vacío «{inventarioCreado.Nombre}».";
            return RedirectToAction("Index", "Home");
        }
        catch (InvalidOperationException excepcionInventario)
        {
            TempData["ErrorInventario"] = excepcionInventario.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (IOException)
        {
            TempData["ErrorInventario"] = "No se pudo crear el archivo. Revisa los permisos de la carpeta configurada para inventarios.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException)
        {
            TempData["ErrorInventario"] = "El servidor no tiene permisos de escritura en la carpeta configurada para inventarios.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Cambia el inventario activo del navegador sin modificar ninguna base de datos.
    /// </summary>
    /// <param name="identificador">Identificador registrado; cambiarlo selecciona otro inventario permitido.</param>
    /// <param name="volverA">Ruta local de retorno; cambiarla dirige a otra página del mismo sitio.</param>
    /// <returns>Redirección a la página local solicitada o a la portada.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Seleccionar(string identificador, string? volverA)
    {
        // inventarioSeleccionado valida el identificador contra las bases locales disponibles.
        var inventarioSeleccionado = registroInventarios.Listar().FirstOrDefault(inventario =>
            string.Equals(inventario.Identificador, identificador, StringComparison.OrdinalIgnoreCase));
        if (inventarioSeleccionado is null)
        {
            TempData["ErrorInventario"] = "El inventario solicitado ya no está disponible.";
            return RedirectToAction(nameof(Index));
        }

        GuardarSeleccion(inventarioSeleccionado.Identificador);
        TempData["AvisoInventario"] = $"Inventario activo: {inventarioSeleccionado.Nombre}.";
        return Url.IsLocalUrl(volverA) ? Redirect(volverA!) : RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Abre la carga guiada de equipos desde un archivo CSV.
    /// </summary>
    /// <returns>Formulario que permite subir una plantilla sin modificar las bases existentes.</returns>
    [HttpGet]
    public IActionResult ImportarCsv()
    {
        if (registroInventarios.Listar().Count >= 3)
        {
            TempData["ErrorInventario"] = "Ya se alcanzó el máximo de tres inventarios.";
            return RedirectToAction(nameof(Index));
        }

        // formularioCarga comienza sin archivo ni datos; cambiar sus campos define la futura sede importada.
        return View("~/Views/Home/ImportarCsv.cshtml", new VistaPreviaImportacionViewModel());
    }

    /// <summary>
    /// Descarga el encabezado CSV compatible con la exportación de inventario.
    /// </summary>
    /// <returns>Plantilla vacía; cambiar los encabezados cambia qué columnas reconoce el importador.</returns>
    [HttpGet]
    public IActionResult DescargarPlantillaCsv()
    {
        // bytesPlantilla incorpora BOM UTF-8 para que las hojas de cálculo reconozcan las tildes.
        var bytesPlantilla = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(ImportadorInventarioCsv.CrearPlantilla()))
            .ToArray();
        return File(bytesPlantilla, "text/csv; charset=utf-8", "plantilla-inventario-red.csv");
    }

    /// <summary>
    /// Analiza un CSV y presenta errores y una muestra antes de crear otra base SQLite.
    /// </summary>
    /// <param name="nombreInventario">Nombre de la sede; cambiarlo modifica el archivo que se crearía.</param>
    /// <param name="archivo">CSV subido; cambiarlo modifica la vista previa, pero aún no escribe en SQLite.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la lectura del archivo.</param>
    /// <returns>Vista previa con advertencias y candidatos, o formulario con errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> PrevisualizarCsv(
        string? nombreInventario,
        IFormFile? archivo,
        CancellationToken cancelacion)
    {
        if (registroInventarios.Listar().Count >= 3)
        {
            TempData["ErrorInventario"] = "Ya se alcanzó el máximo de tres inventarios.";
            return RedirectToAction(nameof(Index));
        }

        // erroresNombre registra problemas de etiqueta antes de reservar un archivo SQLite.
        var erroresNombre = ValidarNombreInventario(nombreInventario);
        if (archivo is null || archivo.Length == 0)
        {
            erroresNombre.Add("Selecciona un archivo CSV con encabezados y al menos un equipo.");
            return View("~/Views/Home/ImportarCsv.cshtml", new VistaPreviaImportacionViewModel
            {
                NombreInventario = nombreInventario ?? string.Empty,
                Errores = erroresNombre
            });
        }

        if (!string.Equals(Path.GetExtension(archivo.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            erroresNombre.Add("El archivo debe tener extensión .csv.");
            return View("~/Views/Home/ImportarCsv.cshtml", new VistaPreviaImportacionViewModel
            {
                NombreInventario = nombreInventario ?? string.Empty,
                Errores = erroresNombre
            });
        }

        if (archivo.Length > 1_000_000)
        {
            erroresNombre.Add("El archivo no puede superar 1 MB.");
            return View("~/Views/Home/ImportarCsv.cshtml", new VistaPreviaImportacionViewModel
            {
                NombreInventario = nombreInventario ?? string.Empty,
                Errores = erroresNombre
            });
        }

        // lectorCsv interpreta UTF-8 y detecta el BOM opcional del archivo exportado.
        using var lectorCsv = new StreamReader(archivo.OpenReadStream(), Encoding.UTF8, true);
        // contenidoCsv queda limitado por el tamaño máximo de archivo antes de analizar las filas.
        var contenidoCsv = await lectorCsv.ReadToEndAsync();
        // resultadoAnalisis contiene fichas candidatas y avisos sin persistirlos.
        var resultadoAnalisis = ImportadorInventarioCsv.Analizar(contenidoCsv);
        // vistaPrevia incluye solo una muestra de filas para revisar antes de confirmar.
        var vistaPrevia = CrearVistaPrevia(nombreInventario ?? string.Empty, contenidoCsv, resultadoAnalisis);
        vistaPrevia.Errores.AddRange(erroresNombre);
        return View("~/Views/Home/ImportarCsv.cshtml", vistaPrevia);
    }

    /// <summary>
    /// Reanaliza el contenido confirmado y crea una sede nueva sin modificar Catastro.
    /// </summary>
    /// <param name="nombreInventario">Nombre de sede confirmado; cambiarlo define la etiqueta y archivo creados.</param>
    /// <param name="contenidoCsvCodificado">CSV de la vista previa; cambiarlo hace que el servidor vuelva a validar otro contenido.</param>
    /// <param name="confirmarAvisos">Confirmación humana; marcarla autoriza continuar pese a avisos no bloqueantes.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la decodificación o escritura.</param>
    /// <returns>Redirección al inventario nuevo o vista previa si el contenido ya no es válido.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> ConfirmarCsv(
        string? nombreInventario,
        string? contenidoCsvCodificado,
        bool confirmarAvisos,
        CancellationToken cancelacion)
    {
        // erroresNombre vuelve a validar el nombre justo antes de intentar crear el archivo.
        var erroresNombre = ValidarNombreInventario(nombreInventario);
        if (string.IsNullOrWhiteSpace(contenidoCsvCodificado) || contenidoCsvCodificado.Length > 1_400_000)
        {
            erroresNombre.Add("La vista previa venció o su contenido supera el límite. Vuelve a seleccionar el archivo.");
            return View("~/Views/Home/ImportarCsv.cshtml", new VistaPreviaImportacionViewModel
            {
                NombreInventario = nombreInventario ?? string.Empty,
                Errores = erroresNombre
            });
        }

        // contenidoBytes conserva el CSV original para verificarlo otra vez en el servidor.
        byte[] contenidoBytes;
        try
        {
            contenidoBytes = Convert.FromBase64String(contenidoCsvCodificado);
        }
        catch (FormatException)
        {
            erroresNombre.Add("El contenido de la vista previa no es válido. Vuelve a cargar el archivo.");
            return View("~/Views/Home/ImportarCsv.cshtml", new VistaPreviaImportacionViewModel
            {
                NombreInventario = nombreInventario ?? string.Empty,
                Errores = erroresNombre
            });
        }

        if (contenidoBytes.Length > 1_000_000)
        {
            erroresNombre.Add("El CSV supera el límite de 1 MB.");
            return View("~/Views/Home/ImportarCsv.cshtml", new VistaPreviaImportacionViewModel
            {
                NombreInventario = nombreInventario ?? string.Empty,
                Errores = erroresNombre
            });
        }

        // contenidoCsv vuelve a pasar por el mismo parser para impedir confiar en los datos del navegador.
        string contenidoCsv;
        try
        {
            contenidoCsv = new UTF8Encoding(false, true).GetString(contenidoBytes);
        }
        catch (DecoderFallbackException)
        {
            erroresNombre.Add("El archivo debe estar codificado en UTF-8. Vuelve a cargar una versión compatible.");
            return View("~/Views/Home/ImportarCsv.cshtml", new VistaPreviaImportacionViewModel
            {
                NombreInventario = nombreInventario ?? string.Empty,
                Errores = erroresNombre
            });
        }

        // resultadoAnalisis valida contenido, duplicados y capacidades justo antes de escribir.
        var resultadoAnalisis = ImportadorInventarioCsv.Analizar(contenidoCsv);
        // vistaPrevia conserva el contenido y explica cualquier cambio inválido detectado al confirmar.
        var vistaPrevia = CrearVistaPrevia(nombreInventario ?? string.Empty, contenidoCsv, resultadoAnalisis);
        vistaPrevia.Errores.AddRange(erroresNombre);
        if (registroInventarios.Listar().Count >= 3)
        {
            vistaPrevia.Errores.Add("Ya se alcanzó el máximo de tres inventarios.");
        }

        if (resultadoAnalisis.Avisos.Count > 0 && !confirmarAvisos)
        {
            vistaPrevia.ConfirmacionAvisosPendiente = true;
        }

        if (vistaPrevia.Errores.Count > 0 || resultadoAnalisis.Equipos.Count == 0)
        {
            return View("~/Views/Home/ImportarCsv.cshtml", vistaPrevia);
        }

        try
        {
            // usuarioEditor identifica la cuenta o el modo local que confirmó la carga.
            var usuarioEditor = User.Identity?.Name ?? "Carga CSV sin sesión";
            // cargaCompletada crea una base nueva e inserta equipos, puertos provisionales e historial dentro de ella.
            var cargaCompletada = await registroInventarios.CrearConEquiposAsync(
                nombreInventario!.Trim(), resultadoAnalisis.Equipos, usuarioEditor, cancelacion);
            GuardarSeleccion(cargaCompletada.Inventario.Identificador);
            TempData["AvisoInventario"] =
                $"Se creó «{cargaCompletada.Inventario.Nombre}» con {cargaCompletada.CantidadEquipos} equipos y {cargaCompletada.CantidadInterfaces} puertos provisionales. No se importaron enlaces.";
            return RedirectToAction("Index", "Inventario");
        }
        catch (InvalidOperationException excepcionCarga)
        {
            // excepcionCarga informa un límite o nombre no disponible; cambiar su texto modifica el aviso mostrado.
            vistaPrevia.Errores.Add(excepcionCarga.Message);
            return View("~/Views/Home/ImportarCsv.cshtml", vistaPrevia);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // falloPersistencia evita mostrar detalles técnicos del motor y conserva la vista previa para corregir.
            vistaPrevia.Errores.Add("No se pudo guardar la carga en la nueva base. Revisa los datos del archivo e inténtalo de nuevo.");
            return View("~/Views/Home/ImportarCsv.cshtml", vistaPrevia);
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            // falloSqlite indica una operación SQLite fallida sin filtrar la consulta ni la ruta del archivo.
            vistaPrevia.Errores.Add("SQLite no pudo completar la carga. Verifica el espacio disponible y vuelve a intentarlo.");
            return View("~/Views/Home/ImportarCsv.cshtml", vistaPrevia);
        }
        catch (IOException)
        {
            vistaPrevia.Errores.Add("No se pudo crear la nueva base. Revisa los permisos de la carpeta de inventarios.");
            return View("~/Views/Home/ImportarCsv.cshtml", vistaPrevia);
        }
        catch (UnauthorizedAccessException)
        {
            vistaPrevia.Errores.Add("El servidor no tiene permisos de escritura en la carpeta de inventarios.");
            return View("~/Views/Home/ImportarCsv.cshtml", vistaPrevia);
        }
    }

    /// <summary>
    /// Construye la muestra visual a partir de una lectura, sin persistir datos.
    /// </summary>
    /// <param name="nombreInventario">Nombre propuesto; cambiarlo altera lo que se enviaría al crear la sede.</param>
    /// <param name="contenidoCsv">Texto original; cambiarlo altera el contenido guardado para confirmación.</param>
    /// <param name="resultadoAnalisis">Datos parseados; cambiarlo cambia filas, errores y advertencias.</param>
    /// <returns>Modelo de vista con una muestra de hasta 25 equipos.</returns>
    private static VistaPreviaImportacionViewModel CrearVistaPrevia(
        string nombreInventario,
        string contenidoCsv,
        ResultadoImportacionCsv resultadoAnalisis)
    {
        // cantidadFilasVisibles limita la muestra sin quitar equipos de la carga confirmada.
        const int cantidadFilasVisibles = 25;
        // puertosProvisionales suma capacidades declaradas; las etiquetas concretas seguirán por verificar.
        var puertosProvisionales = resultadoAnalisis.Equipos.Sum(equipo =>
            (equipo.CantidadPuertosRj45 ?? 0) + (equipo.CantidadPuertosSfp ?? 0)
            + (equipo.CantidadPuertosSfpPlus ?? 0) + (equipo.CantidadPuertosQsfp ?? 0)
            + (equipo.CantidadPuertosQsfpPlus ?? 0) + (equipo.CantidadPuertosQsfp28 ?? 0));
        // modeloPrevisualizacion conserva el CSV original y los resultados que se revisan en la interfaz.
        var modeloPrevisualizacion = new VistaPreviaImportacionViewModel
        {
            NombreInventario = nombreInventario,
            ContenidoCsvCodificado = Convert.ToBase64String(Encoding.UTF8.GetBytes(contenidoCsv)),
            TieneVistaPrevia = true,
            Errores = resultadoAnalisis.Errores.ToList(),
            Avisos = resultadoAnalisis.Avisos.ToList(),
            CantidadEquipos = resultadoAnalisis.Equipos.Count,
            CantidadPuertosProvisionales = puertosProvisionales,
            FilasNoMostradas = Math.Max(0, resultadoAnalisis.Equipos.Count - cantidadFilasVisibles),
            Filas = resultadoAnalisis.Equipos.Take(cantidadFilasVisibles).Select(equipo =>
                new FilaPreviaImportacionViewModel
                {
                    Nombre = equipo.Nombre,
                    Tipo = equipo.TipoDispositivo,
                    Ip = equipo.IpAdministracion,
                    Area = equipo.Area,
                    Piso = equipo.Piso,
                    Fabricante = equipo.Fabricante,
                    Modelo = equipo.Modelo,
                    Mac = equipo.DireccionMac,
                    Rol = equipo.RolEnArea,
                    Capacidad = DescribirCapacidad(equipo),
                    EstadoDatos = equipo.EstadoDatos
                }).ToList()
        };
        return modeloPrevisualizacion;
    }

    /// <summary>
    /// Resume las familias de puertos indicadas para que la persona revise qué interfaces se generarían.
    /// </summary>
    /// <param name="equipo">Ficha analizada; cambiar sus cantidades modifica el resumen mostrado.</param>
    /// <returns>Capacidades con cantidad expresa o una indicación de ausencia.</returns>
    private static string DescribirCapacidad(EquipoRed equipo)
    {
        // capacidadesEquipo incluye solo conectores cuya cantidad fue especificada en el archivo.
        var capacidadesEquipo = new List<string>();
        if (equipo.CantidadPuertosRj45.HasValue) capacidadesEquipo.Add($"{equipo.CantidadPuertosRj45} RJ45");
        if (equipo.CantidadPuertosSfp.HasValue) capacidadesEquipo.Add($"{equipo.CantidadPuertosSfp} SFP");
        if (equipo.CantidadPuertosSfpPlus.HasValue) capacidadesEquipo.Add($"{equipo.CantidadPuertosSfpPlus} SFP+");
        if (equipo.CantidadPuertosQsfp.HasValue) capacidadesEquipo.Add($"{equipo.CantidadPuertosQsfp} QSFP");
        if (equipo.CantidadPuertosQsfpPlus.HasValue) capacidadesEquipo.Add($"{equipo.CantidadPuertosQsfpPlus} QSFP+");
        if (equipo.CantidadPuertosQsfp28.HasValue) capacidadesEquipo.Add($"{equipo.CantidadPuertosQsfp28} QSFP28");
        return capacidadesEquipo.Count == 0 ? "Sin capacidad indicada" : string.Join(" · ", capacidadesEquipo);
    }

    /// <summary>
    /// Verifica el nombre visible antes de reservar una nueva base SQLite.
    /// </summary>
    /// <param name="nombreInventario">Texto propuesto; cambiarlo puede activar reglas de longitud.</param>
    /// <returns>Errores de nombre que bloquean la confirmación.</returns>
    private static List<string> ValidarNombreInventario(string? nombreInventario)
    {
        // erroresNombre acumula restricciones de tamaño sin tocar el archivo de inventario.
        var erroresNombre = new List<string>();
        var nombreNormalizado = nombreInventario?.Trim() ?? string.Empty;
        if (nombreNormalizado.Length is < 2 or > 60)
        {
            erroresNombre.Add("El nombre de la sede debe tener entre 2 y 60 caracteres.");
        }

        return erroresNombre;
    }

    /// <summary>
    /// Guarda la preferencia local del navegador para posteriores solicitudes.
    /// </summary>
    /// <param name="identificador">Inventario permitido; cambiarlo conserva otra selección en la cookie.</param>
    private void GuardarSeleccion(string identificador)
    {
        Response.Cookies.Append(RegistroInventarios.NombreCookieInventario, identificador, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            MaxAge = TimeSpan.FromDays(365)
        });
    }
}
