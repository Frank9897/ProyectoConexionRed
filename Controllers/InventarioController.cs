using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;

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
    /// Lista equipos y aplica una búsqueda opcional por nombre, IP, área, modelo o fabricante.
    /// </summary>
    /// <param name="busqueda">Texto de búsqueda; cambiarlo filtra qué equipos aparecen en la vista.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la consulta pendiente.</param>
    /// <returns>Vista con los equipos coincidentes; modificar la búsqueda cambia el conjunto mostrado.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(string? busqueda, CancellationToken cancelacion)
    {
        // equiposEncontrados contiene la búsqueda actual; cambiar su contenido cambia las filas de la página.
        var equiposEncontrados = await ObtenerEquiposAsync(busqueda, cancelacion);
        return View(equiposEncontrados);
    }

    /// <summary>
    /// Descarga como CSV los equipos que coinciden con la búsqueda actual.
    /// </summary>
    /// <param name="busqueda">Texto de búsqueda; cambiarlo filtra las filas incluidas en el archivo.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la consulta pendiente.</param>
    /// <returns>Archivo CSV UTF-8 con los resultados; cambiar búsqueda altera el contenido exportado.</returns>
    [HttpGet]
    public async Task<IActionResult> ExportarCsv(string? busqueda, CancellationToken cancelacion)
    {
        // equiposExportados contiene las filas descargables; cambiar la búsqueda cambia sus datos.
        var equiposExportados = await ObtenerEquiposAsync(busqueda, cancelacion);

        // contenidoCsv acumula el archivo; cambiar encabezados o separadores modifica cómo se interpreta en hojas de cálculo.
        var contenidoCsv = new StringBuilder();
        contenidoCsv.AppendLine("Equipo;Fabricante;Modelo;Área;IP de administración;MAC;Rol;Tipo;Acceso administrativo;Vigencia");

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
                equipo.IpAdministracion,
                equipo.DireccionMac,
                equipo.RolEnArea,
                equipo.TipoDispositivo,
                equipo.EstadoAdministrativo,
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
            RolEnArea = equipoEncontrado.RolEnArea,
            TipoDispositivo = equipoEncontrado.TipoDispositivo,
            EstadoAdministrativo = equipoEncontrado.EstadoAdministrativo,
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

        AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Nombre", equipoGuardado.Nombre, formulario.Nombre);
        AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Fabricante", equipoGuardado.Fabricante, formulario.Fabricante);
        AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Modelo", equipoGuardado.Modelo, formulario.Modelo);
        AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "IP de administración", equipoGuardado.IpAdministracion, formulario.IpAdministracion);
        AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Dirección MAC", equipoGuardado.DireccionMac, formulario.DireccionMac);
        AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Área", equipoGuardado.Area, formulario.Area);
        AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Rol en el área", equipoGuardado.RolEnArea, formulario.RolEnArea);
        AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Tipo de dispositivo", equipoGuardado.TipoDispositivo, formulario.TipoDispositivo);
        AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Acceso administrativo", equipoGuardado.EstadoAdministrativo, formulario.EstadoAdministrativo);
        AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Vigencia de datos", equipoGuardado.EstadoDatos, formulario.EstadoDatos);
        AgregarCambio(
            cambiosRegistrados,
            equipoGuardado.Identificador,
            nombreEquipoAnterior,
            usuarioEditor,
            "Última revisión",
            equipoGuardado.FechaUltimaRevision?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            formulario.FechaUltimaRevision?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AgregarCambio(cambiosRegistrados, equipoGuardado.Identificador, nombreEquipoAnterior, usuarioEditor, "Observaciones", equipoGuardado.Observaciones, formulario.Observaciones);

        equipoGuardado.Nombre = formulario.Nombre.Trim();
        equipoGuardado.Fabricante = NormalizarTextoOpcional(formulario.Fabricante);
        equipoGuardado.Modelo = NormalizarTextoOpcional(formulario.Modelo);
        equipoGuardado.IpAdministracion = NormalizarTextoOpcional(formulario.IpAdministracion);
        equipoGuardado.DireccionMac = NormalizarTextoOpcional(formulario.DireccionMac);
        equipoGuardado.Area = NormalizarTextoOpcional(formulario.Area);
        equipoGuardado.RolEnArea = formulario.RolEnArea.Trim();
        equipoGuardado.TipoDispositivo = formulario.TipoDispositivo.Trim();
        equipoGuardado.EstadoAdministrativo = formulario.EstadoAdministrativo.Trim();
        equipoGuardado.EstadoDatos = formulario.EstadoDatos.Trim();
        equipoGuardado.FechaUltimaRevision = formulario.FechaUltimaRevision;
        equipoGuardado.Observaciones = NormalizarTextoOpcional(formulario.Observaciones);

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
    private static void AgregarCambio(
        List<EventoModificacion> cambios,
        int identificadorEquipo,
        string nombreEquipo,
        string usuarioEditor,
        string campo,
        string? valorAnterior,
        string? valorNuevo)
    {
        // valorAnteriorNormalizado y valorNuevoNormalizado coinciden con el recorte aplicado al guardar la ficha.
        var valorAnteriorNormalizado = NormalizarTextoOpcional(valorAnterior);
        var valorNuevoNormalizado = NormalizarTextoOpcional(valorNuevo);
        if (string.Equals(valorAnteriorNormalizado, valorNuevoNormalizado, StringComparison.Ordinal))
        {
            return;
        }

        // eventoCambio almacena la diferencia textual; cambiar estos campos modifica la línea de auditoría.
        var eventoCambio = new EventoModificacion
        {
            IdentificadorEquipo = identificadorEquipo,
            NombreEquipo = nombreEquipo,
            NombreUsuario = usuarioEditor,
            FechaUtc = DateTime.UtcNow,
            Campo = campo,
            ValorAnterior = valorAnteriorNormalizado,
            ValorNuevo = valorNuevoNormalizado
        };
        cambios.Add(eventoCambio);
    }

    /// <summary>
    /// Convierte textos opcionales compuestos por espacios en valor nulo.
    /// </summary>
    /// <param name="texto">Texto recibido; cambiarlo cambia el valor almacenado.</param>
    /// <returns>Texto recortado o nulo cuando no tiene contenido.</returns>
    private static string? NormalizarTextoOpcional(string? texto)
    {
        // textoRecortado elimina espacios externos; cambiar esta regla altera la forma guardada.
        var textoRecortado = texto?.Trim();
        return string.IsNullOrEmpty(textoRecortado) ? null : textoRecortado;
    }

    /// <summary>
    /// Obtiene todos los equipos o filtra por nombre, IP, área, modelo y fabricante.
    /// </summary>
    /// <param name="busqueda">Texto opcional; cambiarlo modifica el subconjunto devuelto.</param>
    /// <param name="cancelacion">Señal que cancela la consulta; cambiarla afecta la duración permitida.</param>
    /// <returns>Lista de equipos coincidentes; modificar datos o búsqueda modifica los elementos devueltos.</returns>
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
}
