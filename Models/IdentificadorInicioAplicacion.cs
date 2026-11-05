namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Conserva una marca aleatoria que cambia cada vez que se inicia el servidor web.
/// </summary>
public sealed class IdentificadorInicioAplicacion
{
    /// <summary>
    /// Nombre del reclamo que identifica el arranque al que pertenece una cookie; cambiarlo requiere modificar también su lectura y escritura.
    /// </summary>
    public const string TipoReclamo = "ProyectoConexionRed.InicioAplicacion";

    /// <summary>
    /// Marca efímera del proceso actual; cambiar su generación modifica cuándo se invalidan cookies anteriores.
    /// </summary>
    public string Valor { get; } = Guid.NewGuid().ToString("N");
}
