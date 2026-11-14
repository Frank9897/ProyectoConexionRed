using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;
using ProyectoConexionRed.Web.Services;

namespace ProyectoConexionRed.Web.Controllers;

/// <summary>
/// Corrige puertos y enlaces persistidos en el inventario documental.
/// </summary>
public class CorreccionesController : Controller
{
    // contextoInventario guarda las correcciones y el historial; cambiarlo selecciona otra base documental.
    private readonly InventarioDbContext contextoInventario;

    /// <summary>
    /// Prepara el acceso a las tablas documentales de red.
    /// </summary>
    /// <param name="contexto">Contexto EF Core; cambiarlo selecciona otra base.</param>
    public CorreccionesController(InventarioDbContext contexto)
    {
        contextoInventario = contexto;
    }

    /// <summary>
    /// Carga una interfaz existente en el formulario de corrección.
    /// </summary>
    /// <param name="identificador">ID del puerto; cambiarlo selecciona otra interfaz.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la consulta.</param>
    /// <returns>Formulario actual o respuesta no encontrado.</returns>
    [HttpGet]
    public async Task<IActionResult> EditarInterfaz(int identificador, CancellationToken cancelacion)
    {
        // interfazGuardada conserva el puerto solicitado; cambiar el ID consultado abre otro registro.
        var interfazGuardada = await contextoInventario.InterfacesEquipo.AsNoTracking()
            .SingleOrDefaultAsync(interfaz => interfaz.Identificador == identificador, cancelacion);
        if (interfazGuardada is null) return NotFound();

        // formularioEdicion prepara los valores actuales y opciones del selector.
        var formularioEdicion = new EditarInterfazViewModel
        {
            Identificador = interfazGuardada.Identificador,
            TieneRelacion = await contextoInventario.EnlacesFisicos.AnyAsync(enlace =>
                enlace.IdentificadorPuertoOrigen == interfazGuardada.Identificador
                || enlace.IdentificadorPuertoDestino == interfazGuardada.Identificador, cancelacion)
                || await contextoInventario.ConexionesDocumentadas.AnyAsync(
                    relacion => relacion.IdentificadorPuertoLocal == interfazGuardada.Identificador, cancelacion),
            IdentificadorEquipo = interfazGuardada.IdentificadorEquipo,
            EtiquetaPuerto = interfazGuardada.EtiquetaPuerto,
            TipoInterfaz = interfazGuardada.TipoInterfaz,
            EstadoDatos = interfazGuardada.EstadoDatos,
            EstadoUso = interfazGuardada.EstadoUso,
            FechaUltimaRevision = interfazGuardada.FechaUltimaRevision,
            Observaciones = interfazGuardada.Observaciones,
            EquiposDisponibles = await UtilidadesConexiones.ObtenerEquiposDisponiblesAsync(contextoInventario, interfazGuardada.IdentificadorEquipo, cancelacion)
        };
        return View(formularioEdicion);
    }

