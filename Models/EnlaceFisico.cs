namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Representa un enlace físico documentado entre dos interfaces.
/// </summary>
public class EnlaceFisico
{
    /// <summary>
    /// Identificador interno del enlace; cambiarlo altera su referencia en la topología.
    /// </summary>
    public int Identificador { get; set; }

    /// <summary>
    /// Interfaz de origen; cambiarla conecta el enlace a otro puerto.
    /// </summary>
    public int IdentificadorPuertoOrigen { get; set; }

    /// <summary>
    /// Interfaz de destino; cambiarla conecta el otro extremo a un puerto distinto.
    /// </summary>
    public int IdentificadorPuertoDestino { get; set; }

    /// <summary>
    /// Medio físico documentado; cambiarlo modifica cómo se representa el enlace.
    /// </summary>
    public string Medio { get; set; } = "No confirmado";

    /// <summary>
    /// Transceptor o SFP del extremo de origen; cambiarlo actualiza un modelo solo si está verificado.
    /// </summary>
    public string? ModuloOrigen { get; set; }

    /// <summary>
    /// Transceptor o SFP del extremo de destino; cambiarlo actualiza un modelo solo si está verificado.
    /// </summary>
    public string? ModuloDestino { get; set; }

    /// <summary>
    /// Estado de validación del enlace; cambiarlo altera si aparece como confirmado, pendiente o para revisar.
    /// </summary>
    public string EstadoDatos { get; set; } = "Pendiente de revisión";

    /// <summary>
    /// Fecha real de revisión del enlace; cambiarla actualiza el dato temporal y debe corresponder a una comprobación hecha.
    /// </summary>
    public DateOnly? FechaUltimaRevision { get; set; }

    /// <summary>
    /// Fuente que respalda la relación; cambiarla cambia la referencia documental usada para validar el enlace.
    /// </summary>
    public string? Fuente { get; set; }

    /// <summary>
    /// Observaciones del enlace; cambiarlo modifica el contexto asociado a la conexión física.
    /// </summary>
    public string? Observaciones { get; set; }
}
