using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;
using ProyectoConexionRed.Web.Services;

namespace ProyectoConexionRed.Web.Controllers;

/// <summary>
/// Muestra las capas de red y dibuja solo dispositivos, puertos y enlaces documentados.
/// </summary>
public class TopologiaController : Controller
{
    // contextoInventario consulta exclusivamente la base documental; cambiarlo selecciona otra base registrada.
    private readonly InventarioDbContext contextoInventario;

    /// <summary>
    /// Prepara el controlador con el contexto del inventario.
    /// </summary>
    /// <param name="contexto">Contexto de EF Core; cambiarlo modifica la fuente de dispositivos y enlaces.</param>
    public TopologiaController(InventarioDbContext contexto)
    {
        contextoInventario = contexto;
    }

    /// <summary>
    /// Carga el grafo documental y sus detalles sin consultar la red de producción.
    /// </summary>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene las consultas pendientes.</param>
    /// <returns>Vista de topología con equipos, puertos, enlaces y métricas.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancelacion)
    {
        // equiposRegistrados contiene solo fichas guardadas; cambiar la consulta altera los nodos visibles.
        var equiposRegistrados = await contextoInventario.Equipos.AsNoTracking()
            .OrderBy(equipo => equipo.Area).ThenBy(equipo => equipo.Nombre)
            .ToListAsync(cancelacion);
        // interfacesRegistradas conserva etiquetas exactas sin inferir puertos faltantes.
        var interfacesRegistradas = await contextoInventario.InterfacesEquipo.AsNoTracking()
            .OrderBy(interfaz => interfaz.EtiquetaPuerto)
            .ToListAsync(cancelacion);
        // enlacesRegistrados contiene relaciones explícitas y determina qué aristas se pueden dibujar.
        var enlacesRegistrados = await contextoInventario.EnlacesFisicos.AsNoTracking()
            .ToListAsync(cancelacion);

        // equiposPorId resuelve cada extremo de enlace a su ficha propietaria.
        var equiposPorId = equiposRegistrados.ToDictionary(equipo => equipo.Identificador);
        // interfacesPorId resuelve cada extremo guardado a su etiqueta y propietario exactos.
        var interfacesPorId = interfacesRegistradas.ToDictionary(interfaz => interfaz.Identificador);
        // puertosEnlazados permite pintar ocupado solo un puerto incluido en un enlace guardado.
        var puertosEnlazados = enlacesRegistrados
            .SelectMany(enlace => new[] { enlace.IdentificadorPuertoOrigen, enlace.IdentificadorPuertoDestino })
            .ToHashSet();
        // interfacesPorEquipo agrupa los puertos reales; cambiar el agrupamiento altera cada frontal.
        var interfacesPorEquipo = interfacesRegistradas.ToLookup(interfaz => interfaz.IdentificadorEquipo);

        // dispositivosDibujados genera un nodo por equipo y conserva los puertos sin conexión como desconocidos.
        var dispositivosDibujados = equiposRegistrados.Select(equipo =>
        {
            var esSwitch = string.Equals(equipo.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase);
            var perfilVisual = esSwitch ? CatalogoVisualesDispositivos.ObtenerPerfil(equipo.Fabricante, equipo.Modelo) : null;
            var puertosDelEquipo = interfacesPorEquipo[equipo.Identificador].ToList();
            var puertosDibujados = CatalogoVisualesDispositivos.CrearPuertos(equipo, puertosDelEquipo, puertosEnlazados);
            var cantidadesConocidas = new[]
            {
                equipo.CantidadPuertosRj45 ?? perfilVisual?.CantidadRj45,
                equipo.CantidadPuertosSfp ?? perfilVisual?.CantidadSfp,
                equipo.CantidadPuertosSfpPlus ?? perfilVisual?.CantidadSfpPlus,
                equipo.CantidadPuertosQsfp ?? perfilVisual?.CantidadQsfp,
                equipo.CantidadPuertosQsfpPlus ?? perfilVisual?.CantidadQsfpPlus,
                equipo.CantidadPuertosQsfp28 ?? perfilVisual?.CantidadQsfp28
            };
            var hayCapacidadManual = new[]
            {
                equipo.CantidadPuertosRj45, equipo.CantidadPuertosSfp, equipo.CantidadPuertosSfpPlus,
                equipo.CantidadPuertosQsfp, equipo.CantidadPuertosQsfpPlus, equipo.CantidadPuertosQsfp28
            }.Any(cantidad => cantidad.HasValue);
            var capacidadCompleta = cantidadesConocidas.All(cantidad => cantidad.HasValue);

            return new DispositivoTopologiaViewModel
            {
                Identificador = equipo.Identificador,
                Nombre = equipo.Nombre,
                TipoDispositivo = equipo.TipoDispositivo,
                Modelo = equipo.Modelo,
                Area = equipo.Area,
                RolEnArea = equipo.RolEnArea,
                IpAdministracion = equipo.IpAdministracion,
                Icono = CatalogoVisualesDispositivos.ObtenerIcono(equipo.TipoDispositivo, perfilVisual),
                FuentePerfil = perfilVisual?.Fuente,
                EstadoCapacidad = !esSwitch ? "No aplica"
                    : perfilVisual is not null && hayCapacidadManual ? "Perfil verificado con ajustes manuales"
                    : perfilVisual is not null ? "Perfil de modelo verificado"
                    : hayCapacidadManual ? capacidadCompleta ? "Capacidad manual completa" : "Capacidad manual parcial"
                    : "Capacidad sin documentar",
                EsSwitch = esSwitch,
                Puertos = puertosDibujados
            };
        }).ToList();

        // enlacesDibujables requiere que ambos puertos y equipos existan antes de construir una arista.
        var enlacesDibujables = new List<EnlaceTopologiaViewModel>();
        foreach (var enlace in enlacesRegistrados)
        {
            if (!interfacesPorId.TryGetValue(enlace.IdentificadorPuertoOrigen, out var puertoOrigen)
                || !interfacesPorId.TryGetValue(enlace.IdentificadorPuertoDestino, out var puertoDestino)
                || !equiposPorId.TryGetValue(puertoOrigen.IdentificadorEquipo, out var equipoOrigen)
                || !equiposPorId.TryGetValue(puertoDestino.IdentificadorEquipo, out var equipoDestino))
            {
                continue;
            }

            enlacesDibujables.Add(new EnlaceTopologiaViewModel
            {
                IdentificadorEquipoOrigen = equipoOrigen.Identificador,
                IdentificadorPuertoOrigen = puertoOrigen.Identificador,
                EquipoOrigen = equipoOrigen.Nombre,
                PuertoOrigen = puertoOrigen.EtiquetaPuerto,
                IdentificadorEquipoDestino = equipoDestino.Identificador,
                IdentificadorPuertoDestino = puertoDestino.Identificador,
                EquipoDestino = equipoDestino.Nombre,
                PuertoDestino = puertoDestino.EtiquetaPuerto,
                Medio = enlace.Medio,
                EstadoDatos = enlace.EstadoDatos,
                Confirmado = string.Equals(enlace.EstadoDatos, "Confirmado", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(enlace.EstadoDatos, "Vigente", StringComparison.OrdinalIgnoreCase),
                Fuente = enlace.Fuente,
                FechaUltimaRevision = enlace.FechaUltimaRevision
            });
        }

        // switchesInaccesibles mantiene el indicador administrativo previo, sin convertirlo en conectividad.
        var switchesInaccesibles = equiposRegistrados.Count(equipo =>
            equipo.TipoDispositivo == "Switch" && equipo.EstadoAdministrativo.StartsWith("Inaccesible", StringComparison.OrdinalIgnoreCase));
        var areasDocumentadas = equiposRegistrados.Select(equipo => equipo.Area)
            .Where(area => !string.IsNullOrWhiteSpace(area))
            .Select(area => area!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(area => area)
            .ToList();

        // resumenTopologia agrupa listas y conteos para la vista y sus filtros.
        var resumenTopologia = new TopologiaViewModel
        {
            TotalEquipos = equiposRegistrados.Count,
            TotalSwitches = equiposRegistrados.Count(equipo => equipo.TipoDispositivo == "Switch"),
            TotalPuestos = equiposRegistrados.Count(equipo => equipo.TipoDispositivo == "Puesto de usuario"),
            PuestosConIp = equiposRegistrados.Count(equipo => equipo.TipoDispositivo == "Puesto de usuario"
                && !string.IsNullOrWhiteSpace(equipo.IpAdministracion)),
            SwitchesInaccesibles = switchesInaccesibles,
            TotalInterfaces = interfacesRegistradas.Count,
            TotalEnlaces = enlacesDibujables.Count,
            Areas = areasDocumentadas,
            Dispositivos = dispositivosDibujados,
            Enlaces = enlacesDibujables
        };

        return View(resumenTopologia);
    }
}
