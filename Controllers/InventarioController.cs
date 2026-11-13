using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;
using ProyectoConexionRed.Web.Services;

namespace ProyectoConexionRed.Web.Controllers;

/// <summary>
/// Permite consultar y exportar el inventario documental de equipos.
/// </summary>
public class InventarioController : Controller
{
    // contextoInventario mantiene el acceso de esta solicitud a SQLite; cambiar el contexto cambia la base consultada.
    private readonly InventarioDbContext contextoInventario;

    /// <summary>
    /// Prepara el controlador con el contexto de inventario configurado.
    /// </summary>
    /// <param name="contexto">Contexto de datos; cambiarlo selecciona otra instancia de inventario.</param>
    public InventarioController(InventarioDbContext contexto)
    {
        contextoInventario = contexto;
    }

    /// <summary>
    /// Lista equipos y aplica una búsqueda opcional por nombre, IP, área, usuario, tipo, modelo o fabricante.
    /// </summary>
    /// <param name="busqueda">Texto de búsqueda; cambiarlo filtra qué equipos aparecen por sus datos de inventario.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la consulta pendiente.</param>
    /// <returns>Vista con los equipos coincidentes; modificar la búsqueda cambia el conjunto mostrado.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(string? busqueda, string? seccion, string? categoria, CancellationToken cancelacion)
    {
        // seccionActiva fija la familia que se presenta; cambiarla cambia el tipo de equipos filtrados.
        var seccionActiva = NormalizarSeccion(seccion);
        // categoriaActiva valida el subfiltro permitido; cambiarla selecciona otra agrupación documental.
        var categoriaActiva = NormalizarCategoria(seccionActiva, categoria);
        // equiposEncontrados conserva los criterios de búsqueda actuales antes de aplicar la sección.
        var equiposEncontrados = await ObtenerEquiposAsync(busqueda, cancelacion);
        // equiposVisibles contiene solo las fichas pertenecientes a la sección y categoría seleccionadas.
        var equiposVisibles = await FiltrarPorSeccionAsync(equiposEncontrados, seccionActiva, categoriaActiva, cancelacion);

        ViewBag.SeccionActiva = seccionActiva;
        ViewBag.CategoriaActiva = categoriaActiva;
        ViewBag.TituloVista = ObtenerTituloVista(seccionActiva, categoriaActiva);
        return View(equiposVisibles);
    }

    /// <summary>
    /// Presenta un formulario vacío para registrar un equipo de red o un puesto de usuario.
    /// </summary>
    /// <returns>Formulario con valores documentales iniciales; cambiar los valores predeterminados cambia cómo comienza el alta.</returns>
    [HttpGet]
    public IActionResult Crear()
    {
        // formularioNuevo define valores iniciales seguros; modificarlos cambia los valores que verá el técnico al abrir el alta.
        var formularioNuevo = new EditarEquipoViewModel
        {
            RolEnArea = "Usuario",
            TipoDispositivo = "Puesto de usuario",
            EstadoDatos = "Pendiente de validación"
        };

        return View(formularioNuevo);
    }

    /// <summary>
    /// Crea una ficha documental y registra sus valores iniciales en el historial.
    /// </summary>
    /// <param name="formulario">Datos del nuevo equipo; cambiar sus propiedades modifica solo el inventario local.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la escritura antes de confirmar la transacción.</param>
    /// <returns>Redirección al inventario o formulario con los errores de validación.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(EditarEquipoViewModel formulario, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        // usuarioEditor atribuye el alta; cambiar su identidad cambia el autor del historial.
        var usuarioEditor = User.Identity?.Name ?? "Usuario desconocido";
        // esSwitch decide si se conservan cantidades de puertos; cambiarlo altera qué datos de capacidad se guardan.
        var esSwitch = string.Equals(formulario.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase);
        // equipoNuevo contiene la ficha normalizada; cambiar su asignación cambia únicamente el registro local.
        var equipoNuevo = new EquipoRed
        {
            Nombre = formulario.Nombre.Trim(),
            Fabricante = UtilidadesConexiones.NormalizarTexto(formulario.Fabricante),
            Modelo = UtilidadesConexiones.NormalizarTexto(formulario.Modelo),
            IpAdministracion = UtilidadesConexiones.NormalizarTexto(formulario.IpAdministracion),
            DireccionMac = UtilidadesConexiones.NormalizarTexto(formulario.DireccionMac),
            Area = UtilidadesConexiones.NormalizarTexto(formulario.Area),
            Piso = UtilidadesConexiones.NormalizarTexto(formulario.Piso),
            RolEnArea = formulario.RolEnArea.Trim(),
            TipoDispositivo = formulario.TipoDispositivo.Trim(),
            CantidadPuertosRj45 = esSwitch ? formulario.CantidadPuertosRj45 : null,
            CantidadPuertosSfp = esSwitch ? formulario.CantidadPuertosSfp : null,
            CantidadPuertosSfpPlus = esSwitch ? formulario.CantidadPuertosSfpPlus : null,
            CantidadPuertosQsfp = esSwitch ? formulario.CantidadPuertosQsfp : null,
            CantidadPuertosQsfpPlus = esSwitch ? formulario.CantidadPuertosQsfpPlus : null,
            CantidadPuertosQsfp28 = esSwitch ? formulario.CantidadPuertosQsfp28 : null,
            EstadoDatos = formulario.EstadoDatos.Trim(),
            FechaUltimaRevision = formulario.FechaUltimaRevision,
            Observaciones = UtilidadesConexiones.NormalizarTexto(formulario.Observaciones)
        };

        // transaccionAlta confirma la ficha y el historial juntos para evitar registros sin trazabilidad.
        await using var transaccionAlta = await contextoInventario.Database.BeginTransactionAsync(cancelacion);
        await contextoInventario.Equipos.AddAsync(equipoNuevo, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);

