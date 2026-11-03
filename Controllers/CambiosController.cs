using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;

namespace ProyectoConexionRed.Web.Controllers;

/// <summary>
/// Presenta el historial de cambios efectuados en fichas de equipos.
/// </summary>
public class CambiosController : Controller
{
    // contextoInventario lee los cambios guardados; cambiarlo consulta otra base.
    private readonly InventarioDbContext contextoInventario;

    /// <summary>
    /// Prepara la lectura del historial de modificaciones.
    /// </summary>
    /// <param name="contexto">Contexto de datos; cambiarlo selecciona otra fuente de auditoría.</param>
    public CambiosController(InventarioDbContext contexto)
    {
        contextoInventario = contexto;
    }

    /// <summary>
    /// Muestra los últimos doscientos cambios documentales.
    /// </summary>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la consulta.</param>
    /// <returns>Vista de auditoría ordenada desde el cambio más reciente.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancelacion)
    {
        // cambiosRecientes limita y ordena los registros; cambiar Take altera cuántos eventos se muestran.
        var cambiosRecientes = await contextoInventario.EventosModificacion
            .AsNoTracking()
            .OrderByDescending(cambio => cambio.FechaUtc)
            .Take(200)
            .ToListAsync(cancelacion);

        return View(cambiosRecientes);
    }
}
