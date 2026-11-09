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
        // destinosPorInterfaz resume el otro extremo guardado para que el título del puerto identifique su destino.
        var destinosPorInterfaz = new Dictionary<int, string>();
        foreach (var enlace in enlacesRegistrados)
        {
            if (!interfacesPorId.TryGetValue(enlace.IdentificadorPuertoOrigen, out var interfazOrigen)
                || !interfacesPorId.TryGetValue(enlace.IdentificadorPuertoDestino, out var interfazDestino)
                || !equiposPorId.TryGetValue(interfazOrigen.IdentificadorEquipo, out var equipoOrigen)
                || !equiposPorId.TryGetValue(interfazDestino.IdentificadorEquipo, out var equipoDestino))
                continue;
            destinosPorInterfaz[interfazOrigen.Identificador] = $"{equipoDestino.Nombre} · {interfazDestino.EtiquetaPuerto}";
            destinosPorInterfaz[interfazDestino.Identificador] = $"{equipoOrigen.Nombre} · {interfazOrigen.EtiquetaPuerto}";
        }
        // interfacesPorEquipo agrupa los puertos reales; cambiar el agrupamiento altera cada frontal.
        var interfacesPorEquipo = interfacesRegistradas.ToLookup(interfaz => interfaz.IdentificadorEquipo);

        // dispositivosDibujados genera un nodo por equipo y conserva los puertos sin conexión como desconocidos.
        var dispositivosDibujados = equiposRegistrados.Select(equipo =>
        {
            var esSwitch = string.Equals(equipo.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase);
            var perfilVisual = esSwitch ? CatalogoVisualesDispositivos.ObtenerPerfil(equipo.Fabricante, equipo.Modelo) : null;
            var puertosDelEquipo = interfacesPorEquipo[equipo.Identificador].ToList();
            var puertosDibujados = CatalogoVisualesDispositivos.CrearPuertos(equipo, puertosDelEquipo, puertosEnlazados, destinosPorInterfaz);
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

    /// <summary>
    /// Presenta el frontal y los enlaces guardados de un switch, sin consultar la red real.
    /// </summary>
    /// <param name="identificador">ID del switch; cambiarlo selecciona otra ficha documentada.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene las consultas pendientes.</param>
    /// <returns>Detalle de switch o respuesta 404 cuando la ficha no existe/no es switch.</returns>
    [HttpGet]
    public async Task<IActionResult> Detalle(int identificador, CancellationToken cancelacion)
    {
        // equipoConsultado limita la ruta a switches existentes y evita presentar otro tipo como frontal.
        var equipoConsultado = await contextoInventario.Equipos.AsNoTracking()
            .SingleOrDefaultAsync(equipo => equipo.Identificador == identificador
                && equipo.TipoDispositivo == "Switch", cancelacion);
        if (equipoConsultado is null) return NotFound();

        // interfacesDelSwitch conserva solo las posiciones inventariadas para esta ficha.
        var interfacesDelSwitch = await contextoInventario.InterfacesEquipo.AsNoTracking()
            .Where(interfaz => interfaz.IdentificadorEquipo == identificador)
            .OrderBy(interfaz => interfaz.EtiquetaPuerto)
            .ToListAsync(cancelacion);
        // enlacesRegistrados permite resolver ambos extremos y listar solo enlaces de esta ficha.
        var enlacesRegistrados = await contextoInventario.EnlacesFisicos.AsNoTracking().ToListAsync(cancelacion);
        // interfacesPorId resuelve cada extremo guardado sin buscar por nombre o IP.
        var interfacesPorId = await contextoInventario.InterfacesEquipo.AsNoTracking()
            .ToDictionaryAsync(interfaz => interfaz.Identificador, cancelacion);
        // equiposPorId permite presentar el nombre/IP del otro equipo que figura en el enlace.
        var equiposPorId = await contextoInventario.Equipos.AsNoTracking()
            .ToDictionaryAsync(equipo => equipo.Identificador, cancelacion);
        // enlacesDelSwitch convierte solo sus enlaces explícitos en filas legibles.
        var enlacesDelSwitch = new List<EnlaceTopologiaViewModel>();
        foreach (var enlace in enlacesRegistrados.Where(enlace =>
            interfacesPorId.TryGetValue(enlace.IdentificadorPuertoOrigen, out var interfazOrigen)
                && interfazOrigen.IdentificadorEquipo == identificador
            || interfacesPorId.TryGetValue(enlace.IdentificadorPuertoDestino, out var interfazDestino)
                && interfazDestino.IdentificadorEquipo == identificador))
        {
            if (!interfacesPorId.TryGetValue(enlace.IdentificadorPuertoOrigen, out var puertoOrigen)
                || !interfacesPorId.TryGetValue(enlace.IdentificadorPuertoDestino, out var puertoDestino)
                || !equiposPorId.TryGetValue(puertoOrigen.IdentificadorEquipo, out var equipoOrigen)
                || !equiposPorId.TryGetValue(puertoDestino.IdentificadorEquipo, out var equipoDestino))
                continue;
            enlacesDelSwitch.Add(new EnlaceTopologiaViewModel
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
                Confirmado = enlace.EstadoDatos is "Confirmado" or "Vigente",
                Fuente = enlace.Fuente,
                FechaUltimaRevision = enlace.FechaUltimaRevision
            });
        }

        // perfilVisual usa datos exactos del catálogo; sin perfil, la capacidad manual sigue respetándose.
        var perfilVisual = CatalogoVisualesDispositivos.ObtenerPerfil(equipoConsultado.Fabricante, equipoConsultado.Modelo);
        // puertosEnlazados delimita los puertos que tienen una relación física almacenada.
        var puertosEnlazados = enlacesRegistrados
            .SelectMany(enlace => new[] { enlace.IdentificadorPuertoOrigen, enlace.IdentificadorPuertoDestino })
            .ToHashSet();
        // destinosPorInterfaz expone el extremo opuesto sin completar medio o puertos faltantes.
        var destinosPorInterfaz = new Dictionary<int, string>();
        foreach (var enlace in enlacesDelSwitch)
        {
            if (!interfacesPorId.TryGetValue(enlace.IdentificadorPuertoOrigen, out var puertoOrigen)
                || !interfacesPorId.TryGetValue(enlace.IdentificadorPuertoDestino, out var puertoDestino))
                continue;
            destinosPorInterfaz[puertoOrigen.Identificador] = $"{enlace.EquipoDestino} · {enlace.PuertoDestino}";
            destinosPorInterfaz[puertoDestino.Identificador] = $"{enlace.EquipoOrigen} · {enlace.PuertoOrigen}";
        }
        // capacidadesConocidas permite informar la fuente del conteo sin inventar puertos.
        var capacidadesConocidas = new[]
        {
            equipoConsultado.CantidadPuertosRj45 ?? perfilVisual?.CantidadRj45,
            equipoConsultado.CantidadPuertosSfp ?? perfilVisual?.CantidadSfp,
            equipoConsultado.CantidadPuertosSfpPlus ?? perfilVisual?.CantidadSfpPlus,
            equipoConsultado.CantidadPuertosQsfp ?? perfilVisual?.CantidadQsfp,
            equipoConsultado.CantidadPuertosQsfpPlus ?? perfilVisual?.CantidadQsfpPlus,
            equipoConsultado.CantidadPuertosQsfp28 ?? perfilVisual?.CantidadQsfp28
        };
        // puertosDelSwitch usa capacidad y datos guardados para los mismos estados del mapa general.
        var puertosDelSwitch = CatalogoVisualesDispositivos.CrearPuertos(equipoConsultado, interfacesDelSwitch,
            puertosEnlazados, destinosPorInterfaz);
        var dispositivo = new DispositivoTopologiaViewModel
        {
            Identificador = equipoConsultado.Identificador,
            Nombre = equipoConsultado.Nombre,
            TipoDispositivo = equipoConsultado.TipoDispositivo,
            Modelo = equipoConsultado.Modelo,
            Area = equipoConsultado.Area,
            RolEnArea = equipoConsultado.RolEnArea,
            IpAdministracion = equipoConsultado.IpAdministracion,
            EsSwitch = true,
            Icono = CatalogoVisualesDispositivos.ObtenerIcono("Switch", perfilVisual),
            FuentePerfil = perfilVisual?.Fuente,
            EstadoCapacidad = capacidadesConocidas.Any(cantidad => cantidad.HasValue)
                ? perfilVisual is null ? "Capacidad manual documentada" : "Perfil de modelo verificado"
                : "Capacidad sin documentar",
            Puertos = puertosDelSwitch
        };

        return View(new DetalleTopologiaViewModel { Dispositivo = dispositivo, Enlaces = enlacesDelSwitch });
    }
}