    /// <summary>
    /// Guarda cambios de interfaz y audita cada campo modificado.
    /// </summary>
    /// <param name="formulario">Valores corregidos; cambiarlos solo actualiza el registro documental.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la escritura.</param>
    /// <returns>Redirección al listado o formulario con errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarInterfaz(
        [Bind("Identificador,IdentificadorEquipo,EtiquetaPuerto,TipoInterfaz,EstadoDatos,EstadoUso,FechaUltimaRevision,Observaciones")]
        EditarInterfazViewModel formulario,
        CancellationToken cancelacion)
    {
        // interfazGuardada aporta los valores anteriores que se auditan.
        var interfazGuardada = await contextoInventario.InterfacesEquipo
            .SingleOrDefaultAsync(interfaz => interfaz.Identificador == formulario.Identificador, cancelacion);
        if (interfazGuardada is null) return NotFound();

        // etiquetaNueva recorta espacios externos; cambiar esta regla altera la etiqueta guardada.
        var etiquetaNueva = formulario.EtiquetaPuerto.Trim();

        if (formulario.EstadoUso == "Libre confirmado" && formulario.EstadoDatos != "Confirmado")
            ModelState.AddModelError(nameof(formulario.EstadoUso), "Confirma también la vigencia del puerto antes de marcarlo libre.");

        // puertoTieneRelacion impide declarar libre una interfaz que ya tiene conexión completa o parcial.
        var puertoTieneRelacion = await contextoInventario.EnlacesFisicos.AnyAsync(enlace =>
            enlace.IdentificadorPuertoOrigen == interfazGuardada.Identificador
            || enlace.IdentificadorPuertoDestino == interfazGuardada.Identificador, cancelacion)
            || await contextoInventario.ConexionesDocumentadas.AnyAsync(relacion =>
                relacion.IdentificadorPuertoLocal == interfazGuardada.Identificador, cancelacion);
        if (formulario.EstadoUso == "Libre confirmado" && puertoTieneRelacion)
            ModelState.AddModelError(nameof(formulario.EstadoUso),
                "No se puede marcar libre un puerto que tiene una relación registrada.");

        // identidadAlterada detecta cambios de equipo o etiqueta que desplazarían un extremo conectado.
        var identidadAlterada = interfazGuardada.IdentificadorEquipo != formulario.IdentificadorEquipo
            || !string.Equals(interfazGuardada.EtiquetaPuerto, etiquetaNueva, StringComparison.Ordinal);
        if (puertoTieneRelacion && identidadAlterada)
            ModelState.AddModelError(string.Empty,
                "Este puerto tiene conexiones registradas. Primero resuelve esas conexiones antes de cambiar su equipo o etiqueta.");

        if (!await contextoInventario.Equipos.AnyAsync(
                equipo => equipo.Identificador == formulario.IdentificadorEquipo, cancelacion))
            ModelState.AddModelError(nameof(formulario.IdentificadorEquipo), "Selecciona un equipo existente.");

        // puertoDuplicado conserva la unicidad del nombre de puerto dentro de cada equipo.
        var puertoDuplicado = await contextoInventario.InterfacesEquipo.AnyAsync(
            interfaz => interfaz.Identificador != formulario.Identificador
                && interfaz.IdentificadorEquipo == formulario.IdentificadorEquipo
                && interfaz.EtiquetaPuerto == etiquetaNueva,
            cancelacion);
        if (puertoDuplicado)
            ModelState.AddModelError(nameof(formulario.EtiquetaPuerto), "Ese puerto ya está registrado para el equipo.");

        if (!ModelState.IsValid)
        {
            formulario.TieneRelacion = puertoTieneRelacion;
            if (puertoTieneRelacion)
            {
                // Se restaura la identidad real para que una petición alterada no confunda la vista bloqueada.
                formulario.IdentificadorEquipo = interfazGuardada.IdentificadorEquipo;
                formulario.EtiquetaPuerto = interfazGuardada.EtiquetaPuerto;
                ModelState.Remove(nameof(formulario.IdentificadorEquipo));
                ModelState.Remove(nameof(formulario.EtiquetaPuerto));
            }
            formulario.EquiposDisponibles = await UtilidadesConexiones.ObtenerEquiposDisponiblesAsync(contextoInventario, formulario.IdentificadorEquipo, cancelacion);
            return View(formulario);
        }

        // equipoAnterior atribuye el historial a la ficha previa, aunque el puerto cambie de propietario.
        var equipoAnterior = await contextoInventario.Equipos
            .SingleAsync(equipo => equipo.Identificador == interfazGuardada.IdentificadorEquipo, cancelacion);
        // equipoNuevo aporta la etiqueta del equipo seleccionado para las diferencias auditadas.
        var equipoNuevo = await contextoInventario.Equipos
            .SingleAsync(equipo => equipo.Identificador == formulario.IdentificadorEquipo, cancelacion);
        // usuarioEditor identifica al técnico o el modo abierto de pruebas.
        var usuarioEditor = User.Identity?.Name ?? "Pruebas sin sesión";
        // cambiosInterfaz acumula únicamente diferencias reales.
        var cambiosInterfaz = new List<EventoModificacion>();

        UtilidadesConexiones.AgregarCambio(cambiosInterfaz, equipoAnterior.Identificador, equipoAnterior.Nombre, usuarioEditor, "Equipo del puerto", $"{equipoAnterior.Identificador} · {equipoAnterior.Nombre}", $"{equipoNuevo.Identificador} · {equipoNuevo.Nombre}");
        UtilidadesConexiones.AgregarCambio(cambiosInterfaz, equipoAnterior.Identificador, equipoAnterior.Nombre, usuarioEditor, "Etiqueta del puerto", interfazGuardada.EtiquetaPuerto, etiquetaNueva);
        UtilidadesConexiones.AgregarCambio(cambiosInterfaz, equipoAnterior.Identificador, equipoAnterior.Nombre, usuarioEditor, "Tipo de interfaz", interfazGuardada.TipoInterfaz, formulario.TipoInterfaz);
        UtilidadesConexiones.AgregarCambio(cambiosInterfaz, equipoAnterior.Identificador, equipoAnterior.Nombre, usuarioEditor, "Estado de interfaz", interfazGuardada.EstadoDatos, formulario.EstadoDatos);
        UtilidadesConexiones.AgregarCambio(cambiosInterfaz, equipoAnterior.Identificador, equipoAnterior.Nombre, usuarioEditor, "Uso del puerto", interfazGuardada.EstadoUso, formulario.EstadoUso);
        UtilidadesConexiones.AgregarCambio(cambiosInterfaz, equipoAnterior.Identificador, equipoAnterior.Nombre, usuarioEditor, "Fecha de revisión",
            interfazGuardada.FechaUltimaRevision?.ToString("yyyy-MM-dd"),
            formulario.FechaUltimaRevision?.ToString("yyyy-MM-dd"));
        UtilidadesConexiones.AgregarCambio(cambiosInterfaz, equipoAnterior.Identificador, equipoAnterior.Nombre, usuarioEditor, "Observaciones", interfazGuardada.Observaciones, formulario.Observaciones);

        interfazGuardada.IdentificadorEquipo = equipoNuevo.Identificador;
        interfazGuardada.EtiquetaPuerto = etiquetaNueva;
        interfazGuardada.TipoInterfaz = formulario.TipoInterfaz;
        interfazGuardada.EstadoDatos = formulario.EstadoDatos;
        interfazGuardada.EstadoUso = formulario.EstadoUso;
        interfazGuardada.FechaUltimaRevision = formulario.FechaUltimaRevision;
        interfazGuardada.Observaciones = UtilidadesConexiones.NormalizarTexto(formulario.Observaciones);
        await contextoInventario.EventosModificacion.AddRangeAsync(cambiosInterfaz, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);

        TempData["Mensaje"] = cambiosInterfaz.Count == 0
            ? "No se detectaron cambios en el puerto."
            : $"Se actualizaron {cambiosInterfaz.Count} campos del puerto.";
        return string.Equals(equipoNuevo.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction("Detalle", "Topologia", new { identificador = equipoNuevo.Identificador })
            : RedirectToAction("Index", "Conexiones");
    }

    /// <summary>
    /// Elimina una interfaz solo si no sostiene una relación documentada.
    /// </summary>
    /// <param name="identificador">ID del puerto; cambiarlo selecciona otra interfaz a retirar.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene las verificaciones.</param>
    /// <returns>Regresa al frontal del switch o al listado de puertos tras la operación.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarInterfaz(int identificador, CancellationToken cancelacion)
    {
        // interfazGuardada es el registro que se intenta eliminar; cambiar el ID afecta otra posición.
        var interfazGuardada = await contextoInventario.InterfacesEquipo
            .SingleOrDefaultAsync(interfaz => interfaz.Identificador == identificador, cancelacion);
        if (interfazGuardada is null) return NotFound();

        // tieneEnlaceCompleto comprueba si algún enlace físico usa el puerto.
        var tieneEnlaceCompleto = await contextoInventario.EnlacesFisicos.AnyAsync(enlace =>
            enlace.IdentificadorPuertoOrigen == identificador || enlace.IdentificadorPuertoDestino == identificador,
            cancelacion);
        // tieneRelacionParcial comprueba las asignaciones guardadas desde ese puerto.
        var tieneRelacionParcial = await contextoInventario.ConexionesDocumentadas.AnyAsync(
            relacion => relacion.IdentificadorPuertoLocal == identificador, cancelacion);
        if (tieneEnlaceCompleto || tieneRelacionParcial)
        {
            TempData["Error"] = "No se puede eliminar este puerto mientras tenga una conexión registrada. Edita o elimina primero la relación.";
            return await VolverAlEquipoAsync(interfazGuardada.IdentificadorEquipo, cancelacion);
        }

        // equipoPropietario determina dónde volver y a qué ficha se atribuye el historial.
        var equipoPropietario = await contextoInventario.Equipos
            .SingleAsync(equipo => equipo.Identificador == interfazGuardada.IdentificadorEquipo, cancelacion);
        // eventoEliminacion conserva el texto anterior sin borrar la auditoría histórica.
        var eventoEliminacion = new EventoModificacion
        {
            IdentificadorEquipo = equipoPropietario.Identificador,
            NombreEquipo = equipoPropietario.Nombre,
            NombreUsuario = User.Identity?.Name ?? "Pruebas sin sesión",
            FechaUtc = DateTime.UtcNow,
            Campo = "Interfaz eliminada",
            ValorAnterior = $"{interfazGuardada.EtiquetaPuerto} · {interfazGuardada.TipoInterfaz} · {interfazGuardada.EstadoDatos} · {interfazGuardada.EstadoUso}",
            ValorNuevo = null
        };
        contextoInventario.InterfacesEquipo.Remove(interfazGuardada);
        await contextoInventario.EventosModificacion.AddAsync(eventoEliminacion, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);

        TempData["Mensaje"] = $"Se eliminó el puerto {interfazGuardada.EtiquetaPuerto}.";
        return await VolverAlEquipoAsync(equipoPropietario.Identificador, cancelacion);
    }

    /// <summary>
    /// Vuelve al frontal si el equipo es switch; de lo contrario abre Puertos.
    /// </summary>
    /// <param name="identificadorEquipo">Equipo propietario; cambiarlo selecciona otro destino de regreso.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la consulta del tipo de equipo.</param>
    /// <returns>Redirección a la vista correspondiente del equipo.</returns>
    private async Task<IActionResult> VolverAlEquipoAsync(int identificadorEquipo, CancellationToken cancelacion)
    {
        // equipoPropietario decide si corresponde abrir un frontal o el listado general.
        var equipoPropietario = await contextoInventario.Equipos.AsNoTracking()
            .SingleOrDefaultAsync(equipo => equipo.Identificador == identificadorEquipo, cancelacion);
        return string.Equals(equipoPropietario?.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction("Detalle", "Topologia", new { identificador = identificadorEquipo })
            : RedirectToAction("Index", "Conexiones");
    }

    /// <summary>
    /// Carga un enlace existente para corregir sus extremos o metadatos.
    /// </summary>
    /// <param name="identificador">ID del enlace; cambiarlo selecciona otra conexión.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la consulta.</param>
    /// <returns>Formulario actual o respuesta no encontrado.</returns>
    [HttpGet]
    public async Task<IActionResult> EditarEnlace(
        int identificador,
        int? identificadorSwitchRetorno,
        CancellationToken cancelacion)
    {
        // enlaceGuardado conserva los datos actuales; cambiar el ID carga otra relación.
        var enlaceGuardado = await contextoInventario.EnlacesFisicos.AsNoTracking()
            .SingleOrDefaultAsync(enlace => enlace.Identificador == identificador, cancelacion);
        if (enlaceGuardado is null) return NotFound();

        // formularioEdicion carga el enlace y todas las interfaces seleccionables.
        var formularioEdicion = new EditarEnlaceViewModel
        {
            Identificador = enlaceGuardado.Identificador,
            IdentificadorEquipoRetorno = await ObtenerSwitchRetornoAsync(enlaceGuardado, identificadorSwitchRetorno, cancelacion) ?? 0,
            IdentificadorPuertoOrigen = enlaceGuardado.IdentificadorPuertoOrigen,
            IdentificadorPuertoDestino = enlaceGuardado.IdentificadorPuertoDestino,
            Medio = enlaceGuardado.Medio,
            ModuloOrigen = enlaceGuardado.ModuloOrigen,
            ModuloDestino = enlaceGuardado.ModuloDestino,
            EstadoDatos = enlaceGuardado.EstadoDatos,
            FechaUltimaRevision = enlaceGuardado.FechaUltimaRevision,
            Fuente = enlaceGuardado.Fuente,
            Observaciones = enlaceGuardado.Observaciones,
            InterfacesDisponibles = await UtilidadesConexiones.ObtenerInterfacesDisponiblesAsync(contextoInventario, enlaceGuardado.IdentificadorPuertoOrigen, cancelacion)
        };
        return View(formularioEdicion);
    }

    /// <summary>
    /// Guarda correcciones de enlace si sus dos puertos existen y no están usados por otra relación.
    /// </summary>
    /// <param name="formulario">Datos corregidos; cambiarlos solo actualiza la documentación.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene validaciones o escrituras.</param>
    /// <returns>Redirección al listado o formulario con errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarEnlace(
        [Bind("Identificador,IdentificadorEquipoRetorno,IdentificadorPuertoOrigen,IdentificadorPuertoDestino,Medio,ModuloOrigen,ModuloDestino,EstadoDatos,FechaUltimaRevision,Fuente,Observaciones")]
        EditarEnlaceViewModel formulario,
        CancellationToken cancelacion)
    {
        // enlaceGuardado aporta el valor anterior para validar y auditar.
        var enlaceGuardado = await contextoInventario.EnlacesFisicos
            .SingleOrDefaultAsync(enlace => enlace.Identificador == formulario.Identificador, cancelacion);
        if (enlaceGuardado is null) return NotFound();

        if (formulario.IdentificadorPuertoOrigen == formulario.IdentificadorPuertoDestino)
            ModelState.AddModelError(string.Empty, "El origen y el destino deben ser puertos distintos.");

        // puertosNuevos comprueba que ambos extremos enviados correspondan a interfaces existentes.
        var puertosNuevos = await contextoInventario.InterfacesEquipo
            .Where(interfaz => interfaz.Identificador == formulario.IdentificadorPuertoOrigen
                || interfaz.Identificador == formulario.IdentificadorPuertoDestino)
            .ToListAsync(cancelacion);
        if (puertosNuevos.Count != 2)
            ModelState.AddModelError(string.Empty, "Selecciona dos puertos existentes.");
        else if (puertosNuevos.Any(interfaz => interfaz.EstadoUso == "Libre confirmado"
            && interfaz.Identificador != enlaceGuardado.IdentificadorPuertoOrigen
            && interfaz.Identificador != enlaceGuardado.IdentificadorPuertoDestino))
            ModelState.AddModelError(string.Empty,
                "Un puerto nuevo está marcado libre confirmado. Actualiza primero su uso antes de asignarlo al enlace.");

        // extremoOcupado evita usar un puerto que pertenezca a otro enlace.
        var extremoOcupado = await contextoInventario.EnlacesFisicos.AnyAsync(enlace =>
            enlace.Identificador != formulario.Identificador
            && (enlace.IdentificadorPuertoOrigen == formulario.IdentificadorPuertoOrigen
                || enlace.IdentificadorPuertoDestino == formulario.IdentificadorPuertoOrigen
                || enlace.IdentificadorPuertoOrigen == formulario.IdentificadorPuertoDestino
                || enlace.IdentificadorPuertoDestino == formulario.IdentificadorPuertoDestino), cancelacion);
        if (extremoOcupado)
            ModelState.AddModelError(string.Empty, "Uno de los puertos ya participa en otro enlace.");

        // extremoParcial impide reutilizar un puerto que tenga una relación documental de un solo extremo.
        var extremoParcial = await contextoInventario.ConexionesDocumentadas.AnyAsync(relacion =>
            relacion.IdentificadorPuertoLocal == formulario.IdentificadorPuertoOrigen
            || relacion.IdentificadorPuertoLocal == formulario.IdentificadorPuertoDestino, cancelacion);
        if (extremoParcial)
            ModelState.AddModelError(string.Empty, "Uno de los puertos ya tiene una relación documental parcial. Resuélvela antes de asignarlo a este enlace.");

        if (!ModelState.IsValid)
        {
            // Se valida el destino de retorno contra los switches que realmente participan en este enlace.
            formulario.IdentificadorEquipoRetorno = await ObtenerSwitchRetornoAsync(
                enlaceGuardado, formulario.IdentificadorEquipoRetorno, cancelacion) ?? 0;
            formulario.InterfacesDisponibles = await UtilidadesConexiones.ObtenerInterfacesDisponiblesAsync(contextoInventario, formulario.IdentificadorPuertoOrigen, cancelacion);
            return View(formulario);
        }

        // idsExtremos incluye IDs anteriores y propuestos para recuperar nombres de equipos y puertos.
        var idsExtremos = new[]
        {
            enlaceGuardado.IdentificadorPuertoOrigen, enlaceGuardado.IdentificadorPuertoDestino,
            formulario.IdentificadorPuertoOrigen, formulario.IdentificadorPuertoDestino
        };
        // extremos identifica cada puerto y su equipo antes de aplicar la actualización.
        var extremos = await (
            from interfaz in contextoInventario.InterfacesEquipo.AsNoTracking()
            join equipo in contextoInventario.Equipos.AsNoTracking()
                on interfaz.IdentificadorEquipo equals equipo.Identificador
            where idsExtremos.Contains(interfaz.Identificador)
            select new
            {
                IdInterfaz = interfaz.Identificador,
                interfaz.EtiquetaPuerto,
                IdEquipo = equipo.Identificador,
                equipo.Nombre,
                equipo.TipoDispositivo
            }).ToListAsync(cancelacion);

        // origenAnterior y destinoAnterior identifican el enlace antes de corregirse.
        var origenAnterior = extremos.Single(extremo => extremo.IdInterfaz == enlaceGuardado.IdentificadorPuertoOrigen);
        var destinoAnterior = extremos.Single(extremo => extremo.IdInterfaz == enlaceGuardado.IdentificadorPuertoDestino);
        // origenNuevo y destinoNuevo identifican los extremos seleccionados para guardar.
        var origenNuevo = extremos.Single(extremo => extremo.IdInterfaz == formulario.IdentificadorPuertoOrigen);
        var destinoNuevo = extremos.Single(extremo => extremo.IdInterfaz == formulario.IdentificadorPuertoDestino);
        // usuarioEditor atribuye las diferencias a la sesión actual o al modo de pruebas.
        var usuarioEditor = User.Identity?.Name ?? "Pruebas sin sesión";
        // cambiosEnlace agrupa una entrada de historial por campo modificado.
        var cambiosEnlace = new List<EventoModificacion>();

        UtilidadesConexiones.AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Puerto de origen", $"[{origenAnterior.IdInterfaz}] {origenAnterior.Nombre} · {origenAnterior.EtiquetaPuerto}",
            $"[{origenNuevo.IdInterfaz}] {origenNuevo.Nombre} · {origenNuevo.EtiquetaPuerto}");
        UtilidadesConexiones.AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Puerto de destino", $"[{destinoAnterior.IdInterfaz}] {destinoAnterior.Nombre} · {destinoAnterior.EtiquetaPuerto}",
            $"[{destinoNuevo.IdInterfaz}] {destinoNuevo.Nombre} · {destinoNuevo.EtiquetaPuerto}");
        UtilidadesConexiones.AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Medio", enlaceGuardado.Medio, formulario.Medio);
        UtilidadesConexiones.AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Módulo origen", enlaceGuardado.ModuloOrigen, formulario.ModuloOrigen);
        UtilidadesConexiones.AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Módulo destino", enlaceGuardado.ModuloDestino, formulario.ModuloDestino);
        UtilidadesConexiones.AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Estado", enlaceGuardado.EstadoDatos, formulario.EstadoDatos);
        UtilidadesConexiones.AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Fecha revisión", enlaceGuardado.FechaUltimaRevision?.ToString("yyyy-MM-dd"),
            formulario.FechaUltimaRevision?.ToString("yyyy-MM-dd"));
        UtilidadesConexiones.AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Fuente", enlaceGuardado.Fuente, formulario.Fuente);
        UtilidadesConexiones.AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Observaciones", enlaceGuardado.Observaciones, formulario.Observaciones);

        enlaceGuardado.IdentificadorPuertoOrigen = formulario.IdentificadorPuertoOrigen;
        enlaceGuardado.IdentificadorPuertoDestino = formulario.IdentificadorPuertoDestino;
        enlaceGuardado.Medio = formulario.Medio;
        enlaceGuardado.ModuloOrigen = UtilidadesConexiones.NormalizarTexto(formulario.ModuloOrigen);
        enlaceGuardado.ModuloDestino = UtilidadesConexiones.NormalizarTexto(formulario.ModuloDestino);
        enlaceGuardado.EstadoDatos = formulario.EstadoDatos;
        enlaceGuardado.FechaUltimaRevision = formulario.FechaUltimaRevision;
        enlaceGuardado.Fuente = UtilidadesConexiones.NormalizarTexto(formulario.Fuente);
        enlaceGuardado.Observaciones = UtilidadesConexiones.NormalizarTexto(formulario.Observaciones);
        await contextoInventario.EventosModificacion.AddRangeAsync(cambiosEnlace, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);

        TempData["Mensaje"] = cambiosEnlace.Count == 0
            ? "No se detectaron cambios en el enlace."
            : $"Se actualizaron {cambiosEnlace.Count} campos del enlace.";
        var identificadorSwitchRegreso = await ObtenerSwitchRetornoAsync(
            enlaceGuardado, formulario.IdentificadorEquipoRetorno, cancelacion);
        return identificadorSwitchRegreso.HasValue
            ? RedirectToAction("Detalle", "Topologia", new { identificador = identificadorSwitchRegreso.Value })
            : RedirectToAction("Index", "Conexiones");
    }


    /// <summary>
    /// Verifica que el switch solicitado participe en el enlace y determina el destino de regreso.
    /// </summary>
    /// <param name="enlace">Enlace vigente; cambiarlo cambia los extremos que se validan.</param>
    /// <param name="identificadorPreferido">Switch de origen; cambiarlo se acepta solo si es uno de los extremos.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la consulta.</param>
    /// <returns>ID de un switch participante o null si el enlace no conecta switches.</returns>
    private async Task<int?> ObtenerSwitchRetornoAsync(
        EnlaceFisico enlace,
        int? identificadorPreferido,
        CancellationToken cancelacion)
    {
        // identificadoresPuertos contiene los dos extremos documentados del enlace.
        var identificadoresPuertos = new[]
        {
            enlace.IdentificadorPuertoOrigen,
            enlace.IdentificadorPuertoDestino
        };
        // identificadoresSwitch identifica solo equipos Switch propietarios de esos extremos.
        var identificadoresSwitch = await (
            from interfaz in contextoInventario.InterfacesEquipo.AsNoTracking()
            join equipo in contextoInventario.Equipos.AsNoTracking()
                on interfaz.IdentificadorEquipo equals equipo.Identificador
            where identificadoresPuertos.Contains(interfaz.Identificador)
                && equipo.TipoDispositivo == "Switch"
            select equipo.Identificador)
            .Distinct()
            .ToListAsync(cancelacion);

        if (identificadorPreferido.HasValue && identificadoresSwitch.Contains(identificadorPreferido.Value))
        {
            return identificadorPreferido.Value;
        }

        // identificadorAlternativo usa un switch del enlace si el preferido no corresponde o no llegó.
        return identificadoresSwitch.Select(identificador => (int?)identificador).FirstOrDefault();
    }

    /// <summary>
    /// Carga una relación parcial para corregir el destino y sus datos de origen.
    /// </summary>
    /// <param name="identificador">ID de la relación; cambiarlo abre otro registro.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la consulta.</param>
    /// <returns>Formulario de corrección o respuesta no encontrado.</returns>
    [HttpGet]
    public async Task<IActionResult> EditarRelacion(int identificador, CancellationToken cancelacion)
    {
        // relacionGuardada conserva la conexión actual; cambiar el ID consulta otra ficha.
        var relacionGuardada = await contextoInventario.ConexionesDocumentadas.AsNoTracking()
            .SingleOrDefaultAsync(relacion => relacion.Identificador == identificador, cancelacion);
        if (relacionGuardada is null) return NotFound();

        // interfazLocal identifica legiblemente el puerto cuya relación se corrige.
        var interfazLocal = await (
            from interfaz in contextoInventario.InterfacesEquipo.AsNoTracking()
            join equipo in contextoInventario.Equipos.AsNoTracking()
                on interfaz.IdentificadorEquipo equals equipo.Identificador
            where interfaz.Identificador == relacionGuardada.IdentificadorPuertoLocal
            select new { equipo.Identificador, equipo.Nombre, equipo.IpAdministracion, interfaz.EtiquetaPuerto })
            .SingleOrDefaultAsync(cancelacion);
        if (interfazLocal is null) return NotFound();

        // formularioEdicion carga los valores guardados para editar el extremo remoto.
        var formularioEdicion = new EditarConexionDocumentadaViewModel
        {
            Identificador = relacionGuardada.Identificador,
            IdentificadorPuertoLocal = relacionGuardada.IdentificadorPuertoLocal,
            IdentificadorEquipo = interfazLocal.Identificador,
            NombrePuertoLocal = $"{interfazLocal.Nombre} · {interfazLocal.IpAdministracion ?? "IP no indicada"} · Puerto {interfazLocal.EtiquetaPuerto}",
            EquipoRemoto = relacionGuardada.EquipoRemoto,
            IpRemota = relacionGuardada.IpRemota,
            PuertoRemoto = relacionGuardada.PuertoRemoto,
            Medio = relacionGuardada.Medio,
            EstadoDatos = relacionGuardada.EstadoDatos,
            FechaUltimaRevision = relacionGuardada.FechaUltimaRevision,
            FuentePagina = relacionGuardada.FuentePagina,
            FuenteLibro = relacionGuardada.FuenteLibro,
            FuenteFila = relacionGuardada.FuenteFila,
            Observaciones = relacionGuardada.Observaciones
        };
        return View(formularioEdicion);
    }

    /// <summary>
    /// Guarda cambios en una conexión parcial sin alterar su puerto local propietario.
    /// </summary>
    /// <param name="formulario">Datos corregidos; cambiar los campos remotos modifica la relación documental.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la verificación o escritura.</param>
    /// <returns>Redirección al frontal del switch que conserva el puerto local.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarRelacion(
        [Bind("Identificador,EquipoRemoto,IpRemota,PuertoRemoto,Medio,EstadoDatos,FechaUltimaRevision,FuentePagina,FuenteLibro,FuenteFila,Observaciones")]
        EditarConexionDocumentadaViewModel formulario,
        CancellationToken cancelacion)
    {
        // relacionGuardada conserva la ubicación del puerto local y los valores que se auditarán.
        var relacionGuardada = await contextoInventario.ConexionesDocumentadas
            .SingleOrDefaultAsync(relacion => relacion.Identificador == formulario.Identificador, cancelacion);
        if (relacionGuardada is null) return NotFound();

        // equipoLocal atribuye la corrección al switch que contiene el puerto.
        var equipoLocal = await (
            from interfaz in contextoInventario.InterfacesEquipo.AsNoTracking()
            join equipo in contextoInventario.Equipos.AsNoTracking()
                on interfaz.IdentificadorEquipo equals equipo.Identificador
            where interfaz.Identificador == relacionGuardada.IdentificadorPuertoLocal
            select new { equipo.Identificador, equipo.Nombre, equipo.TipoDispositivo, equipo.IpAdministracion, interfaz.EtiquetaPuerto })
            .SingleOrDefaultAsync(cancelacion);
        if (equipoLocal is null) return NotFound();

        // nombrePuertoLocal muestra el extremo local sin permitir cambiar su propietario en este formulario.
        formulario.NombrePuertoLocal = $"{equipoLocal.Nombre} · {equipoLocal.IpAdministracion ?? "IP no indicada"} · Puerto {equipoLocal.EtiquetaPuerto}";
        formulario.IdentificadorEquipo = equipoLocal.Identificador;
        // Se conserva el puerto local original para evitar mover la relación con un ID oculto manipulado.
        formulario.IdentificadorPuertoLocal = relacionGuardada.IdentificadorPuertoLocal;
        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        // usuarioEditor identifica al técnico que ajusta la ficha.
        var usuarioEditor = User.Identity?.Name ?? "Pruebas sin sesión";
        // cambiosRelacion guarda solo diferencias y atribuye cada una al equipo local.
        var cambiosRelacion = new List<EventoModificacion>();
        UtilidadesConexiones.AgregarCambio(cambiosRelacion, equipoLocal.Identificador, equipoLocal.Nombre, usuarioEditor,
            "Equipo remoto", relacionGuardada.EquipoRemoto, UtilidadesConexiones.NormalizarTexto(formulario.EquipoRemoto));
        UtilidadesConexiones.AgregarCambio(cambiosRelacion, equipoLocal.Identificador, equipoLocal.Nombre, usuarioEditor,
            "IP remota", relacionGuardada.IpRemota, UtilidadesConexiones.NormalizarTexto(formulario.IpRemota));
        UtilidadesConexiones.AgregarCambio(cambiosRelacion, equipoLocal.Identificador, equipoLocal.Nombre, usuarioEditor,
            "Puerto remoto", relacionGuardada.PuertoRemoto, UtilidadesConexiones.NormalizarTexto(formulario.PuertoRemoto));
        UtilidadesConexiones.AgregarCambio(cambiosRelacion, equipoLocal.Identificador, equipoLocal.Nombre, usuarioEditor,
            "Medio", relacionGuardada.Medio, formulario.Medio);
        UtilidadesConexiones.AgregarCambio(cambiosRelacion, equipoLocal.Identificador, equipoLocal.Nombre, usuarioEditor,
            "Estado documental", relacionGuardada.EstadoDatos, formulario.EstadoDatos);
        UtilidadesConexiones.AgregarCambio(cambiosRelacion, equipoLocal.Identificador, equipoLocal.Nombre, usuarioEditor,
            "Fuente página", relacionGuardada.FuentePagina, formulario.FuentePagina.Trim());
        UtilidadesConexiones.AgregarCambio(cambiosRelacion, equipoLocal.Identificador, equipoLocal.Nombre, usuarioEditor,
            "Fuente libro", relacionGuardada.FuenteLibro, UtilidadesConexiones.NormalizarTexto(formulario.FuenteLibro));
        UtilidadesConexiones.AgregarCambio(cambiosRelacion, equipoLocal.Identificador, equipoLocal.Nombre, usuarioEditor,
            "Fuente fila", relacionGuardada.FuenteFila, UtilidadesConexiones.NormalizarTexto(formulario.FuenteFila));
        UtilidadesConexiones.AgregarCambio(cambiosRelacion, equipoLocal.Identificador, equipoLocal.Nombre, usuarioEditor,
            "Fecha de revisión", relacionGuardada.FechaUltimaRevision?.ToString("dd/MM/yyyy"),
            formulario.FechaUltimaRevision?.ToString("dd/MM/yyyy"));
        UtilidadesConexiones.AgregarCambio(cambiosRelacion, equipoLocal.Identificador, equipoLocal.Nombre, usuarioEditor,
            "Observaciones", relacionGuardada.Observaciones, UtilidadesConexiones.NormalizarTexto(formulario.Observaciones));

        relacionGuardada.EquipoRemoto = UtilidadesConexiones.NormalizarTexto(formulario.EquipoRemoto);
        relacionGuardada.IpRemota = UtilidadesConexiones.NormalizarTexto(formulario.IpRemota);
        relacionGuardada.PuertoRemoto = UtilidadesConexiones.NormalizarTexto(formulario.PuertoRemoto);
        relacionGuardada.Medio = formulario.Medio;
        relacionGuardada.EstadoDatos = formulario.EstadoDatos;
        relacionGuardada.FechaUltimaRevision = formulario.FechaUltimaRevision;
        relacionGuardada.FuentePagina = formulario.FuentePagina.Trim();
        relacionGuardada.FuenteLibro = UtilidadesConexiones.NormalizarTexto(formulario.FuenteLibro);
        relacionGuardada.FuenteFila = UtilidadesConexiones.NormalizarTexto(formulario.FuenteFila);
        relacionGuardada.Observaciones = UtilidadesConexiones.NormalizarTexto(formulario.Observaciones);
        await contextoInventario.EventosModificacion.AddRangeAsync(cambiosRelacion, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);

        TempData["Mensaje"] = cambiosRelacion.Count == 0
            ? "No se detectaron cambios en la conexión."
            : $"Se actualizaron {cambiosRelacion.Count} campos de la conexión.";
        return string.Equals(equipoLocal.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction("Detalle", "Topologia", new { identificador = equipoLocal.Identificador })
            : RedirectToAction("Index", "Conexiones");
    }

    /// <summary>
    /// Elimina una relación parcial conservando su rastro en el historial.
    /// </summary>
    /// <param name="identificador">ID de relación; cambiarlo selecciona otro registro documental.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la operación.</param>
    /// <returns>Regresa al frontal del equipo local o al listado de puertos.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarRelacion(int identificador, CancellationToken cancelacion)
    {
        // relacionGuardada es la observación parcial que se intenta retirar.
        var relacionGuardada = await contextoInventario.ConexionesDocumentadas
            .SingleOrDefaultAsync(relacion => relacion.Identificador == identificador, cancelacion);
        if (relacionGuardada is null) return NotFound();

        // equipoLocal permite guardar el evento y volver al mismo frontal.
        var equipoLocal = await (
            from interfaz in contextoInventario.InterfacesEquipo.AsNoTracking()
            join equipo in contextoInventario.Equipos.AsNoTracking()
                on interfaz.IdentificadorEquipo equals equipo.Identificador
            where interfaz.Identificador == relacionGuardada.IdentificadorPuertoLocal
            select new { equipo.Identificador, equipo.Nombre, equipo.TipoDispositivo })
            .SingleOrDefaultAsync(cancelacion);
        if (equipoLocal is null) return NotFound();

        // eventoEliminacion deja constancia del destino que se retiró.
        var eventoEliminacion = new EventoModificacion
        {
            IdentificadorEquipo = equipoLocal.Identificador,
            NombreEquipo = equipoLocal.Nombre,
            NombreUsuario = User.Identity?.Name ?? "Pruebas sin sesión",
            FechaUtc = DateTime.UtcNow,
            Campo = "Conexión documental eliminada",
            ValorAnterior = $"{relacionGuardada.EquipoRemoto ?? "Equipo no identificado"} · {relacionGuardada.IpRemota ?? "IP no indicada"}",
            ValorNuevo = null
        };
        contextoInventario.ConexionesDocumentadas.Remove(relacionGuardada);
        await contextoInventario.EventosModificacion.AddAsync(eventoEliminacion, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);

        TempData["Mensaje"] = "Se eliminó la relación documental; el puerto volvió a estar sin destino asignado.";
        return string.Equals(equipoLocal.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction("Detalle", "Topologia", new { identificador = equipoLocal.Identificador })
            : RedirectToAction("Index", "Conexiones");
    }

    /// <summary>
    /// Elimina un enlace físico y registra el cambio en el historial.
    /// </summary>
    /// <param name="identificador">ID del enlace; cambiarlo selecciona otra relación física.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene las consultas y escritura.</param>
    /// <returns>Regresa al frontal de un switch que forme parte del enlace.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarEnlace(
        int identificador,
        int? identificadorSwitchRetorno,
        CancellationToken cancelacion)
    {
        // enlaceGuardado conserva los dos extremos que se van a retirar.
        var enlaceGuardado = await contextoInventario.EnlacesFisicos
            .SingleOrDefaultAsync(enlace => enlace.Identificador == identificador, cancelacion);
        if (enlaceGuardado is null) return NotFound();

        // identificadorSwitchRegreso conserva el switch de origen solo si realmente participa del enlace.
        var identificadorSwitchRegreso = await ObtenerSwitchRetornoAsync(
            enlaceGuardado, identificadorSwitchRetorno, cancelacion);

        // puertosExtremos permite resolver los equipos y atribuir el cambio.
        var puertosExtremos = await contextoInventario.InterfacesEquipo.AsNoTracking()
            .Where(interfaz => interfaz.Identificador == enlaceGuardado.IdentificadorPuertoOrigen
                || interfaz.Identificador == enlaceGuardado.IdentificadorPuertoDestino)
            .ToListAsync(cancelacion);
        if (puertosExtremos.Count != 2) return NotFound();

        // equiposExtremos resuelve ambos puertos a sus fichas propietarias.
        var identificadoresEquipos = puertosExtremos.Select(interfaz => interfaz.IdentificadorEquipo).Distinct().ToList();
        var equiposExtremos = await contextoInventario.Equipos.AsNoTracking()
            .Where(equipo => identificadoresEquipos.Contains(equipo.Identificador))
            .ToListAsync(cancelacion);
        if (equiposExtremos.Count == 0) return NotFound();

        // equipoAtribucion usa un switch como destino al regresar si alguno participa en el enlace.
        var equipoAtribucion = equiposExtremos.FirstOrDefault(equipo =>
            string.Equals(equipo.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase))
            ?? equiposExtremos[0];
        // eventoEliminacion conserva puertos y medio previos en la auditoría.
        var eventoEliminacion = new EventoModificacion
        {
            IdentificadorEquipo = equipoAtribucion.Identificador,
            NombreEquipo = equipoAtribucion.Nombre,
            NombreUsuario = User.Identity?.Name ?? "Pruebas sin sesión",
            FechaUtc = DateTime.UtcNow,
            Campo = "Enlace físico eliminado",
            ValorAnterior = $"{enlaceGuardado.IdentificadorPuertoOrigen} ↔ {enlaceGuardado.IdentificadorPuertoDestino} · {enlaceGuardado.Medio}",
            ValorNuevo = null
        };
        contextoInventario.EnlacesFisicos.Remove(enlaceGuardado);
        await contextoInventario.EventosModificacion.AddAsync(eventoEliminacion, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);

        TempData["Mensaje"] = "Se eliminó el enlace documentado y sus puertos quedaron disponibles según sus estados guardados.";
        return identificadorSwitchRegreso.HasValue
            ? RedirectToAction("Detalle", "Topologia", new { identificador = identificadorSwitchRegreso.Value })
            : RedirectToAction("Index", "Conexiones");
    }


}
