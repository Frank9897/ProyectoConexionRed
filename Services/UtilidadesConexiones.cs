using System.Net;
using System.Net.Sockets;
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
    /// <summary>
    /// Obtiene direcciones libres de las redes 192.168.0.x y 192.168.1.x, conservando la dirección actual.
    /// </summary>
    /// <param name="contextoInventario">Base documental; cambiarla altera las direcciones que se consideran ocupadas.</param>
    /// <param name="ipActual">IP vigente; cambiarla conserva la opción elegida aunque ya esté asignada.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la consulta.</param>
    /// <returns>Opciones de IP libres ordenadas por subred.</returns>
    public static async Task<List<SelectListItem>> ObtenerIpsLibresAsync(InventarioDbContext contextoInventario,
        string? ipActual, CancellationToken cancelacion)
    {
        // ipsRegistradas reúne direcciones ya usadas por equipos y relaciones documentales.
        var ipsRegistradas = await contextoInventario.Equipos.AsNoTracking()
            .Where(equipo => equipo.IpAdministracion != null)
            .Select(equipo => equipo.IpAdministracion!)
            .ToListAsync(cancelacion);
        // ipsRemotas registra IP remotas ya usadas por otras relaciones parciales.
        var ipsRemotas = await contextoInventario.ConexionesDocumentadas.AsNoTracking()
            .Where(relacion => relacion.IpRemota != null)
            .Select(relacion => relacion.IpRemota!)
            .ToListAsync(cancelacion);
        // ipsOcupadas permite verificar direcciones sin repetir búsquedas en SQLite.
        var ipsOcupadas = ipsRegistradas.Concat(ipsRemotas).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // ipsDisponibles agrupa las direcciones libres de las dos subredes permitidas.
        var ipsDisponibles = new List<SelectListItem>();
        // segundoOcteto recorre los segmentos de usuario que se pueden asignar desde el formulario.
        foreach (var segundoOcteto in new[] { 0, 1 })
        {
            // ultimoOcteto recorre hosts utilizables, sin proponer la dirección de red ni broadcast.
            for (var ultimoOcteto = 1; ultimoOcteto <= 254; ultimoOcteto++)
            {
                // ipCandidata forma la dirección que se contrasta con el inventario vigente.
                var ipCandidata = $"192.168.{segundoOcteto}.{ultimoOcteto}";
                if (!ipsOcupadas.Contains(ipCandidata)
                    || string.Equals(ipCandidata, ipActual, StringComparison.OrdinalIgnoreCase))
                {
                    ipsDisponibles.Add(new SelectListItem
                    {
                        Value = ipCandidata,
                        Text = ipCandidata + (ipsOcupadas.Contains(ipCandidata) ? " · actual" : " · libre"),
                        Selected = false
                    });
                }
            }
        }

        // ipActualEsPersonalizada conserva el dato actual en caso de que no pertenezca a las opciones sugeridas.
        var ipActualEsPersonalizada = !string.IsNullOrWhiteSpace(ipActual)
            && !string.Equals(ipActual, "Desconocido", StringComparison.OrdinalIgnoreCase)
            && !ipsDisponibles.Any(opcion => string.Equals(opcion.Value, ipActual, StringComparison.OrdinalIgnoreCase));
        if (ipActualEsPersonalizada)
        {
            ipsDisponibles.Insert(0, new SelectListItem
            {
                Value = ipActual!,
                Text = ipActual + " · actual",
                Selected = false
            });
        }

        return ipsDisponibles;
    }

    /// <summary>
    /// Acepta solo las direcciones IPv4 privadas indicadas por el usuario o el estado explícito Desconocido.
    /// </summary>
    /// <param name="ip">Dirección enviada; cambiarla altera el destino guardado.</param>
    /// <returns>Verdadero para vacío, Desconocido o una IP 192.168.0.1-254/192.168.1.1-254.</returns>
    public static bool EsIpPermitida(string? ip)
    {
        // ipNormalizada elimina espacios antes de validar el valor manual.
        var ipNormalizada = ip?.Trim();
        if (string.IsNullOrEmpty(ipNormalizada)
            || string.Equals(ipNormalizada, "Desconocido", StringComparison.OrdinalIgnoreCase))
            return true;

        // ipAnalizada representa la IPv4 en bytes para aplicar el rango permitido.
        if (!IPAddress.TryParse(ipNormalizada, out var ipAnalizada)
            || ipAnalizada.AddressFamily != AddressFamily.InterNetwork)
            return false;

        // octetosIP permite comprobar las dos redes privadas y evitar hosts reservados.
        var octetosIP = ipAnalizada.GetAddressBytes();
        return octetosIP[0] == 192
            && octetosIP[1] == 168
            && octetosIP[2] is 0 or 1
            && octetosIP[3] is >= 1 and <= 254;
    }
}
