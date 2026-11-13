namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Registra una tarea idempotente ya realizada sobre una base de inventario.
/// </summary>
public class MantenimientoEjecutado
{
    /// <summary>
    /// Código estable de la tarea; cambiarlo permite que una nueva versión se ejecute por separado.
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Fecha UTC en formato de texto; cambiarla altera la marca temporal guardada para la tarea.
    /// </summary>
    public string FechaUtc { get; set; } = string.Empty;
}

