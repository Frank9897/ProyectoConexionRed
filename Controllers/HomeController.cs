using Microsoft.AspNetCore.Mvc;

namespace ProyectoConexionRed.Web.Controllers;

/// <summary>
/// Presenta la portada y el acceso al inventario inicial.
/// </summary>
public class HomeController : Controller
{
    /// <summary>
    /// Muestra el resumen del estado actual del sistema documental.
    /// </summary>
    /// <returns>Vista de inicio; cambiar el contenido de la vista modifica el resumen mostrado.</returns>
    public IActionResult Index()
    {
        return View();
    }
}