        // interfacesGeneradas crea un registro por conector cuando la ficha del switch declara cantidades.
        var interfacesGeneradas = GeneradorInterfacesSwitch.CrearInterfacesFaltantes(equipoNuevo, []);
        if (interfacesGeneradas.Count > 0)
        {
            await contextoInventario.InterfacesEquipo.AddRangeAsync(interfacesGeneradas, cancelacion);
        }

        // cambiosIniciales contiene los campos cargados al crear el equipo; cambiar estos pares altera el detalle del evento.
        var cambiosIniciales = new List<EventoModificacion>();
        UtilidadesConexiones.AgregarCambio(cambiosIniciales, equipoNuevo.Identificador, equipoNuevo.Nombre, usuarioEditor, "Alta de ficha", null, "Equipo registrado");
        UtilidadesConexiones.AgregarCambio(cambiosIniciales, equipoNuevo.Identificador, equipoNuevo.Nombre, usuarioEditor, "Fabricante", null, equipoNuevo.Fabricante);
        UtilidadesConexiones.AgregarCambio(cambiosIniciales, equipoNuevo.Identificador, equipoNuevo.Nombre, usuarioEditor, "Modelo", null, equipoNuevo.Modelo);
        UtilidadesConexiones.AgregarCambio(cambiosIniciales, equipoNuevo.Identificador, equipoNuevo.Nombre, usuarioEditor, "IP documentada", null, equipoNuevo.IpAdministracion);
        UtilidadesConexiones.AgregarCambio(cambiosIniciales, equipoNuevo.Identificador, equipoNuevo.Nombre, usuarioEditor, "MAC", null, equipoNuevo.DireccionMac);
        UtilidadesConexiones.AgregarCambio(cambiosIniciales, equipoNuevo.Identificador, equipoNuevo.Nombre, usuarioEditor, "Área", null, equipoNuevo.Area);
        UtilidadesConexiones.AgregarCambio(cambiosIniciales, equipoNuevo.Identificador, equipoNuevo.Nombre, usuarioEditor, "Piso o sector", null, equipoNuevo.Piso);
        UtilidadesConexiones.AgregarCambio(cambiosIniciales, equipoNuevo.Identificador, equipoNuevo.Nombre, usuarioEditor, "Rol en el área", null, equipoNuevo.RolEnArea);
        UtilidadesConexiones.AgregarCambio(cambiosIniciales, equipoNuevo.Identificador, equipoNuevo.Nombre, usuarioEditor, "Tipo de dispositivo", null, equipoNuevo.TipoDispositivo);
        AgregarCambiosCapacidad(cambiosIniciales, equipoNuevo, usuarioEditor, null);
        // resumenInterfaces evita registrar una línea de auditoría independiente por cada puerto.
        var resumenInterfaces = GeneradorInterfacesSwitch.DescribirInterfacesCreadas(interfacesGeneradas);
        UtilidadesConexiones.AgregarCambio(cambiosIniciales, equipoNuevo.Identificador, equipoNuevo.Nombre,
            usuarioEditor, "Interfaces generadas desde capacidad", null, resumenInterfaces);
        UtilidadesConexiones.AgregarCambio(cambiosIniciales, equipoNuevo.Identificador, equipoNuevo.Nombre, usuarioEditor, "Vigencia de datos", null, equipoNuevo.EstadoDatos);
        UtilidadesConexiones.AgregarCambio(cambiosIniciales, equipoNuevo.Identificador, equipoNuevo.Nombre, usuarioEditor, "Observaciones", null, equipoNuevo.Observaciones);

        await contextoInventario.EventosModificacion.AddRangeAsync(cambiosIniciales, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);
        await transaccionAlta.CommitAsync(cancelacion);

