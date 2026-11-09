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
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancelacion)
    {
        // interfacesRegistradas combina cada puerto con su equipo propietario; cambiar las tablas consultadas altera el listado.
        var interfacesRegistradas = await (
            from interfaz in contextoInventario.InterfacesEquipo.AsNoTracking()
            join equipo in contextoInventario.Equipos.AsNoTracking()
                on interfaz.IdentificadorEquipo equals equipo.Identificador
            orderby equipo.Nombre, interfaz.EtiquetaPuerto
            select new InterfazListadoViewModel
            {
                Identificador = interfaz.Identificador,
                Equipo = equipo.Nombre,
                IpAdministracion = equipo.IpAdministracion,
                Puerto = interfaz.EtiquetaPuerto,
                Tipo = interfaz.TipoInterfaz,
                EstadoDatos = interfaz.EstadoDatos,
                EstadoUso = contextoInventario.EnlacesFisicos.Any(enlace =>
                    enlace.IdentificadorPuertoOrigen == interfaz.Identificador
                    || enlace.IdentificadorPuertoDestino == interfaz.Identificador)
                    || contextoInventario.ConexionesDocumentadas.Any(relacion =>
                        relacion.IdentificadorPuertoLocal == interfaz.Identificador)
                    ? "Ocupado · relación documentada"
                    : interfaz.EstadoUso == "Libre confirmado" && interfaz.EstadoDatos != "Confirmado"
                        ? "Desconocido · vigencia sin confirmar" : interfaz.EstadoUso,
                FechaUltimaRevision = interfaz.FechaUltimaRevision,
                Observaciones = interfaz.Observaciones
            }).ToListAsync(cancelacion);

        // enlacesRegistrados une los dos extremos con sus equipos; cambiar los joins modifica los enlaces presentados.
        var enlacesRegistrados = await (
            from enlace in contextoInventario.EnlacesFisicos.AsNoTracking()
            join puertoOrigen in contextoInventario.InterfacesEquipo.AsNoTracking()
                on enlace.IdentificadorPuertoOrigen equals puertoOrigen.Identificador
            join equipoOrigen in contextoInventario.Equipos.AsNoTracking()
                on puertoOrigen.IdentificadorEquipo equals equipoOrigen.Identificador
            join puertoDestino in contextoInventario.InterfacesEquipo.AsNoTracking()
                on enlace.IdentificadorPuertoDestino equals puertoDestino.Identificador
            join equipoDestino in contextoInventario.Equipos.AsNoTracking()
                on puertoDestino.IdentificadorEquipo equals equipoDestino.Identificador
            orderby equipoOrigen.Nombre, puertoOrigen.EtiquetaPuerto
            select new EnlaceListadoViewModel
            {
                Identificador = enlace.Identificador,
                EquipoOrigen = equipoOrigen.Nombre,
                PuertoOrigen = puertoOrigen.EtiquetaPuerto,
                EquipoDestino = equipoDestino.Nombre,
                PuertoDestino = puertoDestino.EtiquetaPuerto,
                Medio = enlace.Medio,
                EstadoDatos = enlace.EstadoDatos,
                FechaUltimaRevision = enlace.FechaUltimaRevision,
                Fuente = enlace.Fuente
            }).ToListAsync(cancelacion);

        // relacionesParciales muestra el destino opcional y la procedencia sin inventar el otro extremo.
        var relacionesParciales = await (
            from relacion in contextoInventario.ConexionesDocumentadas.AsNoTracking()
            join puertoLocal in contextoInventario.InterfacesEquipo.AsNoTracking()
                on relacion.IdentificadorPuertoLocal equals puertoLocal.Identificador
            join equipoLocal in contextoInventario.Equipos.AsNoTracking()
                on puertoLocal.IdentificadorEquipo equals equipoLocal.Identificador
            orderby equipoLocal.Nombre, puertoLocal.EtiquetaPuerto
            select new RelacionParcialListadoViewModel
            {
                Identificador = relacion.Identificador,
                EquipoLocal = equipoLocal.Nombre,
                PuertoLocal = puertoLocal.EtiquetaPuerto,
                EquipoRemoto = relacion.EquipoRemoto,
                IpRemota = relacion.IpRemota,
                PuertoRemoto = relacion.PuertoRemoto,
                Medio = relacion.Medio,
                EstadoDatos = relacion.EstadoDatos,
                FuentePagina = relacion.FuentePagina,
                FuenteLibro = relacion.FuenteLibro,
                FuenteFila = relacion.FuenteFila,
                Observaciones = relacion.Observaciones
            }).ToListAsync(cancelacion);

        // modeloConexiones agrupa los listados completos y parciales sin confundirlos entre sí.
        var modeloConexiones = new ConexionesIndexViewModel
        {
            Interfaces = interfacesRegistradas,
            Enlaces = enlacesRegistrados,
            RelacionesParciales = relacionesParciales
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
    /// <returns>Redirección al listado o formulario con errores de validación.</returns>
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
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Presenta un formulario para registrar una conexión cuyo extremo remoto puede faltar.
    /// </summary>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la consulta de puertos disponibles.</param>
    /// <returns>Formulario con interfaces locales libres en la documentación.</returns>
    [HttpGet]
    public async Task<IActionResult> CrearRelacion(CancellationToken cancelacion)
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
            select new { interfaz.Identificador, interfaz.EtiquetaPuerto, equipo.Nombre, equipo.IpAdministracion })
            .ToListAsync(cancelacion);
        // formularioRelacion conserva los campos de origen y las opciones libres para el selector.
        var formularioRelacion = new NuevaConexionDocumentadaViewModel
        {
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
    /// <returns>Redirección a puertos y enlaces o formulario con errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearRelacion(
        [Bind("IdentificadorPuertoLocal,EquipoRemoto,IpRemota,PuertoRemoto,Medio,EstadoDatos,FechaUltimaRevision,FuentePagina,FuenteLibro,FuenteFila,Observaciones")]
        NuevaConexionDocumentadaViewModel formulario,
        CancellationToken cancelacion)
    {
        // interfazLocal verifica que el extremo conocido exista antes de persistir la relación.
        var interfazLocal = await (
            from interfaz in contextoInventario.InterfacesEquipo.AsNoTracking()
            join equipo in contextoInventario.Equipos.AsNoTracking()
                on interfaz.IdentificadorEquipo equals equipo.Identificador
            where interfaz.Identificador == formulario.IdentificadorPuertoLocal
            select new { interfaz.Identificador, interfaz.IdentificadorEquipo, interfaz.EtiquetaPuerto, interfaz.EstadoUso, equipo.Nombre })
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

        if (interfazLocal?.EstadoUso == "Libre confirmado")
            ModelState.AddModelError(nameof(formulario.IdentificadorPuertoLocal),
                "El puerto figura libre confirmado. Corrige primero su estado antes de registrar una relación.");

        if (!ModelState.IsValid)
        {
            formulario.InterfacesDisponibles = await ObtenerPuertosSinRelacionAsync(cancelacion);
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
        await contextoInventario.EventosModificacion.AddAsync(eventoCambio, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);
        TempData["Mensaje"] = "Se registró la relación documental con su fuente. El extremo remoto sigue opcional.";
        return RedirectToAction(nameof(Index));
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
