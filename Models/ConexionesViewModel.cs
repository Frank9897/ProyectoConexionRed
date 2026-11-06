using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Agrupa los listados documentales de interfaces y enlaces.
/// </summary>
public class ConexionesIndexViewModel
{
    /// <summary>
    /// Puertos registrados; cambiar la lista modifica las filas de interfaces mostradas.
    /// </summary>
    public IReadOnlyList<InterfazListadoViewModel> Interfaces { get; set; } = [];

    /// <summary>
    /// Enlaces registrados; cambiar la lista modifica las relaciones presentadas.
    /// </summary>
    public IReadOnlyList<EnlaceListadoViewModel> Enlaces { get; set; } = [];
}

/// <summary>
/// Resume un puerto junto con el equipo que lo contiene.
/// </summary>
public class InterfazListadoViewModel
{
    /// <summary>
    /// ID interno del puerto; cambiarlo selecciona otro registro.
    /// </summary>
    public int Identificador { get; set; }

    /// <summary>
    /// Equipo propietario; cambiarlo modifica la etiqueta visible del puerto.
    /// </summary>
    public string Equipo { get; set; } = string.Empty;

    /// <summary>
    /// IP de administración del equipo; cambiarla modifica el dato que aparece junto al puerto.
    /// </summary>
    public string? IpAdministracion { get; set; }

    /// <summary>
    /// Etiqueta exacta del puerto; cambiarla modifica la identificación de la interfaz.
    /// </summary>
    public string Puerto { get; set; } = string.Empty;

    /// <summary>
    /// Tipo físico documentado; cambiarlo modifica la clasificación visible.
    /// </summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>
    /// Vigencia del dato; cambiarla altera la señal de revisión que muestra la página.
    /// </summary>
    public string EstadoDatos { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de revisión real; cambiarla modifica la fecha asociada al puerto.
    /// </summary>
    public DateOnly? FechaUltimaRevision { get; set; }

    /// <summary>
    /// Observaciones del puerto; cambiarlas modifica el contexto mostrado.
    /// </summary>
    public string? Observaciones { get; set; }
}

/// <summary>
/// Resume un enlace y los dos extremos que lo componen.
/// </summary>
public class EnlaceListadoViewModel
{
    /// <summary>
    /// ID interno del enlace; cambiarlo selecciona otra relación.
    /// </summary>
    public int Identificador { get; set; }

    /// <summary>
    /// Equipo de origen; cambiarlo modifica el primer extremo mostrado.
    /// </summary>
    public string EquipoOrigen { get; set; } = string.Empty;

    /// <summary>
    /// Puerto de origen; cambiarlo modifica el puerto inicial del enlace.
    /// </summary>
    public string PuertoOrigen { get; set; } = string.Empty;

    /// <summary>
    /// Equipo de destino; cambiarlo modifica el segundo extremo mostrado.
    /// </summary>
    public string EquipoDestino { get; set; } = string.Empty;

    /// <summary>
    /// Puerto de destino; cambiarlo modifica el puerto final del enlace.
    /// </summary>
    public string PuertoDestino { get; set; } = string.Empty;

    /// <summary>
    /// Medio documentado; cambiarlo altera la categoría del enlace.
    /// </summary>
    public string Medio { get; set; } = string.Empty;

