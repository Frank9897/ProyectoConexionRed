using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;

namespace ProyectoConexionRed.Web.Controllers;

/// <summary>
/// Muestra el historial reciente de accesos a la aplicación.
/// </summary>
public class AuditoriaController : Controller
{
    // contextoInventario lee el historial persistido; cambiarlo consulta otra base de datos.
    private readonly InventarioDbContext contextoInventario;

    /// <summary>
    /// Prepara la consulta de eventos de autenticación.
    /// </summary>
    /// <param name="contexto">Contexto de datos; cambiarlo cambia la fuente del historial.</param>
    public AuditoriaController(InventarioDbContext contexto)
    {
        contextoInventario = contexto;
    }

    /// <summary>
    /// Presenta hasta cien eventos de acceso, empezando por los más recientes.
    /// </summary>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la lectura.</param>
    /// <returns>Vista del historial; cambiar el límite altera cuántos eventos se muestran.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancelacion)
    {
        // eventosAcceso ordena los eventos por hora; cambiar la expresión cambia el orden temporal mostrado.
        var eventosAcceso = await contextoInventario.EventosAcceso
            .AsNoTracking()
            .OrderByDescending(evento => evento.FechaUtc)
            .Take(100)
            .ToListAsync(cancelacion);

        return View(eventosAcceso);
    }
}
