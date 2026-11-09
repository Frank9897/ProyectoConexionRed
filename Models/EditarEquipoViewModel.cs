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
    /// Función del equipo en la red; cambiarla modifica su clasificación operativa, independiente del fabricante y modelo.
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
    /// Puertos RJ45 del switch; nulo mantiene la capacidad sin confirmar y un valor la define manualmente.
    /// </summary>
    [Range(0, 512)]
    public int? CantidadPuertosRj45 { get; set; }

    /// <summary>
    /// Puertos SFP del switch; nulo mantiene la capacidad sin confirmar.
    /// </summary>
    [Range(0, 512)]
    public int? CantidadPuertosSfp { get; set; }

    /// <summary>
    /// Puertos SFP+ del switch; nulo mantiene la capacidad sin confirmar.
    /// </summary>
    [Range(0, 512)]
    public int? CantidadPuertosSfpPlus { get; set; }

    /// <summary>
    /// Puertos QSFP del switch; nulo mantiene la capacidad sin confirmar.
    /// </summary>
    [Range(0, 512)]
    public int? CantidadPuertosQsfp { get; set; }

    /// <summary>
    /// Puertos QSFP+ del switch; nulo mantiene la capacidad sin confirmar.
    /// </summary>
    [Range(0, 512)]
    public int? CantidadPuertosQsfpPlus { get; set; }

    /// <summary>
    /// Puertos QSFP28 del switch; nulo mantiene la capacidad sin confirmar.
    /// </summary>
    [Range(0, 512)]
    public int? CantidadPuertosQsfp28 { get; set; }

    /// <summary>
    /// Estado de vigencia documental; cambiarlo ajusta el aviso que presenta la aplicación.
    /// </summary>
    [Required]
    [StringLength(60)]
    [RegularExpression("^(Vigente|Pendiente de validación|A verificar|Desactualizado|Confirmado)$", ErrorMessage = "Selecciona una vigencia válida.")]
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
