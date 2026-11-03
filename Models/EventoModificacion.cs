namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Conserva un cambio documental aplicado a un equipo, sin ejecutar acciones en la red real.
/// </summary>
public class EventoModificacion
{
    /// <summary>
    /// Identificador interno del cambio; cambiarlo altera su referencia en el historial.
    /// </summary>
    public int Identificador { get; set; }

    /// <summary>
    /// Identificador del equipo editado; cambiarlo asocia el cambio a otro registro.
    /// </summary>
    public int IdentificadorEquipo { get; set; }

    /// <summary>
    /// Nombre del equipo al momento de la edición; cambiarlo altera la etiqueta histórica mostrada.
    /// </summary>
    public string NombreEquipo { get; set; } = string.Empty;

    /// <summary>
    /// Cuenta que realizó el cambio; cambiarla altera la atribución registrada.
    /// </summary>
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>
    /// Fecha UTC del cambio; cambiarla altera el orden cronológico del historial.
    /// </summary>
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Nombre del campo modificado; cambiarlo altera qué propiedad describe el registro.
    /// </summary>
    public string Campo { get; set; } = string.Empty;

    /// <summary>
    /// Valor anterior del campo; cambiarlo altera la comparación histórica.
    /// </summary>
    public string? ValorAnterior { get; set; }

    /// <summary>
    /// Valor nuevo del campo; cambiarlo altera el resultado documentado.
    /// </summary>
    public string? ValorNuevo { get; set; }
}
