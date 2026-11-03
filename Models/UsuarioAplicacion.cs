namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Representa una cuenta local utilizada para iniciar sesión en la aplicación.
/// </summary>
public class UsuarioAplicacion
{
    /// <summary>
    /// Identificador de la cuenta; cambiarlo altera sus referencias en tablas relacionadas.
    /// </summary>
    public int Identificador { get; set; }

    /// <summary>
    /// Nombre visible de la cuenta; cambiarlo modifica cómo se identifica al iniciar sesión.
    /// </summary>
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>
    /// Nombre normalizado para buscar sin diferencias de mayúsculas; cambiarlo modifica la coincidencia de inicio de sesión.
    /// </summary>
    public string NombreUsuarioNormalizado { get; set; } = string.Empty;

    /// <summary>
    /// Hash irreversible de la contraseña; cambiarlo invalida la contraseña asociada a esta cuenta.
    /// </summary>
    public string HashContrasena { get; set; } = string.Empty;

    /// <summary>
    /// Rol de acceso; cambiarlo entre Administrador y Tecnico modifica qué secciones protegidas puede administrar la cuenta.
    /// </summary>
    public string Rol { get; set; } = "Tecnico";

    /// <summary>
    /// Indica si la cuenta puede iniciar sesión; cambiarlo habilita o bloquea su acceso a la aplicación.
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Fecha UTC de creación de la cuenta; cambiarla altera la fecha mostrada en la administración futura.
    /// </summary>
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
}
