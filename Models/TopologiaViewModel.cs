namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Reúne los conteos y enlaces físicos disponibles para la página de topología.
/// </summary>
public class TopologiaViewModel
{
    /// <summary>
    /// Total de fichas de equipo; cambiarlo actualiza el indicador global de inventario en topología.
    /// </summary>
    public int TotalEquipos { get; set; }

    /// <summary>
    /// Total de switches clasificados; cambiarlo actualiza el resumen de la capa de switching.
    /// </summary>
    public int TotalSwitches { get; set; }

    /// <summary>
    /// Total de puestos de usuario registrados; cambiarlo actualiza el resumen de acceso.
    /// </summary>
    public int TotalPuestos { get; set; }

    /// <summary>
    /// Puestos de usuario que tienen una IP documentada; cambiarlo actualiza el indicador de asignación IP.
    /// </summary>
    public int PuestosConIp { get; set; }

    /// <summary>
    /// Switches cuyo acceso administrativo está marcado inaccesible; cambiarlo actualiza el aviso de gestión.
    /// </summary>
    public int SwitchesInaccesibles { get; set; }

    /// <summary>
    /// Cantidad de interfaces registradas; cambiarla actualiza el resumen de puertos documentados.
    /// </summary>
    public int TotalInterfaces { get; set; }

    /// <summary>
    /// Cantidad de enlaces físicos; cambiarla actualiza el resumen de conexiones documentadas.
    /// </summary>
    public int TotalEnlaces { get; set; }

    /// <summary>
    /// Enlaces físicos completos; cambiar la lista cambia las filas que se dibujan o muestran.
    /// </summary>
    public IReadOnlyList<EnlaceTopologiaViewModel> Enlaces { get; set; } = [];
}

/// <summary>
/// Presenta los dos extremos y el estado de un enlace físico.
/// </summary>
public class EnlaceTopologiaViewModel
{
    /// <summary>
    /// Equipo del extremo de origen; cambiarlo altera la etiqueta del primer dispositivo.
    /// </summary>
    public string EquipoOrigen { get; set; } = string.Empty;

    /// <summary>
    /// Puerto del extremo de origen; cambiarlo modifica la interfaz señalada.
    /// </summary>
    public string PuertoOrigen { get; set; } = string.Empty;

    /// <summary>
    /// Equipo del extremo de destino; cambiarlo altera la etiqueta del segundo dispositivo.
    /// </summary>
    public string EquipoDestino { get; set; } = string.Empty;

    /// <summary>
    /// Puerto del extremo de destino; cambiarlo modifica la interfaz enlazada.
    /// </summary>
    public string PuertoDestino { get; set; } = string.Empty;

    /// <summary>
    /// Medio documentado; cambiarlo modifica la descripción del enlace.
    /// </summary>
    public string Medio { get; set; } = string.Empty;

    /// <summary>
    /// Estado de validación; cambiarlo modifica el aviso de confianza de la relación.
    /// </summary>
    public string EstadoDatos { get; set; } = string.Empty;

    /// <summary>
    /// Fuente del enlace; cambiarla modifica la referencia documental mostrada junto a la conexión.
    /// </summary>
    public string? Fuente { get; set; }

    /// <summary>
    /// Fecha real de revisión; cambiarla altera la fecha visible del enlace.
    /// </summary>
    public DateOnly? FechaUltimaRevision { get; set; }
}