        TempData["Mensaje"] = "Se creó la ficha y se registró su origen en el historial.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Descarga como CSV los equipos que coinciden con la búsqueda actual.
    /// </summary>
    /// <param name="busqueda">Texto de búsqueda; cambiarlo filtra las filas incluidas en el archivo.</param>
    /// <param name="seccion">Familia activa; cambiarla limita la exportación a switches, usuarios u otros equipos.</param>
    /// <param name="categoria">Subfiltro activo; cambiarlo exporta otra categoría visible.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la consulta pendiente.</param>
    /// <returns>Archivo CSV UTF-8 con los resultados visibles; cambiar filtros altera las filas exportadas.</returns>
    [HttpGet]
    public async Task<IActionResult> ExportarCsv(string? busqueda, string? seccion, string? categoria, CancellationToken cancelacion)
    {
        // seccionActiva y categoriaActiva hacen que el archivo coincida con la agrupación vista en pantalla.
        var seccionActiva = NormalizarSeccion(seccion);
        var categoriaActiva = NormalizarCategoria(seccionActiva, categoria);
        // equiposCoincidentes conserva el filtro textual antes de aplicar la categoría.
        var equiposCoincidentes = await ObtenerEquiposAsync(busqueda, cancelacion);
        // equiposExportados contiene solo las filas de la búsqueda, sección y categoría seleccionadas.
        var equiposExportados = await FiltrarPorSeccionAsync(equiposCoincidentes, seccionActiva, categoriaActiva, cancelacion);

        // contenidoCsv acumula el archivo; cambiar encabezados o separadores modifica cómo se interpreta en hojas de cálculo.
        var contenidoCsv = new StringBuilder();
        contenidoCsv.AppendLine("Equipo;Fabricante;Modelo;Área;Piso;IP documentada;MAC;Rol;Tipo;Puertos RJ45;Puertos SFP;Puertos SFP+;Puertos QSFP;Puertos QSFP+;Puertos QSFP28;Vigencia");

        // equipo recorre cada fila exportada; cambiar los registros de entrada cambia las filas del archivo.
        foreach (var equipo in equiposExportados)
        {
            // columnasCsv contiene los valores de una fila; cambiar su orden debe corresponder al orden del encabezado.
            var columnasCsv = new string?[]
            {
                equipo.Nombre,
                equipo.Fabricante,
                equipo.Modelo,
                equipo.Area,
                equipo.Piso,
                equipo.IpAdministracion,
                equipo.DireccionMac,
                equipo.RolEnArea,
                equipo.TipoDispositivo,
                equipo.CantidadPuertosRj45?.ToString(CultureInfo.InvariantCulture),
                equipo.CantidadPuertosSfp?.ToString(CultureInfo.InvariantCulture),
                equipo.CantidadPuertosSfpPlus?.ToString(CultureInfo.InvariantCulture),
                equipo.CantidadPuertosQsfp?.ToString(CultureInfo.InvariantCulture),
                equipo.CantidadPuertosQsfpPlus?.ToString(CultureInfo.InvariantCulture),
                equipo.CantidadPuertosQsfp28?.ToString(CultureInfo.InvariantCulture),
                equipo.EstadoDatos
            };

            // indiceColumna ubica cada campo de la fila; cambiar su avance altera el orden de las columnas escritas.
            for (var indiceColumna = 0; indiceColumna < columnasCsv.Length; indiceColumna++)
            {
                if (indiceColumna > 0)
                {
                    contenidoCsv.Append(';');
                }

                contenidoCsv.Append(EscaparCampoCsv(columnasCsv[indiceColumna]));
            }

            contenidoCsv.AppendLine();
        }

        // bytesCsv incluye marca UTF-8 para que lectores comunes reconozcan tildes; cambiar la codificación cambia los bytes descargados.
        var bytesCsv = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(contenidoCsv.ToString()))
            .ToArray();

