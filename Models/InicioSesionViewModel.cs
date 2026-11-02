using System.ComponentModel.DataAnnotations;

namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Contiene los campos transitorios del formulario de inicio de sesión.
/// </summary>
public class InicioSesionViewModel
{
    /// <summary>
    /// Nombre de cuenta ingresado; cambiarlo cambia qué usuario se intenta autenticar.
    /// </summary>
    [Required(ErrorMessage = "Ingresá tu usuario.")]
    [Display(Name = "Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña recibida solo durante el intento; cambiarla cambia la credencial comprobada y nunca se persiste.
    /// </summary>
    [Required(ErrorMessage = "Ingresá tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>
    /// Dirección local a la que se vuelve tras acceder; cambiarla modifica el destino posterior al ingreso.
    /// </summary>
    public string? UrlRetorno { get; set; }
}
