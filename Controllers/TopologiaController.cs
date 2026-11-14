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
        // enlacesRegistrados contiene relaciones explícitas entre dos puertos y determina las aristas completas.
        var enlacesRegistrados = await contextoInventario.EnlacesFisicos.AsNoTracking()
            .ToListAsync(cancelacion);
        // relacionesParciales conserva filas con un puerto local conocido y remoto opcional.
        var relacionesParciales = await (
            from relacion in contextoInventario.ConexionesDocumentadas.AsNoTracking()
            join puertoLocal in contextoInventario.InterfacesEquipo.AsNoTracking()
                on relacion.IdentificadorPuertoLocal equals puertoLocal.Identificador
            join equipoLocal in contextoInventario.Equipos.AsNoTracking()
                on puertoLocal.IdentificadorEquipo equals equipoLocal.Identificador
            orderby equipoLocal.Nombre, puertoLocal.EtiquetaPuerto
            select new ConexionParcialTopologiaViewModel
            {
                Identificador = relacion.Identificador,
                IdentificadorEquipoLocal = equipoLocal.Identificador,
                IdentificadorPuertoLocal = relacion.IdentificadorPuertoLocal,
                EquipoLocal = equipoLocal.Nombre,
                IpLocal = equipoLocal.IpAdministracion,
                PuertoLocal = puertoLocal.EtiquetaPuerto,
                EquipoRemoto = relacion.EquipoRemoto,
                IpRemota = relacion.IpRemota,
                PuertoRemoto = relacion.PuertoRemoto,
                Medio = relacion.Medio,
                EstadoDatos = string.IsNullOrWhiteSpace(relacion.FuentePagina) ? relacion.EstadoDatos : "Confirmado",
                FuentePagina = relacion.FuentePagina,
                FuenteLibro = relacion.FuenteLibro,
                FuenteFila = relacion.FuenteFila,
                Observaciones = relacion.Observaciones
            }).ToListAsync(cancelacion);
        // ResolverRelacionesParciales compara las filas recíprocas y enlaza solo destinos únicos.
        ResolverRelacionesParciales(relacionesParciales, equiposRegistrados);
        // Cada interfaz local conserva su ID; se normalizan etiquetas de HP 1920 y Dell S4148U para presentación.
        foreach (var relacionParcial in relacionesParciales)
        {
            // interfazLocal resuelve el tipo del puerto antes de presentar su numeración física.
            var interfazLocal = interfacesRegistradas.FirstOrDefault(interfaz =>
                interfaz.Identificador == relacionParcial.IdentificadorPuertoLocal);
            if (interfazLocal is not null)
                relacionParcial.PuertoLocal = CatalogoVisualesDispositivos.NormalizarEtiquetaPuertoVisual(
                    relacionParcial.EquipoLocal, relacionParcial.PuertoLocal, interfazLocal.TipoInterfaz);
        }

        // equiposPorId resuelve cada extremo de enlace a su ficha propietaria.
        var equiposPorId = equiposRegistrados.ToDictionary(equipo => equipo.Identificador);
        // interfacesPorId resuelve cada extremo guardado a su etiqueta y propietario exactos.
        var interfacesPorId = interfacesRegistradas.ToDictionary(interfaz => interfaz.Identificador);
        // puertosEnlazados permite pintar ocupado solo un puerto incluido en un enlace guardado.
        var puertosEnlazados = enlacesRegistrados
            .SelectMany(enlace => new[] { enlace.IdentificadorPuertoOrigen, enlace.IdentificadorPuertoDestino })
            .ToHashSet();
        // ConexionesDocumentadas también ocupa el puerto local aunque no se dibuje un enlace de dos extremos.
        puertosEnlazados.UnionWith(relacionesParciales.Select(relacion => relacion.IdentificadorPuertoLocal));
        // destinosPorInterfaz resume el otro extremo guardado para que el título del puerto identifique su destino.
        var destinosPorInterfaz = new Dictionary<int, string>();
        foreach (var enlace in enlacesRegistrados)
        {
            if (!interfacesPorId.TryGetValue(enlace.IdentificadorPuertoOrigen, out var interfazOrigen)
                || !interfacesPorId.TryGetValue(enlace.IdentificadorPuertoDestino, out var interfazDestino)
                || !equiposPorId.TryGetValue(interfazOrigen.IdentificadorEquipo, out var equipoOrigen)
                || !equiposPorId.TryGetValue(interfazDestino.IdentificadorEquipo, out var equipoDestino))
                continue;
            destinosPorInterfaz[interfazOrigen.Identificador] = $"{equipoDestino.Nombre} · {equipoDestino.IpAdministracion ?? "IP no indicada"} · {CatalogoVisualesDispositivos.NormalizarEtiquetaPuertoVisual(equipoDestino.Nombre, interfazDestino.EtiquetaPuerto, interfazDestino.TipoInterfaz)}";
            destinosPorInterfaz[interfazDestino.Identificador] = $"{equipoOrigen.Nombre} · {equipoOrigen.IpAdministracion ?? "IP no indicada"} · {CatalogoVisualesDispositivos.NormalizarEtiquetaPuertoVisual(equipoOrigen.Nombre, interfazOrigen.EtiquetaPuerto, interfazOrigen.TipoInterfaz)}";
        }
        foreach (var relacion in relacionesParciales)
        {
            // destinoParcial señala el extremo documentado sin inventar campos que no aparecen en el Visio.
            var destinoParcial = string.Join(" · ", new[]
            {
                relacion.EquipoRemoto, relacion.IpRemota, relacion.PuertoRemoto
            }.Where(texto => !string.IsNullOrWhiteSpace(texto)));
            if (string.IsNullOrWhiteSpace(destinoParcial))
                destinoParcial = "Extremo remoto no identificado";
            destinosPorInterfaz[relacion.IdentificadorPuertoLocal] = destinoParcial;
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

        // gruposEquiposFinales reúne endpoints Capa 3 que aparecen en puertos pero aún no tienen ficha propia.
        var gruposEquiposFinales = relacionesParciales
            .Where(relacion => relacion.CapaRelacional == "acceso"
                && !relacion.IdentificadorEquipoRemoto.HasValue
                && (!string.IsNullOrWhiteSpace(relacion.EquipoRemoto) || !string.IsNullOrWhiteSpace(relacion.IpRemota)))
            .GroupBy(relacion => $"{NormalizarEtiquetaRelacion(relacion.EquipoRemoto)}|{relacion.IpRemota?.Trim()}",
                StringComparer.OrdinalIgnoreCase);

        foreach (var grupoEquipoFinal in gruposEquiposFinales)
        {
            // relacionRepresentativa aporta el rótulo y la IP originales del grupo documental.
            var relacionRepresentativa = grupoEquipoFinal.First();
            // identificadorVisual negativo distingue el nodo dibujado de cualquier ficha guardada en SQLite.
            var identificadorVisual = -relacionRepresentativa.Identificador;
            // tipoEquipoFinal permite seleccionar el icono por la clase explícita del nombre documental.
            var tipoEquipoFinal = ClasificarTipoEquipoFinal(relacionRepresentativa.EquipoRemoto);
            // equipoLocalUbicado aporta el área del switch sin alterar ni completar el registro de destino.
            var equipoLocalUbicado = equiposRegistrados.FirstOrDefault(equipo =>
                equipo.Identificador == relacionRepresentativa.IdentificadorEquipoLocal);
            // nombreEquipoFinal conserva el nombre escrito o la IP si el rótulo falta.
            var nombreEquipoFinal = string.IsNullOrWhiteSpace(relacionRepresentativa.EquipoRemoto)
                ? relacionRepresentativa.IpRemota ?? "Equipo final documentado"
                : relacionRepresentativa.EquipoRemoto;

            dispositivosDibujados.Add(new DispositivoTopologiaViewModel
            {
                Identificador = identificadorVisual,
                Nombre = nombreEquipoFinal,
                TipoDispositivo = tipoEquipoFinal,
                Modelo = null,
                Area = equipoLocalUbicado?.Area,
                RolEnArea = tipoEquipoFinal == "PC" ? "Usuario" : "Equipo final",
                IpAdministracion = relacionRepresentativa.IpRemota,
                Icono = CatalogoVisualesDispositivos.ObtenerIcono(tipoEquipoFinal, null),
                EstadoCapacidad = "No aplica",
                EsSwitch = false,
                Puertos = []
            });

            foreach (var relacionAgrupada in grupoEquipoFinal)
            {
                // El ID solo enlaza la observación con su nodo de mapa y no crea una ficha en inventario.
                relacionAgrupada.IdentificadorEquipoRemoto = identificadorVisual;
            }
        }

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
                IpEquipoOrigen = equipoOrigen.IpAdministracion,
                PuertoOrigen = CatalogoVisualesDispositivos.NormalizarEtiquetaPuertoVisual(equipoOrigen.Nombre, puertoOrigen.EtiquetaPuerto, puertoOrigen.TipoInterfaz),
                IdentificadorEquipoDestino = equipoDestino.Identificador,
                IdentificadorPuertoDestino = puertoDestino.Identificador,
                EquipoDestino = equipoDestino.Nombre,
                IpEquipoDestino = equipoDestino.IpAdministracion,
                PuertoDestino = CatalogoVisualesDispositivos.NormalizarEtiquetaPuertoVisual(equipoDestino.Nombre, puertoDestino.EtiquetaPuerto, puertoDestino.TipoInterfaz),
                Medio = enlace.Medio,
                EstadoDatos = string.IsNullOrWhiteSpace(enlace.Fuente) ? enlace.EstadoDatos : "Confirmado",
                Confirmado = string.IsNullOrWhiteSpace(enlace.Fuente)
                    ? enlace.EstadoDatos is "Confirmado" or "Vigente"
                    : true,
                Fuente = enlace.Fuente,
                FechaUltimaRevision = enlace.FechaUltimaRevision,
                CapaRelacional = ClasificarCapaRelacion(equipoOrigen, equipoDestino)
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
            Enlaces = enlacesDibujables,
            RelacionesParciales = relacionesParciales
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
        // relacionesParcialesRegistradas incluye filas de ambos extremos para cotejar tablas recíprocas.
        var relacionesParcialesRegistradas = await contextoInventario.ConexionesDocumentadas.AsNoTracking()
            .OrderBy(relacion => relacion.IdentificadorPuertoLocal)
            .ToListAsync(cancelacion);
        // interfacesPorId resuelve cada extremo guardado sin buscar por nombre o IP.
        var interfacesPorId = await contextoInventario.InterfacesEquipo.AsNoTracking()
            .ToDictionaryAsync(interfaz => interfaz.Identificador, cancelacion);
        // equiposPorId permite presentar el nombre/IP del otro equipo que figura en el enlace.
        var equiposPorId = await contextoInventario.Equipos.AsNoTracking()
            .ToDictionaryAsync(equipo => equipo.Identificador, cancelacion);
        // relacionesParciales convierte observaciones en vistas con tipo de capa y contraparte localizada.
        var relacionesParciales = new List<ConexionParcialTopologiaViewModel>();
        foreach (var relacion in relacionesParcialesRegistradas)
        {
            if (!interfacesPorId.TryGetValue(relacion.IdentificadorPuertoLocal, out var puertoLocal)
                || !equiposPorId.TryGetValue(puertoLocal.IdentificadorEquipo, out var equipoLocal))
                continue;

            relacionesParciales.Add(new ConexionParcialTopologiaViewModel
            {
                Identificador = relacion.Identificador,
                IdentificadorEquipoLocal = equipoLocal.Identificador,
                IdentificadorPuertoLocal = puertoLocal.Identificador,
                EquipoLocal = equipoLocal.Nombre,
                IpLocal = equipoLocal.IpAdministracion,
                PuertoLocal = puertoLocal.EtiquetaPuerto,
                EquipoRemoto = relacion.EquipoRemoto,
                IpRemota = relacion.IpRemota,
                PuertoRemoto = relacion.PuertoRemoto,
                Medio = relacion.Medio,
                EstadoDatos = string.IsNullOrWhiteSpace(relacion.FuentePagina) ? relacion.EstadoDatos : "Confirmado",
                FuentePagina = relacion.FuentePagina,
                FuenteLibro = relacion.FuenteLibro,
                FuenteFila = relacion.FuenteFila,
                Observaciones = relacion.Observaciones
            });
        }
        // equiposParaResolver permite cotejar las filas con las fichas almacenadas sin alterar SQLite.
        var equiposParaResolver = equiposPorId.Values.ToList();
        ResolverRelacionesParciales(relacionesParciales, equiposParaResolver);
        // Cada interfaz local conserva su ID, pero el HP 1920 muestra SFP 25-28 como SFP 1-4.
        foreach (var relacionParcial in relacionesParciales)
        {
            // interfazLocal resuelve el tipo del puerto antes de presentar su numeración física.
            var interfazLocal = interfacesPorId.GetValueOrDefault(relacionParcial.IdentificadorPuertoLocal);
            if (interfazLocal is not null)
                relacionParcial.PuertoLocal = CatalogoVisualesDispositivos.NormalizarEtiquetaPuertoVisual(
                    relacionParcial.EquipoLocal, relacionParcial.PuertoLocal, interfazLocal.TipoInterfaz);
        }
        // conexionesParciales limita el detalle a las relaciones locales del switch actual.
        var conexionesParciales = relacionesParciales
            .Where(relacion => relacion.IdentificadorEquipoLocal == identificador)
            .ToList();

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
                Identificador = enlace.Identificador,
                IdentificadorEquipoOrigen = equipoOrigen.Identificador,
                IdentificadorPuertoOrigen = puertoOrigen.Identificador,
                EquipoOrigen = equipoOrigen.Nombre,
                PuertoOrigen = puertoOrigen.EtiquetaPuerto,
                IdentificadorEquipoDestino = equipoDestino.Identificador,
                IdentificadorPuertoDestino = puertoDestino.Identificador,
                EquipoDestino = equipoDestino.Nombre,
                PuertoDestino = puertoDestino.EtiquetaPuerto,
                Medio = enlace.Medio,
                EstadoDatos = string.IsNullOrWhiteSpace(enlace.Fuente) ? enlace.EstadoDatos : "Confirmado",
                Confirmado = string.IsNullOrWhiteSpace(enlace.Fuente)
                    ? enlace.EstadoDatos is "Confirmado" or "Vigente"
                    : true,
                Fuente = enlace.Fuente,
                FechaUltimaRevision = enlace.FechaUltimaRevision,
                CapaRelacional = ClasificarCapaRelacion(equipoOrigen, equipoDestino)
            });
        }

        // perfilVisual usa datos exactos del catálogo; sin perfil, la capacidad manual sigue respetándose.
        var perfilVisual = CatalogoVisualesDispositivos.ObtenerPerfil(equipoConsultado.Fabricante, equipoConsultado.Modelo);
        // puertosEnlazados delimita los puertos que tienen una relación física almacenada.
        var puertosEnlazados = enlacesRegistrados
            .SelectMany(enlace => new[] { enlace.IdentificadorPuertoOrigen, enlace.IdentificadorPuertoDestino })
            .ToHashSet();
        // conexionesParciales también marca ocupado el puerto local, igual que en el mapa general.
        puertosEnlazados.UnionWith(conexionesParciales.Select(conexion => conexion.IdentificadorPuertoLocal));
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
        // conexionParcial agrega solo los datos remotos que existan en su registro original.
        foreach (var conexionParcial in conexionesParciales)
        {
            // destinoDocumentado concatena únicamente nombre, IP y puerto que ya están registrados.
            var destinoDocumentado = string.Join(" · ", new[]
            {
                conexionParcial.EquipoRemoto, conexionParcial.IpRemota, conexionParcial.PuertoRemoto
            }.Where(valor => !string.IsNullOrWhiteSpace(valor)));
            if (string.IsNullOrWhiteSpace(destinoDocumentado))
                destinoDocumentado = "Extremo remoto no indicado";
            destinosPorInterfaz[conexionParcial.IdentificadorPuertoLocal] = destinoDocumentado;
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
        // hayCapacidadManual detecta cantidades aportadas por técnicos y evita confundirlas con perfil verificado.
        var hayCapacidadManual = new[]
        {
            equipoConsultado.CantidadPuertosRj45, equipoConsultado.CantidadPuertosSfp,
            equipoConsultado.CantidadPuertosSfpPlus, equipoConsultado.CantidadPuertosQsfp,
            equipoConsultado.CantidadPuertosQsfpPlus, equipoConsultado.CantidadPuertosQsfp28
        }.Any(cantidad => cantidad.HasValue);
        // capacidadCompleta señala si todas las familias de conectores tienen cantidad definida.
        var capacidadCompleta = capacidadesConocidas.All(cantidad => cantidad.HasValue);
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
            EstadoCapacidad = perfilVisual is not null && hayCapacidadManual ? "Perfil verificado con ajustes manuales"
                : perfilVisual is not null ? "Perfil de modelo verificado"
                : hayCapacidadManual ? capacidadCompleta ? "Capacidad manual completa" : "Capacidad manual parcial"
                : "Capacidad sin documentar",
            Puertos = puertosDelSwitch
        };

        return View(new DetalleTopologiaViewModel { Dispositivo = dispositivo, Enlaces = enlacesDelSwitch, ConexionesParciales = conexionesParciales });
    }
    /// <summary>
    /// Resuelve el equipo remoto por IP o etiqueta y completa pares únicamente cuando hay una contraparte única.
    /// Cambiar la lista de equipos cambia qué destinos pueden enlazarse; cambiar datos de una relación cambia su clasificación o emparejamiento.
    /// </summary>
    /// <param name="relaciones">Registros con un puerto local; añadir registros puede revelar o desambiguar una contraparte.</param>
    /// <param name="equipos">Fichas del inventario; añadir o renombrar equipos puede resolver un destino.</param>
    private static void ResolverRelacionesParciales(
        IReadOnlyList<ConexionParcialTopologiaViewModel> relaciones,
        IReadOnlyList<EquipoRed> equipos)
    {
        // relacion recorre cada fila local para resolver su equipo remoto sin completar valores ausentes.
        foreach (var relacion in relaciones)
        {
            // equipoLocal permite clasificar el vínculo desde el tipo real del nodo de origen.
            var equipoLocal = equipos.FirstOrDefault(equipo => equipo.Identificador == relacion.IdentificadorEquipoLocal);
            // equipoRemoto es una ficha única coincidente; si no es única, el enlace queda solo como nota local.
            var equipoRemoto = ResolverEquipoRelacionado(equipos, relacion.EquipoRemoto, relacion.IpRemota);
            if (equipoLocal is null)
                continue;

            if (equipoRemoto is not null)
            {
                relacion.IdentificadorEquipoRemoto = equipoRemoto.Identificador;
                relacion.CapaRelacional = ClasificarCapaRelacion(equipoLocal, equipoRemoto);
            }
            else if (NormalizarEtiquetaRelacion(relacion.EquipoRemoto).Contains("SWITCH", StringComparison.Ordinal))
            {
                relacion.CapaRelacional = "switching";
            }
            else if (!string.IsNullOrWhiteSpace(relacion.EquipoRemoto) || !string.IsNullOrWhiteSpace(relacion.IpRemota))
            {
                relacion.CapaRelacional = "acceso";
            }
        }

        // relacion recorre cada enlace entre switches para buscar una sola fila que apunte de regreso.
        foreach (var relacion in relaciones.Where(relacion => relacion.CapaRelacional == "switching"
                     && relacion.IdentificadorEquipoRemoto.HasValue))
        {
            // candidatasReciprocas contiene tablas del switch remoto cuyo destino es el switch local actual.
            var candidatasReciprocas = relaciones.Where(candidata =>
                candidata.IdentificadorEquipoLocal == relacion.IdentificadorEquipoRemoto
                && candidata.IdentificadorEquipoRemoto == relacion.IdentificadorEquipoLocal
                && candidata.Identificador != relacion.Identificador).ToList();
            if (candidatasReciprocas.Count != 1)
                continue;

            // contraparte conserva el único puerto local de la tabla remota que apunta al switch actual.
            var contraparte = candidatasReciprocas[0];
            // candidatasDeVuelta evita emparejar dos puertos locales distintos con el mismo remoto.
            var candidatasDeVuelta = relaciones.Where(candidata =>
                candidata.IdentificadorEquipoLocal == contraparte.IdentificadorEquipoRemoto
                && candidata.IdentificadorEquipoRemoto == contraparte.IdentificadorEquipoLocal
                && candidata.Identificador != contraparte.Identificador).ToList();
            if (candidatasDeVuelta.Count != 1 || candidatasDeVuelta[0].Identificador != relacion.Identificador)
                continue;

            relacion.IdentificadorRelacionReciproca = contraparte.Identificador;
            relacion.IdentificadorPuertoRemoto = contraparte.IdentificadorPuertoLocal;
            relacion.PuertoRemoto = contraparte.PuertoLocal;
        }
    }

    /// <summary>
    /// Encuentra una ficha remota usando primero una IP inequívoca y luego una etiqueta única normalizada.
    /// Cambiar nombre o IP permite resolver el enlace; si dos candidatos siguen coincidiendo, devuelve nulo.
    /// </summary>
    /// <param name="equipos">Fichas disponibles; agregar duplicados puede hacer que el resultado sea ambiguo.</param>
    /// <param name="nombreRemoto">Etiqueta escrita en el Visio; cambiarla selecciona otra ficha por nombre.</param>
    /// <param name="ipRemota">IP escrita en el Visio; cambiarla prioriza otra coincidencia exacta.</param>
    /// <returns>La ficha única encontrada o nulo si la evidencia no distingue un solo equipo.</returns>
    private static EquipoRed? ResolverEquipoRelacionado(
        IReadOnlyList<EquipoRed> equipos,
        string? nombreRemoto,
        string? ipRemota)
    {
        // coincidenciasIp agrupa coincidencias exactas sin limpiar rangos o corregir direcciones.
        var coincidenciasIp = string.IsNullOrWhiteSpace(ipRemota)
            ? new List<EquipoRed>()
            : equipos.Where(equipo => string.Equals(equipo.IpAdministracion?.Trim(), ipRemota.Trim(),
                StringComparison.OrdinalIgnoreCase)).ToList();
        // nombreNormalizado elimina espacios y signos para tolerar diferencias menores del rótulo.
        var nombreNormalizado = NormalizarEtiquetaRelacion(nombreRemoto);
        // coincidenciasNombre conserva coincidencias exactas antes de intentar una búsqueda parcial.
        var coincidenciasNombre = string.IsNullOrWhiteSpace(nombreNormalizado)
            ? new List<EquipoRed>()
            : equipos.Where(equipo => NormalizarEtiquetaRelacion(equipo.Nombre) == nombreNormalizado).ToList();

        if (coincidenciasIp.Count == 1)
        {
            // coincidenciaNombre evita asociar una IP única a una ficha con una etiqueta contradictoria.
            if (coincidenciasNombre.Count == 1
                && coincidenciasNombre[0].Identificador != coincidenciasIp[0].Identificador)
                return null;
            return coincidenciasIp[0];
        }

        if (coincidenciasIp.Count > 1)
        {
            // candidatoPorNombre resuelve IP repetida solo cuando el nombre señala uno de sus equipos.
            var candidatoPorNombre = coincidenciasNombre.SingleOrDefault();
            return candidatoPorNombre is not null
                && coincidenciasIp.Any(equipo => equipo.Identificador == candidatoPorNombre.Identificador)
                    ? candidatoPorNombre
                    : null;
        }

        if (coincidenciasNombre.Count == 1)
            return coincidenciasNombre[0];

        // coincidenciasParciales admite etiquetas breves únicamente si una sola ficha las contiene.
        var coincidenciasParciales = string.IsNullOrWhiteSpace(nombreNormalizado) || nombreNormalizado.Length < 4
            ? new List<EquipoRed>()
            : equipos.Where(equipo =>
            {
                // etiquetaNormalizada representa cada nombre inventariado en la misma forma compacta.
                var etiquetaNormalizada = NormalizarEtiquetaRelacion(equipo.Nombre);
                return etiquetaNormalizada.Contains(nombreNormalizado, StringComparison.Ordinal)
                    || nombreNormalizado.Contains(etiquetaNormalizada, StringComparison.Ordinal);
            }).ToList();
        return coincidenciasParciales.Count == 1 ? coincidenciasParciales[0] : null;
    }

    /// <summary>
    /// Clasifica un vínculo de switch a switch como switching y una relación entre switch y equipo final como acceso.
    /// Cambiar cualquiera de los tipos modifica la capa asignada al enlace.
    /// </summary>
    /// <param name="equipoOrigen">Equipo desde el que se registra la relación; su tipo afecta la clasificación.</param>
    /// <param name="equipoDestino">Equipo remoto resuelto; su tipo completa la clasificación del par.</param>
    /// <returns>El identificador físico, switching o acceso que usa el mapa.</returns>
    private static string ClasificarCapaRelacion(EquipoRed equipoOrigen, EquipoRed equipoDestino)
    {
        // origenEsSwitch identifica si el primer extremo es un switch.
        var origenEsSwitch = string.Equals(equipoOrigen.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase);
        // destinoEsSwitch identifica si el segundo extremo es un switch.
        var destinoEsSwitch = string.Equals(equipoDestino.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase);
        return origenEsSwitch && destinoEsSwitch ? "switching"
            : origenEsSwitch || destinoEsSwitch ? "acceso"
            : "fisica";
    }

    /// <summary>
    /// Normaliza una etiqueta para comparar solo sus letras y números.
    /// Cambiar el texto cambia únicamente su forma de comparación, no la etiqueta guardada.
    /// </summary>
    /// <param name="etiqueta">Nombre o etiqueta de origen; cambiarlo puede alterar coincidencias de resolución.</param>
    /// <returns>La etiqueta compactada en mayúsculas.</returns>
    /// <summary>
    /// Clasifica el icono de un endpoint usando únicamente palabras explícitas del rótulo.
    /// Cambiar la etiqueta puede seleccionar otra imagen; no modifica el inventario.
    /// </summary>
    /// <param name="etiquetaEquipo">Nombre de Visio; cambiarlo puede alterar la familia visual inferida.</param>
    /// <returns>Familia de icono; los rótulos de usuario sin tipo específico se muestran como PC.</returns>
    private static string ClasificarTipoEquipoFinal(string? etiquetaEquipo)
    {
        // etiquetaNormalizada deja la comparación inmune a mayúsculas, acentos y signos.
        var etiquetaNormalizada = NormalizarEtiquetaRelacion(etiquetaEquipo);

        if (etiquetaNormalizada.Contains("IMPRESORA", StringComparison.Ordinal)
            || etiquetaNormalizada.Contains("PRINTER", StringComparison.Ordinal))
            return "Impresora";
        if (etiquetaNormalizada.Contains("SERVIDOR", StringComparison.Ordinal)
            || etiquetaNormalizada.Contains("SERVER", StringComparison.Ordinal)
            || etiquetaNormalizada.Contains("R770", StringComparison.Ordinal)
            || etiquetaNormalizada.Contains("R920", StringComparison.Ordinal)
            || etiquetaNormalizada.Contains("BACKUP", StringComparison.Ordinal)
            || etiquetaNormalizada.Contains("VIRTUAL", StringComparison.Ordinal))
            return "Servidor";
        if (etiquetaNormalizada.Contains("PFSENSE", StringComparison.Ordinal)
            || etiquetaNormalizada.Contains("FIREWALL", StringComparison.Ordinal))
            return "Firewall";
        if (etiquetaNormalizada.Contains("ROUTER", StringComparison.Ordinal)
            || etiquetaNormalizada.Contains("CISCO", StringComparison.Ordinal))
            return "Router";
        if (etiquetaNormalizada.Contains("UPS", StringComparison.Ordinal)
            || etiquetaNormalizada.Contains("EATON", StringComparison.Ordinal))
            return "UPS";
        return "PC";
    }

    private static string NormalizarEtiquetaRelacion(string? etiqueta)
    {
        // claveNormalizada quita espacios y signos para comparar rótulos con diferencias menores de formato.
        var claveNormalizada = string.Concat((etiqueta ?? string.Empty).Where(char.IsLetterOrDigit)).ToUpperInvariant();
        return claveNormalizada;
    }

}
