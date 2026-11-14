using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;
using ProyectoConexionRed.Web.Services;

namespace ProyectoConexionRed.Web.Controllers;

/// <summary>
/// Permite registrar interfaces y enlaces documentales sin interactuar con la red real.
/// </summary>
public class ConexionesController : Controller
{
    // contextoInventario permite guardar interfaces, enlaces y su historial; cambiarlo selecciona otra base documental.
    private readonly InventarioDbContext contextoInventario;

    /// <summary>
    /// Prepara las consultas y escrituras de puertos y enlaces.
    /// </summary>
    /// <param name="contexto">Contexto de datos; cambiarlo selecciona otra base documental.</param>
    public ConexionesController(InventarioDbContext contexto)
    {
        contextoInventario = contexto;
    }

    /// <summary>
    /// Muestra las interfaces y enlaces que ya fueron registrados.
    /// </summary>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene las consultas pendientes.</param>
    /// <returns>Listados documentales de puertos y conexiones.</returns>
    /// <summary>
    /// Muestra los switches disponibles, sus puertos inventariados y los destinos documentados.
    /// </summary>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la consulta del inventario.</param>
    /// <returns>Tarjetas de switches con acceso a su frontal editable.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancelacion)
    {
        // equiposRegistrados aporta identidades exactas para resolver propietarios y extremos remotos.
        var equiposRegistrados = await contextoInventario.Equipos.AsNoTracking()
            .OrderBy(equipo => equipo.Area).ThenBy(equipo => equipo.Nombre)
            .ToListAsync(cancelacion);
        // interfacesRegistradas conserva los puertos inventariados y sus estados de uso.
        var interfacesRegistradas = await contextoInventario.InterfacesEquipo.AsNoTracking()
            .OrderBy(interfaz => interfaz.EtiquetaPuerto)
            .ToListAsync(cancelacion);
        // enlacesRegistrados contiene relaciones físicas con dos puertos existentes.
        var enlacesRegistrados = await contextoInventario.EnlacesFisicos.AsNoTracking().ToListAsync(cancelacion);
        // relacionesParciales contiene conexiones que solo guardan un puerto local.
        var relacionesParciales = await contextoInventario.ConexionesDocumentadas.AsNoTracking().ToListAsync(cancelacion);
        // interfacesPorId resuelve puertos sin comparar etiquetas posiblemente repetidas.
        var interfacesPorId = interfacesRegistradas.ToDictionary(interfaz => interfaz.Identificador);
        // equiposPorId convierte el propietario del puerto remoto a una ficha visible.
        var equiposPorId = equiposRegistrados.ToDictionary(equipo => equipo.Identificador);
        // identificadoresPuertosOcupados registra extremos con algún enlace o relación parcial.
        var identificadoresPuertosOcupados = enlacesRegistrados
            .SelectMany(enlace => new[] { enlace.IdentificadorPuertoOrigen, enlace.IdentificadorPuertoDestino })
            .ToHashSet();
        identificadoresPuertosOcupados.UnionWith(relacionesParciales.Select(relacion => relacion.IdentificadorPuertoLocal));

        // switchesDisponibles conserva solo equipos clasificados con el tipo Switch.
        var switchesDisponibles = new List<SwitchPuertosResumenViewModel>();
        // equipo recorre los switches y construye sus conteos desde las interfaces persistidas.
        foreach (var equipo in equiposRegistrados.Where(equipo =>
            string.Equals(equipo.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase)))
        {
            // puertosDelSwitch contiene las interfaces propiedad de esta ficha.
            var puertosDelSwitch = interfacesRegistradas
                .Where(interfaz => interfaz.IdentificadorEquipo == equipo.Identificador)
                .ToList();
            // puertosLibres cuenta solo interfaces libres cuya vigencia fue confirmada.
            var puertosLibres = puertosDelSwitch.Count(interfaz =>
                !identificadoresPuertosOcupados.Contains(interfaz.Identificador)
                && string.Equals(interfaz.EstadoUso, "Libre confirmado", StringComparison.OrdinalIgnoreCase)
                && string.Equals(interfaz.EstadoDatos, "Confirmado", StringComparison.OrdinalIgnoreCase));
            // puertosOcupados cuenta relaciones completas o parciales guardadas en estos puertos.
            var puertosOcupados = puertosDelSwitch.Count(interfaz =>
                identificadoresPuertosOcupados.Contains(interfaz.Identificador));
            // destinosDocumentados resume los extremos remotos expresos sin completar datos ausentes.
            var destinosDocumentados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // relacion recorre conexiones cuyo puerto local pertenece al switch actual.
            foreach (var relacion in relacionesParciales)
            {
                if (!interfacesPorId.TryGetValue(relacion.IdentificadorPuertoLocal, out var interfazLocal)
                    || interfazLocal.IdentificadorEquipo != equipo.Identificador)
                {
                    continue;
                }

                // destinoParcial deja explícito cuando no hay nombre ni IP remota registrada.
                var destinoParcial = string.Join(" · ", new[]
                {
                    relacion.EquipoRemoto, relacion.IpRemota
                }.Where(valor => !string.IsNullOrWhiteSpace(valor)));
                destinosDocumentados.Add(string.IsNullOrWhiteSpace(destinoParcial)
                    ? "Equipo remoto no identificado"
                    : destinoParcial);
            }

            // enlace recorre pares completos para agregar el extremo opuesto cuando uno pertenece a este switch.
            foreach (var enlace in enlacesRegistrados)
            {
                if (!interfacesPorId.TryGetValue(enlace.IdentificadorPuertoOrigen, out var interfazOrigen)
                    || !interfacesPorId.TryGetValue(enlace.IdentificadorPuertoDestino, out var interfazDestino))
                {
                    continue;
                }

                if (interfazOrigen.IdentificadorEquipo == equipo.Identificador
                    && equiposPorId.TryGetValue(interfazDestino.IdentificadorEquipo, out var equipoDestino))
                {
                    destinosDocumentados.Add($"{equipoDestino.Nombre} · {equipoDestino.IpAdministracion ?? "IP no indicada"}");
                }
                else if (interfazDestino.IdentificadorEquipo == equipo.Identificador
                    && equiposPorId.TryGetValue(interfazOrigen.IdentificadorEquipo, out var equipoOrigen))
                {
                    destinosDocumentados.Add($"{equipoOrigen.Nombre} · {equipoOrigen.IpAdministracion ?? "IP no indicada"}");
                }
            }

            // puertosDesconocidos excluye ocupados y libres confirmados del total inventariado.
            var puertosDesconocidos = puertosDelSwitch.Count - puertosOcupados - puertosLibres;
            switchesDisponibles.Add(new SwitchPuertosResumenViewModel
            {
                Identificador = equipo.Identificador,
                Nombre = equipo.Nombre,
                Area = equipo.Area,
                Modelo = equipo.Modelo,
                IpAdministracion = equipo.IpAdministracion,
                TotalPuertos = puertosDelSwitch.Count,
                PuertosOcupados = puertosOcupados,
                PuertosLibres = puertosLibres,
                PuertosDesconocidos = puertosDesconocidos,
                DestinosDocumentados = destinosDocumentados.OrderBy(destino => destino).ToList()
            });
        }

        // modeloConexiones entrega la colección de switches a la vista operativa de puertos.
        var modeloConexiones = new ConexionesIndexViewModel
        {
            Switches = switchesDisponibles
        };

        return View(modeloConexiones);
    }

