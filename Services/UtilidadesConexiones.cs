using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Services;

/// <summary>
/// Reúne operaciones comunes para formularios, normalización y auditoría de conexiones.
/// </summary>
public static class UtilidadesConexiones
{
    /// <summary>
    /// Recorta un texto opcional y convierte una cadena vacía en nulo.
    /// </summary>
    /// <param name="texto">Valor recibido; cambiarlo cambia el texto que se normaliza.</param>
    /// <returns>Texto sin espacios externos o nulo cuando queda vacío.</returns>
    public static string? NormalizarTexto(string? texto)
    {
        // textoNormalizado fija una sola regla para campos opcionales.
        var textoNormalizado = texto?.Trim();
        return string.IsNullOrEmpty(textoNormalizado) ? null : textoNormalizado;
    }

    /// <summary>
    /// Añade al historial una diferencia de campo cuando los valores realmente cambiaron.
    /// </summary>
    /// <param name="eventos">Colección destino; cambiarla determina dónde se acumulan los cambios.</param>
    /// <param name="identificadorEquipo">ID asociado; cambiarlo atribuye el evento a otra ficha.</param>
    /// <param name="nombreEquipo">Nombre asociado; cambiarlo cambia la etiqueta de auditoría.</param>
    /// <param name="usuarioEditor">Autor; cambiarlo modifica quién figura en el historial.</param>
    /// <param name="campo">Nombre del campo; cambiarlo altera la descripción del evento.</param>
    /// <param name="valorAnterior">Valor previo; cambiarlo modifica el origen de la comparación.</param>
    /// <param name="valorNuevo">Valor resultante; cambiarlo modifica el destino de la comparación.</param>
    public static void AgregarCambio(List<EventoModificacion> eventos, int identificadorEquipo, string nombreEquipo,
        string usuarioEditor, string campo, string? valorAnterior, string? valorNuevo)
    {
        // valoresNormalizados evita eventos causados solo por espacios externos.
        var valorAnteriorNormalizado = NormalizarTexto(valorAnterior);
        var valorNuevoNormalizado = NormalizarTexto(valorNuevo);
        if (string.Equals(valorAnteriorNormalizado, valorNuevoNormalizado, StringComparison.Ordinal)) return;

        // eventoCambio conserva la diferencia de un campo en el historial común.
        var eventoCambio = new EventoModificacion
        {
            IdentificadorEquipo = identificadorEquipo, NombreEquipo = nombreEquipo, NombreUsuario = usuarioEditor,
            FechaUtc = DateTime.UtcNow, Campo = campo, ValorAnterior = valorAnteriorNormalizado, ValorNuevo = valorNuevoNormalizado
        };
        eventos.Add(eventoCambio);
    }

    /// <summary>
    /// Consulta equipos para los selectores de asignación de puertos.
    /// </summary>
    /// <param name="contextoInventario">Base documental; cambiarla cambia las opciones consultadas.</param>
    /// <param name="identificadorSeleccionado">ID preseleccionado; cambiarlo marca otra opción.</param>
    /// <param name="cancelacion">Señal de cancelación de la consulta.</param>
    /// <returns>Opciones ordenadas con nombre, modelo e IP administrativa.</returns>
    public static async Task<List<SelectListItem>> ObtenerEquiposDisponiblesAsync(InventarioDbContext contextoInventario,
        int? identificadorSeleccionado, CancellationToken cancelacion)
    {
        // equiposDisponibles limita las opciones a fichas del inventario.
        var equiposDisponibles = await contextoInventario.Equipos.AsNoTracking().OrderBy(equipo => equipo.Nombre)
            .Select(equipo => new { equipo.Identificador, equipo.Nombre, equipo.Modelo, equipo.IpAdministracion })
            .ToListAsync(cancelacion);
        return equiposDisponibles.Select(equipo => new SelectListItem
        {
            Value = equipo.Identificador.ToString(),
            Text = string.Join(" · ", new[] { equipo.Nombre, equipo.Modelo, equipo.IpAdministracion }
                .Where(texto => !string.IsNullOrWhiteSpace(texto))),
            Selected = identificadorSeleccionado == equipo.Identificador
        }).ToList();
    }

    /// <summary>
    /// Consulta interfaces con equipo e IP para los selectores de extremos.
    /// </summary>
    /// <param name="contextoInventario">Base documental; cambiarla cambia los puertos consultados.</param>
    /// <param name="identificadorSeleccionado">ID preseleccionado; cambiarlo marca otra interfaz.</param>
    /// <param name="cancelacion">Señal de cancelación de la consulta.</param>
    /// <returns>Opciones ordenadas con equipo, IP, puerto y estado.</returns>
    public static async Task<List<SelectListItem>> ObtenerInterfacesDisponiblesAsync(InventarioDbContext contextoInventario,
        int? identificadorSeleccionado, CancellationToken cancelacion)
    {
        // interfacesDisponibles relaciona cada puerto con su equipo propietario.
        var interfacesDisponibles = await (
            from interfaz in contextoInventario.InterfacesEquipo.AsNoTracking()
            join equipo in contextoInventario.Equipos.AsNoTracking() on interfaz.IdentificadorEquipo equals equipo.Identificador
            where !contextoInventario.ConexionesDocumentadas.Any(relacion =>
                relacion.IdentificadorPuertoLocal == interfaz.Identificador)
            orderby equipo.Nombre, interfaz.EtiquetaPuerto
            select new { interfaz.Identificador, interfaz.EtiquetaPuerto, equipo.Nombre, equipo.IpAdministracion, interfaz.EstadoDatos })
            .ToListAsync(cancelacion);
        return interfacesDisponibles.Select(interfaz => new SelectListItem
        {
            Value = interfaz.Identificador.ToString(),
            Text = string.Join(" · ", new[] { interfaz.Nombre, interfaz.IpAdministracion, interfaz.EtiquetaPuerto, interfaz.EstadoDatos }
                .Where(texto => !string.IsNullOrWhiteSpace(texto))),
            Selected = identificadorSeleccionado == interfaz.Identificador
        }).ToList();
    }
}
