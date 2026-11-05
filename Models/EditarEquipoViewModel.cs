using System.ComponentModel.DataAnnotations;

namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Contiene los campos permitidos al editar una ficha de equipo.
/// </summary>
public class EditarEquipoViewModel
{
    /// <summary>
    /// Identificador del equipo; cambiarlo selecciona otro registro para la edición.
    /// </summary>
    public int Identificador { get; set; }

    /// <summary>
    /// Etiqueta del equipo; cambiarla modifica el nombre visible en el inventario.
    /// </summary>
    [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
    [StringLength(160, ErrorMessage = "El nombre no puede superar 160 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Fabricante documentado; cambiarlo actualiza la marca que aparece en la ficha.
    /// </summary>
    [StringLength(100)]
    public string? Fabricante { get; set; }

    /// <summary>
    /// Modelo documentado; cambiarlo actualiza el modelo visible sin configurar el equipo real.
    /// </summary>
    [StringLength(120)]
    public string? Modelo { get; set; }

    /// <summary>
    /// Dirección IP documentada; cambiarla modifica solo el inventario, sin configurar el dispositivo.
    /// </summary>
    [StringLength(45)]
    public string? IpAdministracion { get; set; }

    /// <summary>
    /// Dirección MAC documentada; cambiarla actualiza el dato identificador de la interfaz.
    /// </summary>
    [StringLength(32)]
    public string? DireccionMac { get; set; }

    /// <summary>
    /// Área del equipo; cambiarla lo agrupa en otra ubicación lógica.
    /// </summary>
    [StringLength(120)]
    public string? Area { get; set; }

    /// <summary>
    /// Piso o sector físico; cambiarlo actualiza la ubicación que muestra el inventario.
    /// </summary>
    [StringLength(80)]
    public string? Piso { get; set; }

    /// <summary>
    /// Usuario asignado según la fuente; cambiarlo modifica el vínculo registrado, no una cuenta de acceso.
    /// </summary>
    [StringLength(160)]
    public string? UsuarioAsignado { get; set; }

    /// <summary>
    /// Fuente documental; cambiarla modifica la referencia utilizada para revisar el dato.
    /// </summary>
    [StringLength(240)]
    public string? FuenteDatos { get; set; }

    /// <summary>
    /// Función jerárquica documentada; cambiarla modifica la clasificación principal o secundaria.
    /// </summary>
    [Required]
    [StringLength(60)]
    public string RolEnArea { get; set; } = "No indicado";

    /// <summary>
    /// Tipo de dispositivo; cambiarlo modifica la clasificación del inventario.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string TipoDispositivo { get; set; } = "No especificado";

    /// <summary>
    /// Estado de acceso administrativo; cambiarlo no mide conectividad ni disponibilidad física.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EstadoAdministrativo { get; set; } = "Sin información";

    /// <summary>
    /// Estado de vigencia documental; cambiarlo ajusta el aviso que presenta la aplicación.
    /// </summary>
    [Required]
    [StringLength(60)]
    public string EstadoDatos { get; set; } = "Vigente";

    /// <summary>
    /// Fecha real de revisión; cambiarla modifica la fecha registrada y debe corresponder a una revisión efectuada.
    /// </summary>
    public DateOnly? FechaUltimaRevision { get; set; }

    /// <summary>
    /// Observaciones de inventario; cambiarla modifica las notas asociadas a la ficha.
    /// </summary>
    [StringLength(1000)]
    public string? Observaciones { get; set; }
}