    /// <summary>
    /// Presenta el formulario para documentar un puerto de un equipo inventariado.
    /// </summary>
    /// <param name="identificadorEquipo">Equipo opcional para preseleccionar; cambiarlo modifica la selección inicial.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la carga de equipos.</param>
    /// <returns>Formulario de interfaz con los equipos disponibles.</returns>
    [HttpGet]
    public async Task<IActionResult> CrearInterfaz(int? identificadorEquipo, CancellationToken cancelacion)
    {
        // formularioInterfaz conserva el equipo preseleccionado; cambiarlo modifica el valor inicial del selector.
        var formularioInterfaz = new NuevaInterfazViewModel
        {
            IdentificadorEquipo = identificadorEquipo ?? 0,
            EquiposDisponibles = await UtilidadesConexiones.ObtenerEquiposDisponiblesAsync(contextoInventario, identificadorEquipo, cancelacion)
        };

        return View(formularioInterfaz);
    }

    /// <summary>
    /// Guarda una interfaz documental y deja constancia del alta en el historial.
    /// </summary>
    /// <param name="formulario">Datos del puerto; cambiarlos modifica solo la ficha documental guardada.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la validación o escritura.</param>
    /// <returns>Redirección al frontal del switch propietario o a Puertos; formulario con errores si la validación falla.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearInterfaz(
        [Bind("IdentificadorEquipo,EtiquetaPuerto,TipoInterfaz,EstadoDatos,EstadoUso,FechaUltimaRevision,Observaciones")]
        NuevaInterfazViewModel formulario,
        CancellationToken cancelacion)
    {
        // etiquetaPuertoNormalizada recorta espacios externos; cambiar esta regla modifica la etiqueta guardada.
        var etiquetaPuertoNormalizada = formulario.EtiquetaPuerto.Trim();

        if (formulario.EstadoUso == "Libre confirmado" && formulario.EstadoDatos != "Confirmado")
            ModelState.AddModelError(nameof(formulario.EstadoUso), "Confirma también la vigencia del puerto antes de marcarlo libre.");

        if (formulario.IdentificadorEquipo <= 0
            || !await contextoInventario.Equipos.AnyAsync(
                equipo => equipo.Identificador == formulario.IdentificadorEquipo,
                cancelacion))
        {
            ModelState.AddModelError(nameof(formulario.IdentificadorEquipo), "Selecciona un equipo existente.");
        }

        if (await contextoInventario.InterfacesEquipo.AnyAsync(
                interfaz => interfaz.IdentificadorEquipo == formulario.IdentificadorEquipo
                    && interfaz.EtiquetaPuerto == etiquetaPuertoNormalizada,
                cancelacion))
        {
            ModelState.AddModelError(nameof(formulario.EtiquetaPuerto), "Ese puerto ya está registrado para el equipo.");
        }

        if (!ModelState.IsValid)
        {
            formulario.EquiposDisponibles = await UtilidadesConexiones.ObtenerEquiposDisponiblesAsync(contextoInventario, 
                formulario.IdentificadorEquipo,
                cancelacion);
            return View(formulario);
        }

        // equipoPropietario identifica el registro al que se atribuye el alta; cambiarlo cambia su referencia histórica.
        var equipoPropietario = await contextoInventario.Equipos
            .SingleAsync(equipo => equipo.Identificador == formulario.IdentificadorEquipo, cancelacion);

        // nuevaInterfaz contiene únicamente los datos del puerto; cambiar sus valores modifica el inventario, no el equipo real.
        var nuevaInterfaz = new InterfazEquipo
        {
            IdentificadorEquipo = equipoPropietario.Identificador,
            EtiquetaPuerto = etiquetaPuertoNormalizada,
            TipoInterfaz = formulario.TipoInterfaz,
            EstadoDatos = formulario.EstadoDatos,
            EstadoUso = formulario.EstadoUso,
            FechaUltimaRevision = formulario.FechaUltimaRevision,
            Observaciones = UtilidadesConexiones.NormalizarTexto(formulario.Observaciones)
        };

        // eventoCambio registra el alta del puerto; cambiar sus datos altera la línea que queda en el historial.
        var eventoCambio = new EventoModificacion
        {
            IdentificadorEquipo = equipoPropietario.Identificador,
            NombreEquipo = equipoPropietario.Nombre,
            NombreUsuario = ObtenerUsuarioEditor(),
            FechaUtc = DateTime.UtcNow,
            Campo = "Interfaz agregada",
            ValorNuevo = $"{nuevaInterfaz.EtiquetaPuerto} · {nuevaInterfaz.TipoInterfaz} · {nuevaInterfaz.EstadoDatos}"
        };

        await contextoInventario.InterfacesEquipo.AddAsync(nuevaInterfaz, cancelacion);
        await contextoInventario.EventosModificacion.AddAsync(eventoCambio, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);

        TempData["Mensaje"] = $"Se registró el puerto {nuevaInterfaz.EtiquetaPuerto} de {equipoPropietario.Nombre}.";
        return string.Equals(equipoPropietario.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction("Detalle", "Topologia", new { identificador = equipoPropietario.Identificador })
            : RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Presenta un formulario para registrar una conexión cuyo extremo remoto puede faltar.
    /// </summary>
    /// <param name="identificadorPuertoLocal">Puerto elegido desde el frontal; cambiarlo preselecciona otro extremo local.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la consulta de puertos disponibles.</param>
    /// <returns>Formulario con interfaces locales libres en la documentación.</returns>
    [HttpGet]
    public async Task<IActionResult> CrearRelacion(
        int? identificadorPuertoLocal,
        int? identificadorEquipoRemoto,
        CancellationToken cancelacion)
    {
        // interfacesLibres excluye relaciones existentes para evitar doble asignación documental.
        var interfacesLibres = await (
            from interfaz in contextoInventario.InterfacesEquipo.AsNoTracking()
            join equipo in contextoInventario.Equipos.AsNoTracking()
                on interfaz.IdentificadorEquipo equals equipo.Identificador
            where !contextoInventario.EnlacesFisicos.Any(enlace =>
                    enlace.IdentificadorPuertoOrigen == interfaz.Identificador
                    || enlace.IdentificadorPuertoDestino == interfaz.Identificador)
                && !contextoInventario.ConexionesDocumentadas.Any(relacion =>
                    relacion.IdentificadorPuertoLocal == interfaz.Identificador)
            orderby equipo.Nombre, interfaz.EtiquetaPuerto
            select new
            {
                interfaz.Identificador,
                interfaz.EtiquetaPuerto,
                IdEquipo = equipo.Identificador,
                equipo.TipoDispositivo,
                equipo.Nombre,
                equipo.IpAdministracion
            })
            .ToListAsync(cancelacion);
        // identificadorPreseleccionado conserva el puerto recibido solo si sigue disponible.
        var identificadorPreseleccionado = identificadorPuertoLocal.HasValue
            && interfacesLibres.Any(interfaz => interfaz.Identificador == identificadorPuertoLocal.Value)
                ? identificadorPuertoLocal.Value
                : 0;
        // identificadorEquipoLocal permite excluir el equipo del switch de la lista de destinos remotos.
        var identificadorEquipoLocal = interfacesLibres
            .Where(interfaz => interfaz.Identificador == identificadorPreseleccionado)
            .Select(interfaz => (int?)interfaz.IdEquipo)
            .FirstOrDefault();
        // equiposRemotos contiene fichas no switch para conectar equipos finales desde ambos sentidos.
        var equiposRemotos = await ObtenerEquiposRemotosAsync(identificadorEquipoLocal, cancelacion);
        // equipoRemotoSeleccionado precarga solo el destino pedido si está disponible como equipo final.
        var equipoRemotoSeleccionado = equiposRemotos.FirstOrDefault(equipo =>
            equipo.Identificador == identificadorEquipoRemoto);
        // formularioRelacion conserva el extremo preseleccionado y las opciones libres del selector.
        var formularioRelacion = new NuevaConexionDocumentadaViewModel
        {
            IdentificadorPuertoLocal = identificadorPreseleccionado,
            IdentificadorEquipoRemoto = equipoRemotoSeleccionado?.Identificador,
            EquipoRemoto = equipoRemotoSeleccionado?.Nombre,
            IpRemota = equipoRemotoSeleccionado?.IpAdministracion,
            EquiposRemotosDisponibles = equiposRemotos.Select(equipo => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = equipo.Identificador.ToString(),
                Text = string.Join(" · ", new[] { equipo.Nombre, equipo.IpAdministracion, equipo.TipoDispositivo }
                    .Where(texto => !string.IsNullOrWhiteSpace(texto)))
            }).ToList(),
            IdentificadorEquipoRetorno = interfacesLibres
                .Where(interfaz => interfaz.Identificador == identificadorPreseleccionado)
                .Select(interfaz => string.Equals(interfaz.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase)
                    ? interfaz.IdEquipo
                    : 0)
                .FirstOrDefault(),
            InterfacesDisponibles = interfacesLibres.Select(interfaz => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = interfaz.Identificador.ToString(),
                Text = string.Join(" · ", new[] { interfaz.Nombre, interfaz.IpAdministracion, interfaz.EtiquetaPuerto }
                    .Where(texto => !string.IsNullOrWhiteSpace(texto)))
            }).ToList()
        };
        return View(formularioRelacion);
    }

    /// <summary>
    /// Guarda la conexión observada y su procedencia, sin inventar el puerto remoto.
    /// </summary>
    /// <param name="formulario">Puerto local, destino opcional y fuente; cambiarlos modifica el registro documental.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene las validaciones y escrituras.</param>
    /// <returns>Redirección al frontal del switch propietario o a Puertos; formulario con errores si la validación falla.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearRelacion(
        [Bind("IdentificadorPuertoLocal,IdentificadorEquipoRetorno,IdentificadorEquipoRemoto,EquipoRemoto,IpRemota,PuertoRemoto,Medio,EstadoDatos,FechaUltimaRevision,FuentePagina,FuenteLibro,FuenteFila,Observaciones")]
        NuevaConexionDocumentadaViewModel formulario,
        CancellationToken cancelacion)
    {
        // interfazLocal verifica que el extremo conocido exista antes de persistir la relación.
        var interfazLocal = await (
            from interfaz in contextoInventario.InterfacesEquipo.AsNoTracking()
            join equipo in contextoInventario.Equipos.AsNoTracking()
                on interfaz.IdentificadorEquipo equals equipo.Identificador
            where interfaz.Identificador == formulario.IdentificadorPuertoLocal
            select new
            {
                interfaz.Identificador,
                interfaz.IdentificadorEquipo,
                interfaz.EtiquetaPuerto,
                interfaz.EstadoUso,
                equipo.Nombre,
                equipo.TipoDispositivo
            })
            .SingleOrDefaultAsync(cancelacion);
        if (interfazLocal is null)
            ModelState.AddModelError(nameof(formulario.IdentificadorPuertoLocal), "Selecciona un puerto local existente.");

        // puertoYaVinculado evita superponer la observación con un enlace o registro parcial existente.
        var puertoYaVinculado = await contextoInventario.EnlacesFisicos.AnyAsync(enlace =>
            enlace.IdentificadorPuertoOrigen == formulario.IdentificadorPuertoLocal
            || enlace.IdentificadorPuertoDestino == formulario.IdentificadorPuertoLocal, cancelacion)
            || await contextoInventario.ConexionesDocumentadas.AnyAsync(relacion =>
                relacion.IdentificadorPuertoLocal == formulario.IdentificadorPuertoLocal, cancelacion);
        if (puertoYaVinculado)
            ModelState.AddModelError(nameof(formulario.IdentificadorPuertoLocal), "Ese puerto ya tiene una relación registrada.");

        // equipoRemotoSeleccionado resuelve la opción por ID y nunca confía en el nombre enviado por el navegador.
        var equipoRemotoSeleccionado = formulario.IdentificadorEquipoRemoto.HasValue
            ? await contextoInventario.Equipos.AsNoTracking().SingleOrDefaultAsync(
                equipo => equipo.Identificador == formulario.IdentificadorEquipoRemoto.Value,
                cancelacion)
            : null;
        if (formulario.IdentificadorEquipoRemoto.HasValue && equipoRemotoSeleccionado is null)
            ModelState.AddModelError(nameof(formulario.IdentificadorEquipoRemoto), "Selecciona un equipo remoto existente.");
        if (equipoRemotoSeleccionado is not null
            && string.Equals(equipoRemotoSeleccionado.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(formulario.IdentificadorEquipoRemoto), "Para enlazar dos switches registra ambos puertos en la opción de enlace físico.");
        if (equipoRemotoSeleccionado is not null && interfazLocal is not null
            && equipoRemotoSeleccionado.Identificador == interfazLocal.IdentificadorEquipo)
            ModelState.AddModelError(nameof(formulario.IdentificadorEquipoRemoto), "El equipo remoto debe ser distinto del switch local.");
        if (equipoRemotoSeleccionado is not null
            && !string.Equals(equipoRemotoSeleccionado.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase))
        {
            formulario.EquipoRemoto = equipoRemotoSeleccionado.Nombre;
            formulario.IpRemota = equipoRemotoSeleccionado.IpAdministracion;
            ModelState.Remove(nameof(formulario.EquipoRemoto));
            ModelState.Remove(nameof(formulario.IpRemota));
        }

        if (!ModelState.IsValid)
        {
            formulario.IdentificadorEquipoRetorno = interfazLocal is not null
                && string.Equals(interfazLocal.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase)
                    ? interfazLocal.IdentificadorEquipo
                    : 0;
            formulario.InterfacesDisponibles = await ObtenerPuertosSinRelacionAsync(cancelacion);
            formulario.EquiposRemotosDisponibles = await ObtenerEquiposRemotosAsync(
                interfazLocal?.IdentificadorEquipo, cancelacion);
            return View(formulario);
        }

        // nuevaRelacion mantiene opcionales los datos del extremo remoto y registra la fuente por separado.
        var nuevaRelacion = new ConexionDocumentada
        {
            IdentificadorPuertoLocal = formulario.IdentificadorPuertoLocal,
            EquipoRemoto = UtilidadesConexiones.NormalizarTexto(formulario.EquipoRemoto),
            IpRemota = UtilidadesConexiones.NormalizarTexto(formulario.IpRemota),
            PuertoRemoto = UtilidadesConexiones.NormalizarTexto(formulario.PuertoRemoto),
            Medio = formulario.Medio,
            EstadoDatos = formulario.EstadoDatos,
            FechaUltimaRevision = formulario.FechaUltimaRevision,
            FuentePagina = formulario.FuentePagina.Trim(),
            FuenteLibro = UtilidadesConexiones.NormalizarTexto(formulario.FuenteLibro),
            FuenteFila = UtilidadesConexiones.NormalizarTexto(formulario.FuenteFila),
            Observaciones = UtilidadesConexiones.NormalizarTexto(formulario.Observaciones)
        };
        // descripcionDestino deja explícito si el equipo remoto no aparece en la fuente.
        var descripcionDestino = string.Join(" · ", new[]
        {
            nuevaRelacion.EquipoRemoto, nuevaRelacion.IpRemota, nuevaRelacion.PuertoRemoto
        }.Where(texto => !string.IsNullOrWhiteSpace(texto)));
        if (string.IsNullOrWhiteSpace(descripcionDestino))
            descripcionDestino = "Extremo remoto no identificado";
        // interfazPersistida permite convertir un estado libre anterior en ocupado por la nueva relación.
        var interfazPersistida = await contextoInventario.InterfacesEquipo
            .SingleAsync(interfaz => interfaz.Identificador == interfazLocal!.Identificador, cancelacion);
        // eventosRelacion agrupa la creación y cualquier cambio de estado dentro de la misma escritura.
        var eventosRelacion = new List<EventoModificacion>();
        if (interfazPersistida.EstadoUso == "Libre confirmado")
        {
            // estadoUsoAnterior conserva el dato de libre que deja de aplicar al registrar una conexión.
            var estadoUsoAnterior = interfazPersistida.EstadoUso;
            interfazPersistida.EstadoUso = "Desconocido";
            // eventoEstadoPuerto audita el cambio automático causado por la relación documental.
            eventosRelacion.Add(new EventoModificacion
            {
                IdentificadorEquipo = interfazLocal!.IdentificadorEquipo,
                NombreEquipo = interfazLocal.Nombre,
                NombreUsuario = ObtenerUsuarioEditor(),
                FechaUtc = DateTime.UtcNow,
                Campo = "Uso del puerto",
                ValorAnterior = estadoUsoAnterior,
                ValorNuevo = interfazPersistida.EstadoUso
            });
        }
        // eventoCambio atribuye el alta al equipo local y conserva la referencia que se cargó.
        var eventoCambio = new EventoModificacion
        {
            IdentificadorEquipo = interfazLocal!.IdentificadorEquipo,
            NombreEquipo = interfazLocal.Nombre,
            NombreUsuario = ObtenerUsuarioEditor(),
            FechaUtc = DateTime.UtcNow,
            Campo = "Conexión documental agregada",
            ValorNuevo = $"{interfazLocal.Nombre} · {interfazLocal.EtiquetaPuerto} → {descripcionDestino} · {nuevaRelacion.EstadoDatos} · {nuevaRelacion.FuentePagina}"
        };
        await contextoInventario.ConexionesDocumentadas.AddAsync(nuevaRelacion, cancelacion);
        eventosRelacion.Add(eventoCambio);
        await contextoInventario.EventosModificacion.AddRangeAsync(eventosRelacion, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);
        TempData["Mensaje"] = "Se registró la relación documental con su fuente. El extremo remoto sigue opcional.";
        return string.Equals(interfazLocal!.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction("Detalle", "Topologia", new { identificador = interfazLocal.IdentificadorEquipo })
            : RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Obtiene equipos finales que pueden elegirse como destino de una conexión de acceso.
    /// </summary>
    /// <param name="identificadorEquipoLocal">Equipo local opcional; cambiarlo excluye esa ficha de los destinos.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la consulta del inventario.</param>
    /// <returns>Equipos no switch ordenados para el selector remoto.</returns>
    private async Task<List<EquipoRed>> ObtenerEquiposRemotosAsync(
        int? identificadorEquipoLocal,
        CancellationToken cancelacion)
    {
        // equiposRemotos contiene fichas de usuario y otros extremos finales sin incluir switches.
        var equiposRemotos = await contextoInventario.Equipos.AsNoTracking()
            .OrderBy(equipo => equipo.Nombre)
            .ToListAsync(cancelacion);
        return equiposRemotos
            .Where(equipo => equipo.Identificador != identificadorEquipoLocal
                && !string.Equals(equipo.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Consulta puertos que aún no tienen enlace completo ni relación de un extremo.
    /// </summary>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la consulta.</param>
    /// <returns>Opciones de interfaces sin relación asignada.</returns>
    private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> ObtenerPuertosSinRelacionAsync(
        CancellationToken cancelacion)
    {
        // puertosLibres contiene solo interfaces que pueden asignarse sin duplicar otro vínculo.
        var puertosLibres = await (
            from interfaz in contextoInventario.InterfacesEquipo.AsNoTracking()
            join equipo in contextoInventario.Equipos.AsNoTracking()
                on interfaz.IdentificadorEquipo equals equipo.Identificador
            where !contextoInventario.EnlacesFisicos.Any(enlace =>
                    enlace.IdentificadorPuertoOrigen == interfaz.Identificador
                    || enlace.IdentificadorPuertoDestino == interfaz.Identificador)
                && !contextoInventario.ConexionesDocumentadas.Any(relacion =>
                    relacion.IdentificadorPuertoLocal == interfaz.Identificador)
                && interfaz.EstadoUso != "Libre confirmado"
            orderby equipo.Nombre, interfaz.EtiquetaPuerto
            select new { interfaz.Identificador, interfaz.EtiquetaPuerto, equipo.Nombre, equipo.IpAdministracion })
            .ToListAsync(cancelacion);
        return puertosLibres.Select(interfaz => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
        {
            Value = interfaz.Identificador.ToString(),
            Text = string.Join(" · ", new[] { interfaz.Nombre, interfaz.IpAdministracion, interfaz.EtiquetaPuerto }
                .Where(texto => !string.IsNullOrWhiteSpace(texto)))
        }).ToList();
    }

    /// <summary>
    /// Presenta el formulario para relacionar dos puertos ya registrados.
    /// </summary>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la carga de interfaces.</param>
    /// <returns>Formulario con interfaces disponibles para seleccionar ambos extremos.</returns>
    [HttpGet]
    public async Task<IActionResult> CrearEnlace(CancellationToken cancelacion)
    {
        // formularioEnlace contiene opciones basadas en interfaces existentes; cambiar la consulta altera ambos selectores.
        var formularioEnlace = new NuevoEnlaceViewModel
        {
            InterfacesDisponibles = await UtilidadesConexiones.ObtenerInterfacesDisponiblesAsync(contextoInventario, null, cancelacion)
        };

        return View(formularioEnlace);
    }

    /// <summary>
    /// Registra un enlace documental entre dos puertos libres y audita la operación.
    /// </summary>
    /// <param name="formulario">Extremos y datos del enlace; cambiarlos modifica la relación documental.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la validación o escritura.</param>
    /// <returns>Redirección al listado o formulario con los errores detectados.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearEnlace(
        [Bind("IdentificadorPuertoOrigen,IdentificadorPuertoDestino,Medio,ModuloOrigen,ModuloDestino,EstadoDatos,FechaUltimaRevision,Fuente,Observaciones")]
        NuevoEnlaceViewModel formulario,
        CancellationToken cancelacion)
    {
        if (formulario.IdentificadorPuertoOrigen == formulario.IdentificadorPuertoDestino)
        {
            ModelState.AddModelError(string.Empty, "El origen y el destino deben ser puertos distintos.");
        }

        // puertosExistentes confirma que ambos IDs pertenecen a interfaces guardadas; cambiar la consulta cambia la validación.
        var puertosExistentes = await contextoInventario.InterfacesEquipo
            .Where(interfaz => interfaz.Identificador == formulario.IdentificadorPuertoOrigen
                || interfaz.Identificador == formulario.IdentificadorPuertoDestino)
            .ToListAsync(cancelacion);

        if (puertosExistentes.Count != 2)
        {
            ModelState.AddModelError(string.Empty, "Selecciona dos puertos existentes.");
        }
        else if (puertosExistentes.Any(interfaz => interfaz.EstadoUso == "Libre confirmado"))
        {
            ModelState.AddModelError(string.Empty,
                "Uno de los puertos está marcado libre confirmado. Actualiza primero su uso si realmente forma parte de este enlace.");
        }

        // puertoYaVinculado impide registrar un segundo enlace sobre una interfaz que ya tiene relación física.
        var puertoYaVinculado = await contextoInventario.EnlacesFisicos.AnyAsync(
            enlace => enlace.IdentificadorPuertoOrigen == formulario.IdentificadorPuertoOrigen
                || enlace.IdentificadorPuertoDestino == formulario.IdentificadorPuertoOrigen
                || enlace.IdentificadorPuertoOrigen == formulario.IdentificadorPuertoDestino
                || enlace.IdentificadorPuertoDestino == formulario.IdentificadorPuertoDestino,
            cancelacion);

        if (puertoYaVinculado)
        {
            ModelState.AddModelError(string.Empty, "Uno de los puertos ya participa en un enlace. Revisa el registro existente.");
        }

        // puertoConRelacionParcial evita usar un puerto reservado por una observación de un solo lado.
        var puertoConRelacionParcial = await contextoInventario.ConexionesDocumentadas.AnyAsync(relacion =>
            relacion.IdentificadorPuertoLocal == formulario.IdentificadorPuertoOrigen
            || relacion.IdentificadorPuertoLocal == formulario.IdentificadorPuertoDestino, cancelacion);
        if (puertoConRelacionParcial)
            ModelState.AddModelError(string.Empty, "Uno de los puertos ya tiene una relación documental parcial.");

        if (!ModelState.IsValid)
        {
            formulario.InterfacesDisponibles = await UtilidadesConexiones.ObtenerInterfacesDisponiblesAsync(contextoInventario, 
                formulario.IdentificadorPuertoOrigen,
                cancelacion);
            return View(formulario);
        }

        // extremosEnlace relaciona cada ID con su equipo para mostrar nombres legibles en auditoría.
        var extremosEnlace = await (
            from interfaz in contextoInventario.InterfacesEquipo.AsNoTracking()
            join equipo in contextoInventario.Equipos.AsNoTracking()
                on interfaz.IdentificadorEquipo equals equipo.Identificador
            where interfaz.Identificador == formulario.IdentificadorPuertoOrigen
                || interfaz.Identificador == formulario.IdentificadorPuertoDestino
            select new
            {
                IdentificadorInterfaz = interfaz.Identificador,
                interfaz.IdentificadorEquipo,
                interfaz.EtiquetaPuerto,
                equipo.Nombre
            }).ToListAsync(cancelacion);

        // interfazOrigen y interfazDestino conservan los dos extremos elegidos; cambiar sus IDs cambia los equipos unidos.
        var interfazOrigen = extremosEnlace.Single(extremo =>
            extremo.IdentificadorInterfaz == formulario.IdentificadorPuertoOrigen);
        var interfazDestino = extremosEnlace.Single(extremo =>
            extremo.IdentificadorInterfaz == formulario.IdentificadorPuertoDestino);

        // nuevoEnlace guarda fuente y estado para distinguir lo confirmado de lo pendiente.
        var nuevoEnlace = new EnlaceFisico
        {
            IdentificadorPuertoOrigen = interfazOrigen.IdentificadorInterfaz,
            IdentificadorPuertoDestino = interfazDestino.IdentificadorInterfaz,
            Medio = formulario.Medio,
            ModuloOrigen = UtilidadesConexiones.NormalizarTexto(formulario.ModuloOrigen),
            ModuloDestino = UtilidadesConexiones.NormalizarTexto(formulario.ModuloDestino),
            EstadoDatos = formulario.EstadoDatos,
            FechaUltimaRevision = formulario.FechaUltimaRevision,
            Fuente = UtilidadesConexiones.NormalizarTexto(formulario.Fuente),
            Observaciones = UtilidadesConexiones.NormalizarTexto(formulario.Observaciones)
        };

        // eventoCambio describe la conexión completa; cambiar el extremo inicial cambia a qué equipo se atribuye el evento.
        var eventoCambio = new EventoModificacion
        {
            IdentificadorEquipo = interfazOrigen.IdentificadorEquipo,
            NombreEquipo = interfazOrigen.Nombre,
            NombreUsuario = ObtenerUsuarioEditor(),
            FechaUtc = DateTime.UtcNow,
            Campo = "Enlace físico agregado",
            ValorNuevo = $"{interfazOrigen.Nombre} · {interfazOrigen.EtiquetaPuerto} → {interfazDestino.Nombre} · {interfazDestino.EtiquetaPuerto} · {nuevoEnlace.Medio} · {nuevoEnlace.EstadoDatos}"
        };

        await contextoInventario.EnlacesFisicos.AddAsync(nuevoEnlace, cancelacion);
        await contextoInventario.EventosModificacion.AddAsync(eventoCambio, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);

        TempData["Mensaje"] = "Se registró el enlace documental. No se realizó ninguna acción sobre la red.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Obtiene el nombre disponible para atribuir cambios manuales.
    /// </summary>
    /// <returns>Nombre autenticado o etiqueta explícita del modo de pruebas sin sesión.</returns>
    private string ObtenerUsuarioEditor()
    {
        // nombreEditor identifica la sesión actual o deja claro que la página se usa sin login.
        var nombreEditor = User.Identity?.Name ?? "Pruebas sin sesión";
        return nombreEditor;
    }

}
