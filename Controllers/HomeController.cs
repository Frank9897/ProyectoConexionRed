using Microsoft.AspNetCore.Mvc;
using ProyectoConexionRed.Web.Services;

namespace ProyectoConexionRed.Web.Controllers;

/// <summary>
/// Presenta la gestión de inventarios en la página de inicio.
/// </summary>
public class HomeController : Controller
{
    // registroInventarios consulta las sedes disponibles; reemplazarlo cambia las bases mostradas en Inicio.
    private readonly RegistroInventarios registroInventarios;

    /// <summary>
    /// Crea el controlador con el registro de inventarios.
    /// </summary>
    /// <param name="registroInventarios">Registro de bases SQLite; cambiarlo dirige la página de inicio a otra colección.</param>
    public HomeController(RegistroInventarios registroInventarios)
    {
        this.registroInventarios = registroInventarios;
    }

    /// <summary>
    /// Muestra y permite seleccionar o crear inventarios desde Inicio.
    /// </summary>
    /// <returns>Vista de gestión de inventarios; cambiar el modelo modifica las bases enumeradas.</returns>
    public IActionResult Index()
    {
        // inventariosDisponibles enumera las bases registradas para que Inicio las permita seleccionar.
        var inventariosDisponibles = registroInventarios.Listar();
        // identificadorActivo señala la base elegida en este navegador; cambiarlo marca otra ficha como activa.
        var identificadorActivo = registroInventarios.ObtenerIdentificadorActivo(
            Request.Cookies[RegistroInventarios.NombreCookieInventario]);
        ViewBag.IdentificadorActivo = identificadorActivo;
        return View(inventariosDisponibles);
    }
}
