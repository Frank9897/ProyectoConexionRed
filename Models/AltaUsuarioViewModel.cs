using System.ComponentModel.DataAnnotations;

namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Contiene los datos mínimos para crear una cuenta técnica local.
/// </summary>
public class AltaUsuarioViewModel
{
    /// <summary>
    /// Nombre de acceso; cambiarlo define el identificador con el que el técnico inicia sesión.
    /// </summary>
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 100 caracteres.")]
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña inicial; cambiarla modifica la clave que se almacenará únicamente como hash.
    /// </summary>
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(200, MinimumLength = 12, ErrorMessage = "La contraseña debe tener al menos 12 caracteres.")]
    [DataType(DataType.Password)]
    public string Contrasena { get; set; } = string.Empty;
}
