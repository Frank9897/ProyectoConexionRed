namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Registra un intento de inicio de sesión o cierre de sesión sin almacenar contraseñas.
/// </summary>
public class EventoAcceso
{
    /// <summary>
    /// Identificador del evento; cambiarlo altera su referencia en el historial.
    /// </summary>
    public int Identificador { get; set; }

    /// <summary>
    /// Nombre de cuenta presentado; cambiarlo modifica el usuario mostrado en el registro de acceso.
    /// </summary>
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>
    /// Fecha UTC del evento; cambiarla altera el orden cronológico del historial.
    /// </summary>
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tipo de evento registrado; cambiarlo modifica si el historial indica ingreso o cierre.
    /// </summary>
    public string TipoEvento { get; set; } = string.Empty;

    /// <summary>
    /// Resultado del evento; cambiarlo modifica si aparece como correcto o fallido.
    /// </summary>
    public string Resultado { get; set; } = string.Empty;
}
