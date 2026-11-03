namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Reúne los conteos y enlaces físicos disponibles para la página de topología.
/// </summary>
public class TopologiaViewModel
{
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
    /// Fecha real de revisión; cambiarla altera la fecha visible del enlace.
    /// </summary>
    public DateOnly? FechaUltimaRevision { get; set; }
}