        return File(bytesCsv, "text/csv; charset=utf-8", "inventario-red.csv");
    }


    /// <summary>
    /// Presenta la ficha editable del equipo seleccionado.
    /// </summary>
    /// <param name="id">Identificador del equipo; cambiarlo selecciona otra ficha.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la consulta.</param>
    /// <returns>Formulario de edición o respuesta 404 cuando no existe el equipo.</returns>
    [HttpGet]
    public async Task<IActionResult> Editar(int id, CancellationToken cancelacion)
    {
        // equipoEncontrado carga el registro seleccionado; cambiar id consulta otro equipo.
        var equipoEncontrado = await contextoInventario.Equipos
            .AsNoTracking()
            .SingleOrDefaultAsync(equipo => equipo.Identificador == id, cancelacion);
        if (equipoEncontrado is null)
        {
            return NotFound();
        }

        // formularioEdicion contiene solo campos editables; cambiar el mapeo altera los datos enviados al formulario.
        var formularioEdicion = new EditarEquipoViewModel
        {
            Identificador = equipoEncontrado.Identificador,
            Nombre = equipoEncontrado.Nombre,
            Fabricante = equipoEncontrado.Fabricante,
            Modelo = equipoEncontrado.Modelo,
            IpAdministracion = equipoEncontrado.IpAdministracion,
            DireccionMac = equipoEncontrado.DireccionMac,
            Area = equipoEncontrado.Area,
            Piso = equipoEncontrado.Piso,
            RolEnArea = equipoEncontrado.RolEnArea,
            CantidadPuertosRj45 = equipoEncontrado.CantidadPuertosRj45,
            CantidadPuertosSfp = equipoEncontrado.CantidadPuertosSfp,
            CantidadPuertosSfpPlus = equipoEncontrado.CantidadPuertosSfpPlus,
            CantidadPuertosQsfp = equipoEncontrado.CantidadPuertosQsfp,
            CantidadPuertosQsfpPlus = equipoEncontrado.CantidadPuertosQsfpPlus,
            CantidadPuertosQsfp28 = equipoEncontrado.CantidadPuertosQsfp28,
            TipoDispositivo = equipoEncontrado.TipoDispositivo,
            EstadoDatos = equipoEncontrado.EstadoDatos,
            FechaUltimaRevision = equipoEncontrado.FechaUltimaRevision,
            Observaciones = equipoEncontrado.Observaciones
        };

        return View(formularioEdicion);
    }

    /// <summary>
    /// Guarda los campos permitidos y registra cada diferencia en el historial.
    /// </summary>
    /// <param name="formulario">Valores editados; cambiar sus propiedades modifica solo la ficha documental.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe lectura o persistencia.</param>
    /// <returns>Redirección al inventario o formulario con errores de validación.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EditarEquipoViewModel formulario, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        // equipoGuardado es la fila persistida; cambiar el identificador del formulario selecciona otra fila.
        var equipoGuardado = await contextoInventario.Equipos
            .SingleOrDefaultAsync(equipo => equipo.Identificador == formulario.Identificador, cancelacion);
        if (equipoGuardado is null)
        {
            return NotFound();
        }

        // usuarioEditor atribuye el cambio; cambiar la identidad altera el autor almacenado en la auditoría.
        var usuarioEditor = User.Identity?.Name ?? "Usuario desconocido";
        // cambiosRegistrados reúne solo diferencias reales; cambiar los campos comparados altera qué se audita.
        var cambiosRegistrados = new List<EventoModificacion>();
        // nombreEquipoAnterior conserva la etiqueta previa para identificar el registro aunque se renombre.
        var nombreEquipoAnterior = equipoGuardado.Nombre;

        UtilidadesConexiones.AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Nombre", equipoGuardado.Nombre, formulario.Nombre);
        UtilidadesConexiones.AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Fabricante", equipoGuardado.Fabricante, formulario.Fabricante);
        UtilidadesConexiones.AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Modelo", equipoGuardado.Modelo, formulario.Modelo);
        UtilidadesConexiones.AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "IP documentada", equipoGuardado.IpAdministracion, formulario.IpAdministracion);
        UtilidadesConexiones.AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Dirección MAC", equipoGuardado.DireccionMac, formulario.DireccionMac);
        UtilidadesConexiones.AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Área", equipoGuardado.Area, formulario.Area);
        UtilidadesConexiones.AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Piso o sector", equipoGuardado.Piso, formulario.Piso);
        UtilidadesConexiones.AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Rol en el área", equipoGuardado.RolEnArea, formulario.RolEnArea);
        UtilidadesConexiones.AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Tipo de dispositivo", equipoGuardado.TipoDispositivo, formulario.TipoDispositivo);
        AgregarCambiosCapacidad(cambiosRegistrados, equipoGuardado, usuarioEditor, formulario);
        UtilidadesConexiones.AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Vigencia de datos", equipoGuardado.EstadoDatos, formulario.EstadoDatos);
        UtilidadesConexiones.AgregarCambio(
            cambiosRegistrados,
            equipoGuardado.Identificador,
            nombreEquipoAnterior,
            usuarioEditor,
            "Última revisión",
            equipoGuardado.FechaUltimaRevision?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            formulario.FechaUltimaRevision?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        UtilidadesConexiones.AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Observaciones", equipoGuardado.Observaciones, formulario.Observaciones);

        equipoGuardado.Nombre = formulario.Nombre.Trim();
        equipoGuardado.Fabricante = UtilidadesConexiones.NormalizarTexto(formulario.Fabricante);
        equipoGuardado.Modelo = UtilidadesConexiones.NormalizarTexto(formulario.Modelo);
        equipoGuardado.IpAdministracion = UtilidadesConexiones.NormalizarTexto(formulario.IpAdministracion);
        equipoGuardado.DireccionMac = UtilidadesConexiones.NormalizarTexto(formulario.DireccionMac);
        equipoGuardado.Area = UtilidadesConexiones.NormalizarTexto(formulario.Area);
        equipoGuardado.Piso = UtilidadesConexiones.NormalizarTexto(formulario.Piso);
        equipoGuardado.RolEnArea = formulario.RolEnArea.Trim();
        equipoGuardado.TipoDispositivo = formulario.TipoDispositivo.Trim();
        equipoGuardado.CantidadPuertosRj45 = string.Equals(formulario.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase) ? formulario.CantidadPuertosRj45 : null;
        equipoGuardado.CantidadPuertosSfp = string.Equals(formulario.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase) ? formulario.CantidadPuertosSfp : null;
        equipoGuardado.CantidadPuertosSfpPlus = string.Equals(formulario.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase) ? formulario.CantidadPuertosSfpPlus : null;
        equipoGuardado.CantidadPuertosQsfp = string.Equals(formulario.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase) ? formulario.CantidadPuertosQsfp : null;
        equipoGuardado.CantidadPuertosQsfpPlus = string.Equals(formulario.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase) ? formulario.CantidadPuertosQsfpPlus : null;
        equipoGuardado.CantidadPuertosQsfp28 = string.Equals(formulario.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase) ? formulario.CantidadPuertosQsfp28 : null;
        equipoGuardado.EstadoDatos = formulario.EstadoDatos.Trim();
        equipoGuardado.FechaUltimaRevision = formulario.FechaUltimaRevision;
        equipoGuardado.Observaciones = UtilidadesConexiones.NormalizarTexto(formulario.Observaciones);

        // interfacesExistentes conserva etiquetas o estados previamente documentados.
        var interfacesExistentes = await contextoInventario.InterfacesEquipo.AsNoTracking()
            .Where(interfaz => interfaz.IdentificadorEquipo == equipoGuardado.Identificador)
            .ToListAsync(cancelacion);
        // interfacesGeneradas agrega solo las posiciones que todavía no existen para la capacidad guardada.
        var interfacesGeneradas = GeneradorInterfacesSwitch.CrearInterfacesFaltantes(equipoGuardado, interfacesExistentes);
        if (interfacesGeneradas.Count > 0)
        {
            await contextoInventario.InterfacesEquipo.AddRangeAsync(interfacesGeneradas, cancelacion);
            // resumenInterfaces atribuye una sola diferencia al grupo de conectores recién agregados.
            UtilidadesConexiones.AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador,
                nombreEquipoAnterior, usuarioEditor, "Interfaces generadas desde capacidad", null,
                GeneradorInterfacesSwitch.DescribirInterfacesCreadas(interfacesGeneradas));
        }

        if (cambiosRegistrados.Count > 0)
        {
            await contextoInventario.EventosModificacion.AddRangeAsync(cambiosRegistrados, cancelacion);
        }

        await contextoInventario.SaveChangesAsync(cancelacion);
        TempData["Mensaje"] = cambiosRegistrados.Count == 0
            ? "No se detectaron cambios en la ficha."
            : $"Se guardaron {cambiosRegistrados.Count} cambios en el historial.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Agrega al historial una diferencia entre los valores recibidos.
    /// </summary>
    /// <param name="cambios">Lista que acumula las diferencias; cambiarla cambia dónde se agregan.</param>
    /// <param name="identificadorEquipo">ID del equipo editado; cambiarlo asocia el evento a otro registro.</param>
    /// <param name="nombreEquipo">Etiqueta previa; cambiarla modifica el texto histórico del equipo.</param>
    /// <param name="usuarioEditor">Cuenta actual; cambiarla modifica la atribución del evento.</param>
    /// <param name="campo">Nombre del campo; cambiarlo cambia la propiedad descrita.</param>
    /// <param name="valorAnterior">Dato previo; cambiarlo altera el antes documentado.</param>
    /// <param name="valorNuevo">Dato posterior; cambiarlo altera el después documentado.</param>
    private async Task<List<EquipoRed>> ObtenerEquiposAsync(string? busqueda, CancellationToken cancelacion)
    {
        // equiposCargados conserva los datos de SQLite para filtrar; cambiar la fuente cambia el inventario consultado.
        var equiposCargados = await contextoInventario.Equipos
            .AsNoTracking()
            .ToListAsync(cancelacion);

        if (string.IsNullOrWhiteSpace(busqueda))
        {
            return equiposCargados;
        }

        // textoNormalizado conserva el término escrito; cambiarlo altera qué registros coinciden.
        var textoNormalizado = busqueda.Trim();

        // equiposFiltrados contiene solo coincidencias; cambiar las propiedades revisadas cambia los campos buscables.
        var equiposFiltrados = new List<EquipoRed>();

        // equipo avanza por cada registro cargado; cambiar el inventario de entrada cambia los registros comparados.
        foreach (var equipo in equiposCargados)
        {
            if (CoincideConBusqueda(equipo, textoNormalizado))
            {
                equiposFiltrados.Add(equipo);
            }
        }

        return equiposFiltrados;
    }

    /// <summary>
    /// Valida el nombre de sección recibido y limita la vista a una familia conocida.
    /// </summary>
    /// <param name="seccion">Texto de URL; cambiarlo selecciona otra familia o vuelve a Todo si no está permitida.</param>
    /// <returns>Clave de sección reconocida para filtrar el inventario.</returns>
    private static string NormalizarSeccion(string? seccion)
    {
        // seccionNormalizada evita aceptar valores arbitrarios como categorías de inventario.
        var seccionNormalizada = seccion?.Trim().ToLowerInvariant();
        return seccionNormalizada is "switches" or "usuarios" or "otros" ? seccionNormalizada! : "todos";
    }

    /// <summary>
    /// Valida la categoría según la sección seleccionada.
    /// </summary>
    /// <param name="seccion">Familia activa; cambiarla cambia las categorías disponibles.</param>
    /// <param name="categoria">Subfiltro de URL; cambiarlo selecciona una agrupación dentro de la familia.</param>
    /// <returns>Clave de categoría permitida o «todos».</returns>
    private static string NormalizarCategoria(string seccion, string? categoria)
    {
        // categoriaNormalizada normaliza la clave y evita filtros que no pertenecen a esta sección.
        var categoriaNormalizada = categoria?.Trim().ToLowerInvariant();
        // categoriaPermitida impide abrir filtros que no existan en la familia activa.
        var categoriaPermitida = seccion switch
        {
            "switches" => categoriaNormalizada is "areas" or "datacenter" or "reserva" or "sin-acceso" or "servidores",
            "usuarios" => categoriaNormalizada is "documentados" or "sin-documentar",
            _ => false
        };
        return categoriaPermitida ? categoriaNormalizada! : "todos";
    }

    /// <summary>
    /// Devuelve un título legible para la combinación activa de sección y categoría.
    /// </summary>
    /// <param name="seccion">Familia activa; cambiarla modifica el título principal.</param>
    /// <param name="categoria">Subfiltro activo; cambiarlo modifica el título secundario mostrado.</param>
    /// <returns>Nombre de la agrupación visible sobre la tabla.</returns>
    private static string ObtenerTituloVista(string seccion, string categoria)
    {
        // tituloCategoria traduce claves internas a nombres que entiende el técnico.
        var tituloCategoria = categoria switch
        {
            "areas" => "Switches de áreas",
            "datacenter" => "Switches de datacenter",
            "reserva" => "Switches en reserva",
            "sin-acceso" => "Switches sin acceso administrativo",
            "servidores" => "Switches de servidores",
            "documentados" => "Usuarios con conexión documentada a un switch",
            "sin-documentar" => "Usuarios sin conexión documentada a un switch",
            _ => string.Empty
        };
        // tituloSeccion mantiene nombres claros al navegar entre familias.
        var tituloSeccion = seccion switch
        {
            "switches" => "Switches",
            "usuarios" => "Usuarios",
            "otros" => "Otros equipos",
            _ => "Todos los equipos"
        };
        return string.IsNullOrEmpty(tituloCategoria) ? tituloSeccion : tituloCategoria;
    }

    /// <summary>
    /// Filtra las fichas por tipo y categoría usando solo campos y relaciones documentados.
    /// </summary>
    /// <param name="equipos">Resultados de la búsqueda; cambiar la lista cambia los candidatos de la tabla.</param>
    /// <param name="seccion">Familia elegida; cambiarla selecciona switches, usuarios, otros o todos.</param>
    /// <param name="categoria">Subfiltro normalizado; cambiarlo selecciona una categoría permitida.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la lectura de relaciones.</param>
    /// <returns>Fichas coincidentes con la familia y categoría solicitadas.</returns>
    private async Task<List<EquipoRed>> FiltrarPorSeccionAsync(
        List<EquipoRed> equipos, string seccion, string categoria, CancellationToken cancelacion)
    {
        // equiposDeSeccion restringe la lista por su tipo documentado, sin analizar nombres de equipo.
        var equiposDeSeccion = seccion switch
        {
            "switches" => equipos.Where(EsSwitch).ToList(),
            "usuarios" => equipos.Where(EsEquipoUsuario).ToList(),
            "otros" => equipos.Where(equipo => !EsSwitch(equipo) && !EsEquipoUsuario(equipo)).ToList(),
            _ => equipos
        };

        if (seccion == "switches")
        {
            return categoria switch
            {
                "areas" => equiposDeSeccion.Where(EsSwitchDeArea).ToList(),
                "datacenter" => equiposDeSeccion.Where(EsEquipoDeDatacenter).ToList(),
                "reserva" => equiposDeSeccion.Where(EsEquipoDeReserva).ToList(),
                "sin-acceso" => equiposDeSeccion.Where(EsSwitchInaccesible).ToList(),
                "servidores" => equiposDeSeccion.Where(EsSwitchDeServidores).ToList(),
                _ => equiposDeSeccion
            };
        }

        if (seccion != "usuarios" || categoria == "todos")
        {
            return equiposDeSeccion;
        }

        // usuariosConConexion contiene equipos finales vinculados explícitamente a un puerto de switch.
        var usuariosConConexion = await ObtenerUsuariosConSwitchDocumentadoAsync(cancelacion);
        return categoria == "documentados"
            ? equiposDeSeccion.Where(equipo => usuariosConConexion.Contains(equipo.Identificador)).ToList()
            : equiposDeSeccion.Where(equipo => !usuariosConConexion.Contains(equipo.Identificador)).ToList();
    }

    /// <summary>
    /// Identifica los equipos finales vinculados a un switch por una relación explícita.
    /// </summary>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene las consultas de puertos y conexiones.</param>
    /// <returns>Identificadores de equipos de usuario con conexión documentada a un switch.</returns>
    private async Task<HashSet<int>> ObtenerUsuariosConSwitchDocumentadoAsync(CancellationToken cancelacion)
    {
        // equiposRegistrados aporta identidades exactas para resolver destinos remotos sin hacer coincidencias parciales.
        var equiposRegistrados = await contextoInventario.Equipos.AsNoTracking().ToListAsync(cancelacion);
        // usuariosRegistrados limita el resultado a tipos de puesto de usuario reconocidos.
        var usuariosRegistrados = equiposRegistrados.Where(EsEquipoUsuario).ToList();
        // identificadoresSwitches se usa para reconocer los puertos locales pertenecientes a switches.
        var identificadoresSwitches = equiposRegistrados.Where(EsSwitch).Select(equipo => equipo.Identificador).ToHashSet();
        // interfacesRegistradas permite resolver ambos extremos de los enlaces físicos guardados.
        var interfacesRegistradas = await contextoInventario.InterfacesEquipo.AsNoTracking().ToListAsync(cancelacion);
        // puertosSwitch contiene los identificadores locales para buscar conexiones parciales del switch.
        var puertosSwitch = interfacesRegistradas
            .Where(interfaz => identificadoresSwitches.Contains(interfaz.IdentificadorEquipo))
            .Select(interfaz => interfaz.Identificador)
            .ToList();
        // relacionesRegistradas conserva destinos y puertos locales para filtrar con la lista de interfaces ya cargada.
        var relacionesRegistradas = await contextoInventario.ConexionesDocumentadas.AsNoTracking()
            .Select(relacion => new { relacion.IdentificadorPuertoLocal, relacion.EquipoRemoto, relacion.IpRemota })
            .ToListAsync(cancelacion);
        // identificadoresPuertosSwitch evita parámetros SQL por puerto y conserva solo enlaces locales de switches.
        var identificadoresPuertosSwitch = puertosSwitch.ToHashSet();
        // relacionesParciales conserva los destinos escritos desde puertos de switch.
        var relacionesParciales = relacionesRegistradas
            .Where(relacion => identificadoresPuertosSwitch.Contains(relacion.IdentificadorPuertoLocal))
            .ToList();
        // usuariosDocumentados acumula identificadores con una relación exacta por nombre o IP.
        var usuariosDocumentados = new HashSet<int>();

        // relacion recorre destinos documentados desde un puerto de switch.
        foreach (var relacion in relacionesParciales)
        {
            // usuario compara cada destino con las identidades de puestos persistidas.
            foreach (var usuario in usuariosRegistrados)
            {
                if ((!string.IsNullOrWhiteSpace(relacion.EquipoRemoto)
                        && string.Equals(NormalizarClaveComparacion(relacion.EquipoRemoto), NormalizarClaveComparacion(usuario.Nombre), StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(relacion.IpRemota)
                        && string.Equals(NormalizarClaveComparacion(relacion.IpRemota), NormalizarClaveComparacion(usuario.IpAdministracion), StringComparison.OrdinalIgnoreCase)))
                {
                    usuariosDocumentados.Add(usuario.Identificador);
                }
            }
        }

        // equiposPorIdentificador resuelve el tipo de ambos extremos de cada enlace completo.
        var equiposPorIdentificador = equiposRegistrados.ToDictionary(equipo => equipo.Identificador);
        // interfacesPorIdentificador resuelve el propietario de cada puerto enlazado.
        var interfacesPorIdentificador = interfacesRegistradas.ToDictionary(interfaz => interfaz.Identificador);
        // enlacesFisicos contiene únicamente relaciones con ambos puertos persistidos.
        var enlacesFisicos = await contextoInventario.EnlacesFisicos.AsNoTracking().ToListAsync(cancelacion);

        // enlace recorre relaciones completas entre interfaces existentes.
        foreach (var enlace in enlacesFisicos)
        {
            // Los extremos se resuelven a interfaz y equipo; un extremo faltante no se clasifica como conexión confirmada.
            if (!interfacesPorIdentificador.TryGetValue(enlace.IdentificadorPuertoOrigen, out var puertoOrigen)
                || !interfacesPorIdentificador.TryGetValue(enlace.IdentificadorPuertoDestino, out var puertoDestino)
                || !equiposPorIdentificador.TryGetValue(puertoOrigen.IdentificadorEquipo, out var equipoOrigen)
                || !equiposPorIdentificador.TryGetValue(puertoDestino.IdentificadorEquipo, out var equipoDestino))
            {
                continue;
            }

            if (EsSwitch(equipoOrigen) && EsEquipoUsuario(equipoDestino))
            {
                usuariosDocumentados.Add(equipoDestino.Identificador);
            }
            else if (EsEquipoUsuario(equipoOrigen) && EsSwitch(equipoDestino))
            {
                usuariosDocumentados.Add(equipoOrigen.Identificador);
            }
        }

        return usuariosDocumentados;
    }

    /// <summary>
    /// Determina si el tipo de ficha indica un switch.
    /// </summary>
    /// <param name="equipo">Ficha evaluada; cambiar su tipo puede moverla entre secciones.</param>
    /// <returns>Verdadero solo para el tipo Switch.</returns>
    private static bool EsSwitch(EquipoRed equipo)
    {
        return string.Equals(NormalizarClaveComparacion(equipo.TipoDispositivo), "Switch", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determina si el tipo de ficha es un puesto computacional de usuario.
    /// </summary>
    /// <param name="equipo">Ficha evaluada; cambiar su tipo puede moverla entre Usuarios y Otros equipos.</param>
    /// <returns>Verdadero para los tipos explícitos de puesto o computadora.</returns>
    private static bool EsEquipoUsuario(EquipoRed equipo)
    {
        // tipoNormalizado permite comparar variantes conocidas sin depender del nombre del PC.
        var tipoNormalizado = NormalizarClaveComparacion(equipo.TipoDispositivo);
        return tipoNormalizado is "PUESTO DE USUARIO" or "EQUIPO DE USUARIO" or "PC"
            or "COMPUTADORA" or "COMPUTADOR" or "ESTACIÓN DE TRABAJO" or "ESTACION DE TRABAJO" or "USUARIO";
    }

    /// <summary>
    /// Reconoce el rol operativo de un switch de área.
    /// </summary>
    /// <param name="equipo">Ficha evaluada; cambiar RolEnArea cambia su clasificación.</param>
    /// <returns>Verdadero si el rol o el área documentada lo clasifica como switch de oficina.</returns>
    private static bool EsSwitchDeArea(EquipoRed equipo)
    {
        // rolNormalizado conserva el significado funcional separado del modelo.
        var rolNormalizado = NormalizarClaveComparacion(equipo.RolEnArea);
        return (rolNormalizado is "SWITCH DE ÁREA" or "SWITCH DE AREA")
            || (!string.IsNullOrWhiteSpace(equipo.Area)
                && !EsEquipoDeDatacenter(equipo)
                && !EsEquipoDeReserva(equipo)
                && !EsSwitchDeServidores(equipo));
    }

    /// <summary>
    /// Reconoce equipos ubicados en el rack o sector de datacenter.
    /// </summary>
    /// <param name="equipo">Ficha evaluada; cambiar su área modifica esta categoría.</param>
    /// <returns>Verdadero para áreas documentadas como rack o centro de datos.</returns>
    private static bool EsEquipoDeDatacenter(EquipoRed equipo)
    {
        // areaNormalizada usa la ubicación persistida y no supone datacenter por el modelo del switch.
        var areaNormalizada = NormalizarClaveComparacion(equipo.Area);
        return areaNormalizada is "RACK" or "DATACENTER" or "DATA CENTER" or "CENTRO DE DATOS";
    }

    /// <summary>
    /// Reconoce la categoría de reserva por el área o el rol persistido.
    /// </summary>
    /// <param name="equipo">Ficha evaluada; cambiar área o rol actualiza la agrupación.</param>
    /// <returns>Verdadero para equipos asignados a reserva.</returns>
    private static bool EsEquipoDeReserva(EquipoRed equipo)
    {
        return string.Equals(NormalizarClaveComparacion(equipo.Area), "Reserva", StringComparison.OrdinalIgnoreCase)
            || string.Equals(NormalizarClaveComparacion(equipo.RolEnArea), "Reserva", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reconoce switches cuyo acceso administrativo está documentado como inaccesible.
    /// </summary>
    /// <param name="equipo">Ficha evaluada; cambiar el estado administrativo modifica el resultado.</param>
    /// <returns>Verdadero cuando falta acceso por credenciales u otra razón documentada.</returns>
    private static bool EsSwitchInaccesible(EquipoRed equipo)
    {
        return equipo.EstadoAdministrativo.Contains("Inaccesible", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reconoce switches que cumplen una función documentada de servidores o puente al servidor principal.
    /// </summary>
    /// <param name="equipo">Ficha evaluada; cambiar el rol operativo modifica su agrupación.</param>
    /// <returns>Verdadero si el rol documenta servicios de servidor o puente al Dell R770.</returns>
    private static bool EsSwitchDeServidores(EquipoRed equipo)
    {
        // rolNormalizado identifica las funciones ya documentadas para el HP 5500 y el Dell S4148U.
        var rolNormalizado = NormalizarClaveComparacion(equipo.RolEnArea);
        return rolNormalizado.Contains("SERVIDOR", StringComparison.OrdinalIgnoreCase)
            || rolNormalizado.Contains("PUENTE HACIA DELL R770", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normaliza espacios externos e internos para cotejar etiquetas documentales exactas.
    /// </summary>
    /// <param name="valor">Texto de fuente; cambiarlo cambia la clave exacta usada en las comparaciones.</param>
    /// <returns>Texto con espacios consecutivos reducidos y sin espacios externos.</returns>
    private static string NormalizarClaveComparacion(string? valor)
    {
        // claveNormalizada conserva todo el dato salvo espacios redundantes para evitar coincidencias parciales.
        var claveNormalizada = string.Join(" ", (valor ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return claveNormalizada.Trim();
    }

    /// <summary>
    /// Prepara un campo CSV entre comillas y neutraliza prefijos que algunas hojas interpretarían como fórmula.
    /// </summary>
    /// <param name="texto">Valor original; cambiarlo modifica el campo escrito en el CSV.</param>
    /// <returns>Campo entrecomillado y seguro para exportación textual.</returns>
    private static string EscaparCampoCsv(string? texto)
    {
        // valorSeguro conserva un texto vacío cuando el campo no está definido; cambiarlo altera el dato exportado.
        var valorSeguro = texto ?? string.Empty;

        // textoSignificativo permite detectar fórmulas después de espacios; cambiar la regla afecta la neutralización preventiva.
        var textoSignificativo = valorSeguro.TrimStart();
        if (textoSignificativo.StartsWith('=') || textoSignificativo.StartsWith('+')
            || textoSignificativo.StartsWith('-') || textoSignificativo.StartsWith('@'))
        {
            valorSeguro = "'" + valorSeguro;
        }

        // textoEntrecomillado duplica comillas internas; cambiar el escape altera la lectura de los campos CSV.
        var textoEntrecomillado = valorSeguro.Replace("\"", "\"\"");
        return "\"" + textoEntrecomillado + "\"";
    }

    /// <summary>
    /// Determina si un equipo coincide con el texto escrito en la búsqueda.
    /// </summary>
    /// <param name="equipo">Equipo que se evalúa; cambiar sus campos modifica las posibles coincidencias.</param>
    /// <param name="busqueda">Texto que se compara; cambiarlo modifica el resultado de la búsqueda.</param>
    /// <returns>Verdadero cuando coincide nombre, IP, área, modelo o fabricante.</returns>
    private static bool CoincideConBusqueda(EquipoRed equipo, string busqueda)
    {
        return Contiene(equipo.Nombre, busqueda)
            || Contiene(equipo.IpAdministracion, busqueda)
            || Contiene(equipo.Area, busqueda)
            || Contiene(equipo.Piso, busqueda)
            || Contiene(equipo.TipoDispositivo, busqueda)
            || Contiene(equipo.Modelo, busqueda)
            || Contiene(equipo.Fabricante, busqueda);
    }

    /// <summary>
    /// Comprueba una coincidencia textual sin distinguir mayúsculas.
    /// </summary>
    /// <param name="valor">Campo del equipo; cambiarlo cambia el texto comparado.</param>
    /// <param name="busqueda">Texto buscado; cambiarlo cambia el criterio de coincidencia.</param>
    /// <returns>Verdadero si el campo contiene el texto buscado.</returns>
    private static bool Contiene(string? valor, string busqueda)
    {
        return valor?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) == true;
    }
    /// <summary>
    /// Audita cantidades de conectores cuando se crea o edita un switch.
    /// </summary>
    /// <param name="cambios">Lista destino; cambiarla cambia dónde quedan los eventos de capacidad.</param>
    /// <param name="equipo">Ficha asociada; cambiarla atribuye los eventos a otro equipo.</param>
    /// <param name="usuarioEditor">Autor del cambio; cambiarlo modifica la atribución histórica.</param>
    /// <param name="formulario">Formulario editado; nulo indica que se está registrando el alta inicial.</param>
    private static void AgregarCambiosCapacidad(List<EventoModificacion> cambios, EquipoRed equipo,
        string usuarioEditor, EditarEquipoViewModel? formulario)
    {
        // nombresCampos mantiene los rótulos del historial alineados con cada familia de conectores.
        var nombresCampos = new[] { "Puertos RJ45", "Puertos SFP", "Puertos SFP+", "Puertos QSFP", "Puertos QSFP+", "Puertos QSFP28" };
        // cantidadesAnteriores usa nulos para un alta y los valores persistidos para una edición.
        var cantidadesAnteriores = formulario is null
            ? new int?[] { null, null, null, null, null, null }
            : new int?[] { equipo.CantidadPuertosRj45, equipo.CantidadPuertosSfp, equipo.CantidadPuertosSfpPlus,
                equipo.CantidadPuertosQsfp, equipo.CantidadPuertosQsfpPlus, equipo.CantidadPuertosQsfp28 };
        // cantidadesNuevas toma la ficha recién creada o las propiedades recibidas del formulario.
        var cantidadesNuevas = formulario is null
            ? new int?[] { equipo.CantidadPuertosRj45, equipo.CantidadPuertosSfp, equipo.CantidadPuertosSfpPlus,
                equipo.CantidadPuertosQsfp, equipo.CantidadPuertosQsfpPlus, equipo.CantidadPuertosQsfp28 }
            : new int?[] { formulario.CantidadPuertosRj45, formulario.CantidadPuertosSfp, formulario.CantidadPuertosSfpPlus,
                formulario.CantidadPuertosQsfp, formulario.CantidadPuertosQsfpPlus, formulario.CantidadPuertosQsfp28 };

        for (var indiceCapacidad = 0; indiceCapacidad < nombresCampos.Length; indiceCapacidad++)
        {
            // cambioCapacidad guarda una sola diferencia de conectores en el historial común.
            UtilidadesConexiones.AgregarCambio(cambios, equipo.Identificador, equipo.Nombre, usuarioEditor,
                nombresCampos[indiceCapacidad],
                cantidadesAnteriores[indiceCapacidad]?.ToString(CultureInfo.InvariantCulture),
                cantidadesNuevas[indiceCapacidad]?.ToString(CultureInfo.InvariantCulture));
        }
    }

}