    /// <summary>
    /// Estado documental; cambiarlo altera la indicación de verificación.
    /// </summary>
    public string EstadoDatos { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de revisión real; cambiarla modifica la fecha presentada.
    /// </summary>
    public DateOnly? FechaUltimaRevision { get; set; }

    /// <summary>
    /// Fuente de respaldo; cambiarla altera la referencia documental del enlace.
    /// </summary>
    public string? Fuente { get; set; }
}

/// <summary>
/// Campos y validaciones compartidos por los formularios de interfaz.
/// </summary>
public abstract class InterfazFormularioViewModel
{
    /// <summary>Equipo propietario; cambiarlo asigna el puerto a otra ficha.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un equipo.")]
    public int IdentificadorEquipo { get; set; }
    /// <summary>Etiqueta de puerto; cambiarla modifica su identificación documental.</summary>
    [Required(ErrorMessage = "La etiqueta del puerto es obligatoria.")]
    [StringLength(100)]
    public string EtiquetaPuerto { get; set; } = string.Empty;
    /// <summary>Tipo documentado; cambiarlo modifica la clasificación, no la velocidad real.</summary>
    [Required, StringLength(100)]
    [RegularExpression(@"^(No confirmado|RJ45|SFP|SFP\+|QSFP|Otro)$", ErrorMessage = "Selecciona un tipo de interfaz válido.")]
    public string TipoInterfaz { get; set; } = "No confirmado";
    /// <summary>Vigencia; cambiarla modifica el estado de revisión mostrado.</summary>
    [Required, StringLength(60)]
    [RegularExpression(@"^(Pendiente de revisión|Confirmado|A verificar|Desactualizado)$", ErrorMessage = "Selecciona un estado válido.")]
    public string EstadoDatos { get; set; } = "Pendiente de revisión";
    /// <summary>Fecha de revisión; cambiarla debe reflejar una comprobación real.</summary>
    public DateOnly? FechaUltimaRevision { get; set; }
    /// <summary>Notas; cambiarlas modifica el contexto guardado del puerto.</summary>
    [StringLength(1000)]
    public string? Observaciones { get; set; }
    /// <summary>Opciones de equipos; cambiar la lista altera el selector disponible.</summary>
    public List<SelectListItem> EquiposDisponibles { get; set; } = [];
}

/// <summary>Formulario para dar de alta una interfaz.</summary>
public class NuevaInterfazViewModel : InterfazFormularioViewModel { }

/// <summary>
/// Formulario de edición que reutiliza los campos y validaciones del alta.
/// </summary>
public class EditarInterfazViewModel : InterfazFormularioViewModel
{
    /// <summary>ID de la interfaz; cambiarlo selecciona otro puerto para editar.</summary>
    [Range(1, int.MaxValue)]
    public int Identificador { get; set; }
}

/// <summary>
/// Campos y validaciones compartidos por los formularios de enlace.
/// </summary>
public abstract class EnlaceFormularioViewModel
{
    /// <summary>Puerto de origen; cambiarlo selecciona otro extremo inicial.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el puerto de origen.")]
    public int IdentificadorPuertoOrigen { get; set; }
    /// <summary>Puerto de destino; cambiarlo selecciona otro extremo final.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el puerto de destino.")]
    public int IdentificadorPuertoDestino { get; set; }
    /// <summary>Medio documentado; cambiarlo clasifica de otra forma el enlace.</summary>
    [Required, StringLength(60)]
    [RegularExpression(@"^(No confirmado|Cobre RJ45|Fibra|DAC|Otro)$", ErrorMessage = "Selecciona un medio válido.")]
    public string Medio { get; set; } = "No confirmado";
    /// <summary>Módulo de origen; cambiarlo actualiza el modelo solo si fue verificado.</summary>
    [StringLength(120)]
    public string? ModuloOrigen { get; set; }
    /// <summary>Módulo de destino; cambiarlo actualiza el modelo solo si fue verificado.</summary>
    [StringLength(120)]
    public string? ModuloDestino { get; set; }
    /// <summary>Estado documental; cambiarlo altera la indicación de revisión.</summary>
    [Required, StringLength(60)]
    [RegularExpression(@"^(Pendiente de revisión|Confirmado|A verificar|Desactualizado)$", ErrorMessage = "Selecciona un estado válido.")]
    public string EstadoDatos { get; set; } = "Pendiente de revisión";
    /// <summary>Fecha real de revisión; cambiarla debe corresponder a una verificación.</summary>
    public DateOnly? FechaUltimaRevision { get; set; }
    /// <summary>Fuente documental; cambiarla modifica el respaldo registrado.</summary>
    [StringLength(300)]
    public string? Fuente { get; set; }
    /// <summary>Observaciones; cambiarlas modifica el contexto del enlace.</summary>
    [StringLength(1000)]
    public string? Observaciones { get; set; }
    /// <summary>Opciones de puertos; cambiar la lista altera los selectores de extremos.</summary>
    public List<SelectListItem> InterfacesDisponibles { get; set; } = [];
}

/// <summary>Formulario para dar de alta un enlace entre puertos.</summary>
public class NuevoEnlaceViewModel : EnlaceFormularioViewModel { }

/// <summary>
/// Formulario de edición que reutiliza los campos y validaciones del alta.
/// </summary>
public class EditarEnlaceViewModel : EnlaceFormularioViewModel
{
    /// <summary>ID del enlace; cambiarlo selecciona otra relación.</summary>
    [Range(1, int.MaxValue)]
    public int Identificador { get; set; }
}
