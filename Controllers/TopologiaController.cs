using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Controllers;

/// <summary>
/// Muestra las capas previstas y los enlaces físicos que ya estén respaldados por datos.
/// </summary>
public class TopologiaController : Controller
{
    // contextoInventario consulta interfaces y enlaces documentados; cambiarlo selecciona otra base.
    private readonly InventarioDbContext contextoInventario;

    /// <summary>
    /// Prepara la consulta de la vista de topología.
    /// </summary>
    /// <param name="contexto">Contexto del inventario; cambiarlo modifica la fuente de interfaces y enlaces.</param>
    public TopologiaController(InventarioDbContext contexto)
    {
        contextoInventario = contexto;
    }

    /// <summary>
    /// Presenta el estado de preparación y los enlaces físicos registrados.
    /// </summary>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe conteos y consultas.</param>
    /// <returns>Vista por capas con conteos documentales y enlaces disponibles.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancelacion)
    {
        // totalInterfaces resume puertos guardados; cambiar la tabla consultada modifica el conteo mostrado.
        var totalInterfaces = await contextoInventario.InterfacesEquipo.CountAsync(cancelacion);
        // totalEnlaces resume vínculos físicos guardados; cambiar la tabla consultada modifica el conteo mostrado.
        var totalEnlaces = await contextoInventario.EnlacesFisicos.CountAsync(cancelacion);

        // enlacesDocumentados une extremos con sus equipos; cambiar las uniones cambia qué conexiones aparecen.
        var enlacesDocumentados = await (
            from enlace in contextoInventario.EnlacesFisicos.AsNoTracking()
            join puertoOrigen in contextoInventario.InterfacesEquipo.AsNoTracking()
                on enlace.IdentificadorPuertoOrigen equals puertoOrigen.Identificador
            join equipoOrigen in contextoInventario.Equipos.AsNoTracking()
                on puertoOrigen.IdentificadorEquipo equals equipoOrigen.Identificador
            join puertoDestino in contextoInventario.InterfacesEquipo.AsNoTracking()
                on enlace.IdentificadorPuertoDestino equals puertoDestino.Identificador
            join equipoDestino in contextoInventario.Equipos.AsNoTracking()
                on puertoDestino.IdentificadorEquipo equals equipoDestino.Identificador
            orderby equipoOrigen.Nombre, equipoDestino.Nombre
            select new EnlaceTopologiaViewModel
            {
                EquipoOrigen = equipoOrigen.Nombre,
                PuertoOrigen = puertoOrigen.EtiquetaPuerto,
                EquipoDestino = equipoDestino.Nombre,
                PuertoDestino = puertoDestino.EtiquetaPuerto,
                Medio = enlace.Medio,
                EstadoDatos = enlace.EstadoDatos,
                FechaUltimaRevision = enlace.FechaUltimaRevision
            })
            .ToListAsync(cancelacion);

        // resumenTopologia agrupa los datos para la página; cambiar sus conteos modifica los indicadores visibles.
        var resumenTopologia = new TopologiaViewModel
        {
            TotalInterfaces = totalInterfaces,
            TotalEnlaces = totalEnlaces,
            Enlaces = enlacesDocumentados
        };

        return View(resumenTopologia);
    }
}
