using Microsoft.AspNetCore.Mvc;
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
