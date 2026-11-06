using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;

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
            IdentificadorEquipo = interfazGuardada.IdentificadorEquipo,
            EtiquetaPuerto = interfazGuardada.EtiquetaPuerto,
            TipoInterfaz = interfazGuardada.TipoInterfaz,
            EstadoDatos = interfazGuardada.EstadoDatos,
            FechaUltimaRevision = interfazGuardada.FechaUltimaRevision,
            Observaciones = interfazGuardada.Observaciones,
            EquiposDisponibles = await ObtenerEquiposAsync(interfazGuardada.IdentificadorEquipo, cancelacion)
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
        [Bind("Identificador,IdentificadorEquipo,EtiquetaPuerto,TipoInterfaz,EstadoDatos,FechaUltimaRevision,Observaciones")]
        EditarInterfazViewModel formulario,
        CancellationToken cancelacion)
    {
        // interfazGuardada aporta los valores anteriores que se auditan.
        var interfazGuardada = await contextoInventario.InterfacesEquipo
            .SingleOrDefaultAsync(interfaz => interfaz.Identificador == formulario.Identificador, cancelacion);
        if (interfazGuardada is null) return NotFound();

        // etiquetaNueva recorta espacios externos; cambiar esta regla altera la etiqueta guardada.
        var etiquetaNueva = formulario.EtiquetaPuerto.Trim();

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
            formulario.EquiposDisponibles = await ObtenerEquiposAsync(formulario.IdentificadorEquipo, cancelacion);
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

        AgregarCambio(cambiosInterfaz, equipoAnterior, usuarioEditor, "Equipo del puerto", $"{equipoAnterior.Identificador} · {equipoAnterior.Nombre}", $"{equipoNuevo.Identificador} · {equipoNuevo.Nombre}");
        AgregarCambio(cambiosInterfaz, equipoAnterior, usuarioEditor, "Etiqueta del puerto", interfazGuardada.EtiquetaPuerto, etiquetaNueva);
        AgregarCambio(cambiosInterfaz, equipoAnterior, usuarioEditor, "Tipo de interfaz", interfazGuardada.TipoInterfaz, formulario.TipoInterfaz);
        AgregarCambio(cambiosInterfaz, equipoAnterior, usuarioEditor, "Estado de interfaz", interfazGuardada.EstadoDatos, formulario.EstadoDatos);
        AgregarCambio(cambiosInterfaz, equipoAnterior, usuarioEditor, "Fecha de revisión",
            interfazGuardada.FechaUltimaRevision?.ToString("yyyy-MM-dd"),
            formulario.FechaUltimaRevision?.ToString("yyyy-MM-dd"));
        AgregarCambio(cambiosInterfaz, equipoAnterior, usuarioEditor, "Observaciones", interfazGuardada.Observaciones, formulario.Observaciones);

        interfazGuardada.IdentificadorEquipo = equipoNuevo.Identificador;
        interfazGuardada.EtiquetaPuerto = etiquetaNueva;
        interfazGuardada.TipoInterfaz = formulario.TipoInterfaz;
        interfazGuardada.EstadoDatos = formulario.EstadoDatos;
        interfazGuardada.FechaUltimaRevision = formulario.FechaUltimaRevision;
        interfazGuardada.Observaciones = NormalizarTexto(formulario.Observaciones);
        await contextoInventario.EventosModificacion.AddRangeAsync(cambiosInterfaz, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);

        TempData["Mensaje"] = cambiosInterfaz.Count == 0
            ? "No se detectaron cambios en el puerto."
            : $"Se actualizaron {cambiosInterfaz.Count} campos del puerto.";
        return RedirectToAction("Index", "Conexiones");
    }

    /// <summary>
    /// Carga un enlace existente para corregir sus extremos o metadatos.
    /// </summary>
    /// <param name="identificador">ID del enlace; cambiarlo selecciona otra conexión.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la consulta.</param>
    /// <returns>Formulario actual o respuesta no encontrado.</returns>
    [HttpGet]
    public async Task<IActionResult> EditarEnlace(int identificador, CancellationToken cancelacion)
    {
        // enlaceGuardado conserva los datos actuales; cambiar el ID carga otra relación.
        var enlaceGuardado = await contextoInventario.EnlacesFisicos.AsNoTracking()
            .SingleOrDefaultAsync(enlace => enlace.Identificador == identificador, cancelacion);
        if (enlaceGuardado is null) return NotFound();

        // formularioEdicion carga el enlace y todas las interfaces seleccionables.
        var formularioEdicion = new EditarEnlaceViewModel
        {
            Identificador = enlaceGuardado.Identificador,
            IdentificadorPuertoOrigen = enlaceGuardado.IdentificadorPuertoOrigen,
            IdentificadorPuertoDestino = enlaceGuardado.IdentificadorPuertoDestino,
            Medio = enlaceGuardado.Medio,
            ModuloOrigen = enlaceGuardado.ModuloOrigen,
            ModuloDestino = enlaceGuardado.ModuloDestino,
            EstadoDatos = enlaceGuardado.EstadoDatos,
            FechaUltimaRevision = enlaceGuardado.FechaUltimaRevision,
            Fuente = enlaceGuardado.Fuente,
            Observaciones = enlaceGuardado.Observaciones,
            InterfacesDisponibles = await ObtenerInterfacesAsync(enlaceGuardado.IdentificadorPuertoOrigen, cancelacion)
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
        [Bind("Identificador,IdentificadorPuertoOrigen,IdentificadorPuertoDestino,Medio,ModuloOrigen,ModuloDestino,EstadoDatos,FechaUltimaRevision,Fuente,Observaciones")]
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

        // extremoOcupado evita usar un puerto que pertenezca a otro enlace.
        var extremoOcupado = await contextoInventario.EnlacesFisicos.AnyAsync(enlace =>
            enlace.Identificador != formulario.Identificador
            && (enlace.IdentificadorPuertoOrigen == formulario.IdentificadorPuertoOrigen
                || enlace.IdentificadorPuertoDestino == formulario.IdentificadorPuertoOrigen
                || enlace.IdentificadorPuertoOrigen == formulario.IdentificadorPuertoDestino
                || enlace.IdentificadorPuertoDestino == formulario.IdentificadorPuertoDestino), cancelacion);
        if (extremoOcupado)
            ModelState.AddModelError(string.Empty, "Uno de los puertos ya participa en otro enlace.");

        if (!ModelState.IsValid)
        {
            formulario.InterfacesDisponibles = await ObtenerInterfacesAsync(formulario.IdentificadorPuertoOrigen, cancelacion);
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
                equipo.Nombre
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

        AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Puerto de origen", $"[{origenAnterior.IdInterfaz}] {origenAnterior.Nombre} · {origenAnterior.EtiquetaPuerto}",
            $"[{origenNuevo.IdInterfaz}] {origenNuevo.Nombre} · {origenNuevo.EtiquetaPuerto}");
        AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Puerto de destino", $"[{destinoAnterior.IdInterfaz}] {destinoAnterior.Nombre} · {destinoAnterior.EtiquetaPuerto}",
            $"[{destinoNuevo.IdInterfaz}] {destinoNuevo.Nombre} · {destinoNuevo.EtiquetaPuerto}");
        AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Medio", enlaceGuardado.Medio, formulario.Medio);
        AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Módulo origen", enlaceGuardado.ModuloOrigen, formulario.ModuloOrigen);
        AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Módulo destino", enlaceGuardado.ModuloDestino, formulario.ModuloDestino);
        AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Estado", enlaceGuardado.EstadoDatos, formulario.EstadoDatos);
        AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Fecha revisión", enlaceGuardado.FechaUltimaRevision?.ToString("yyyy-MM-dd"),
            formulario.FechaUltimaRevision?.ToString("yyyy-MM-dd"));
        AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Fuente", enlaceGuardado.Fuente, formulario.Fuente);
        AgregarCambio(cambiosEnlace, origenAnterior.IdEquipo, origenAnterior.Nombre, usuarioEditor,
            "Observaciones", enlaceGuardado.Observaciones, formulario.Observaciones);

        enlaceGuardado.IdentificadorPuertoOrigen = formulario.IdentificadorPuertoOrigen;
        enlaceGuardado.IdentificadorPuertoDestino = formulario.IdentificadorPuertoDestino;
        enlaceGuardado.Medio = formulario.Medio;
        enlaceGuardado.ModuloOrigen = NormalizarTexto(formulario.ModuloOrigen);
        enlaceGuardado.ModuloDestino = NormalizarTexto(formulario.ModuloDestino);
        enlaceGuardado.EstadoDatos = formulario.EstadoDatos;
        enlaceGuardado.FechaUltimaRevision = formulario.FechaUltimaRevision;
        enlaceGuardado.Fuente = NormalizarTexto(formulario.Fuente);
        enlaceGuardado.Observaciones = NormalizarTexto(formulario.Observaciones);
        await contextoInventario.EventosModificacion.AddRangeAsync(cambiosEnlace, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);

        TempData["Mensaje"] = cambiosEnlace.Count == 0
            ? "No se detectaron cambios en el enlace."
            : $"Se actualizaron {cambiosEnlace.Count} campos del enlace.";
        return RedirectToAction("Index", "Conexiones");
    }

    /// <summary>
    /// Carga equipos existentes para el selector del puerto.
    /// </summary>
    /// <param name="identificadorSeleccionado">ID inicial; cambiarlo cambia la opción preseleccionada.</param>
    /// <param name="cancelacion">Señal que cancela la consulta.</param>
    /// <returns>Opciones ordenadas por nombre de equipo.</returns>
    private async Task<List<SelectListItem>> ObtenerEquiposAsync(int identificadorSeleccionado, CancellationToken cancelacion)
    {
        // equiposDisponibles trae nombres e IP; cambiar el orden altera la presentación del selector.
        var equiposDisponibles = await contextoInventario.Equipos.AsNoTracking()
            .OrderBy(equipo => equipo.Nombre)
            .Select(equipo => new { equipo.Identificador, equipo.Nombre, equipo.Modelo, equipo.IpAdministracion })
            .ToListAsync(cancelacion);
        // opcionesEquipos convierte los registros existentes en valores seguros del selector.
        var opcionesEquipos = equiposDisponibles.Select(equipo => new SelectListItem
        {
            Value = equipo.Identificador.ToString(),
            Text = string.Join(" · ", new[] { equipo.Nombre, equipo.Modelo, equipo.IpAdministracion }
                .Where(texto => !string.IsNullOrWhiteSpace(texto))),
            Selected = equipo.Identificador == identificadorSeleccionado
        }).ToList();
        return opcionesEquipos;
    }

    /// <summary>
    /// Carga interfaces existentes con sus equipos para elegir extremos de enlace.
    /// </summary>
    /// <param name="identificadorSeleccionado">ID inicial; cambiarlo cambia la opción marcada.</param>
    /// <param name="cancelacion">Señal que cancela la consulta.</param>
    /// <returns>Opciones con equipo, IP y puerto.</returns>
    private async Task<List<SelectListItem>> ObtenerInterfacesAsync(int identificadorSeleccionado, CancellationToken cancelacion)
    {
        // interfacesDisponibles combina equipos y puertos; cambiar sus columnas modifica las etiquetas disponibles.
        var interfacesDisponibles = await (
            from interfaz in contextoInventario.InterfacesEquipo.AsNoTracking()
            join equipo in contextoInventario.Equipos.AsNoTracking()
                on interfaz.IdentificadorEquipo equals equipo.Identificador
            orderby equipo.Nombre, interfaz.EtiquetaPuerto
            select new { interfaz.Identificador, interfaz.EtiquetaPuerto, equipo.Nombre, equipo.IpAdministracion }
        ).ToListAsync(cancelacion);
        // opcionesInterfaces usa IDs de SQLite y una etiqueta legible.
        var opcionesInterfaces = interfacesDisponibles.Select(interfaz => new SelectListItem
        {
            Value = interfaz.Identificador.ToString(),
            Text = string.Join(" · ", new[] { interfaz.Nombre, interfaz.IpAdministracion, interfaz.EtiquetaPuerto }
                .Where(texto => !string.IsNullOrWhiteSpace(texto))),
            Selected = interfaz.Identificador == identificadorSeleccionado
        }).ToList();
        return opcionesInterfaces;
    }

    /// <summary>
    /// Registra una diferencia real de una ficha documental.
    /// </summary>
    /// <param name="eventos">Lista de cambios; cambiarla modifica dónde se agrega la fila.</param>
    /// <param name="equipo">Equipo al que se asocia la auditoría; cambiarlo atribuye el cambio a otro equipo.</param>
    /// <param name="usuario">Cuenta editora; cambiarla modifica la atribución histórica.</param>
    /// <param name="campo">Nombre del campo; cambiarlo modifica la descripción de la fila.</param>
    /// <param name="anterior">Valor anterior; cambiarlo modifica el dato previo del historial.</param>
    /// <param name="nuevo">Valor nuevo; cambiarlo modifica el dato posterior del historial.</param>
    private static void AgregarCambio(List<EventoModificacion> eventos, EquipoRed equipo, string usuario,
        string campo, string? anterior, string? nuevo)
    {
        // anteriorNormalizado y nuevoNormalizado quitan espacios externos y convierten vacío a nulo.
        var anteriorNormalizado = NormalizarTexto(anterior);
        var nuevoNormalizado = NormalizarTexto(nuevo);
        if (string.Equals(anteriorNormalizado, nuevoNormalizado, StringComparison.Ordinal)) return;

        // eventoCambio describe una propiedad; cambiar sus valores altera la línea del historial.
        var eventoCambio = new EventoModificacion
        {
            IdentificadorEquipo = equipo.Identificador,
            NombreEquipo = equipo.Nombre,
            NombreUsuario = usuario,
            FechaUtc = DateTime.UtcNow,
            Campo = campo,
            ValorAnterior = anteriorNormalizado,
            ValorNuevo = nuevoNormalizado
        };
        eventos.Add(eventoCambio);
    }

    /// <summary>
    /// Normaliza textos opcionales antes de guardarlos.
    /// </summary>
    /// <param name="texto">Valor recibido; cambiarlo modifica el texto almacenado.</param>
    /// <returns>Texto recortado o nulo si queda vacío.</returns>
    private static string? NormalizarTexto(string? texto)
    {
        // textoRecortado elimina espacios externos; cambiar esta regla altera el valor guardado.
        var textoRecortado = texto?.Trim();
        return string.IsNullOrEmpty(textoRecortado) ? null : textoRecortado;
    }
}
