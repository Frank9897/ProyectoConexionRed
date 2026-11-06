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
/// Contiene los datos permitidos para registrar una interfaz.
/// </summary>
public class NuevaInterfazViewModel
{
    /// <summary>
    /// Equipo propietario; cambiarlo asigna el puerto a otra ficha inventariada.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un equipo.")]
    public int IdentificadorEquipo { get; set; }

    /// <summary>
    /// Etiqueta del puerto; cambiarla modifica el nombre con que se identifica la interfaz.
    /// </summary>
    [Required(ErrorMessage = "La etiqueta del puerto es obligatoria.")]
    [StringLength(100)]
    public string EtiquetaPuerto { get; set; } = string.Empty;

    /// <summary>
    /// Tipo físico; cambiarlo altera la clasificación del puerto, no su velocidad real.
    /// </summary>
    [Required]
    [StringLength(100)]
    [RegularExpression(@"^(No confirmado|RJ45|SFP|SFP\+|QSFP|Otro)$", ErrorMessage = "Selecciona un tipo de interfaz válido.")]
    public string TipoInterfaz { get; set; } = "No confirmado";

    /// <summary>
    /// Vigencia documental; cambiarla modifica si el puerto aparece confirmado o pendiente.
    /// </summary>
    [Required]
    [StringLength(60)]
    [RegularExpression(@"^(Pendiente de revisión|Confirmado|A verificar|Desactualizado)$", ErrorMessage = "Selecciona un estado válido.")]
    public string EstadoDatos { get; set; } = "Pendiente de revisión";

    /// <summary>
    /// Fecha de revisión; cambiarla debe reflejar una comprobación realmente realizada.
    /// </summary>
    public DateOnly? FechaUltimaRevision { get; set; }

    /// <summary>
    /// Notas de revisión; cambiarlas modifica el contexto guardado del puerto.
    /// </summary>
    [StringLength(1000)]
    public string? Observaciones { get; set; }

    /// <summary>
    /// Equipos existentes para el selector; cambiar las opciones modifica a qué equipo puede asignarse el puerto.
    /// </summary>
    public List<SelectListItem> EquiposDisponibles { get; set; } = [];
}

/// <summary>
/// Contiene los datos permitidos para registrar un enlace entre dos puertos.
/// </summary>
public class NuevoEnlaceViewModel
{
    /// <summary>
    /// Interfaz inicial; cambiarla selecciona otro puerto de origen.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el puerto de origen.")]
    public int IdentificadorPuertoOrigen { get; set; }

    /// <summary>
    /// Interfaz final; cambiarla selecciona otro puerto de destino.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el puerto de destino.")]
    public int IdentificadorPuertoDestino { get; set; }

    /// <summary>
    /// Medio documentado; cambiarlo clasifica la conexión de otra manera.
    /// </summary>
    [Required]
    [StringLength(60)]
    [RegularExpression(@"^(No confirmado|Cobre RJ45|Fibra|DAC|Otro)$", ErrorMessage = "Selecciona un medio válido.")]
    public string Medio { get; set; } = "No confirmado";

    /// <summary>
    /// Módulo del extremo de origen; cambiarlo actualiza el modelo únicamente si se verificó.
    /// </summary>
    [StringLength(120)]
    public string? ModuloOrigen { get; set; }

    /// <summary>
    /// Módulo del extremo de destino; cambiarlo actualiza el modelo únicamente si se verificó.
    /// </summary>
    [StringLength(120)]
    public string? ModuloDestino { get; set; }

    /// <summary>
    /// Estado de revisión; cambiarlo altera si la relación se presenta confirmada o pendiente.
    /// </summary>
    [Required]
    [StringLength(60)]
    [RegularExpression(@"^(Pendiente de revisión|Confirmado|A verificar|Desactualizado)$", ErrorMessage = "Selecciona un estado válido.")]
    public string EstadoDatos { get; set; } = "Pendiente de revisión";

    /// <summary>
    /// Fecha real de revisión; cambiarla debe corresponder a una verificación hecha.
    /// </summary>
    public DateOnly? FechaUltimaRevision { get; set; }

    /// <summary>
    /// Fuente documental; cambiarla modifica el respaldo de la conexión guardada.
    /// </summary>
    [StringLength(300)]
    public string? Fuente { get; set; }

    /// <summary>
    /// Observaciones; cambiarlas modifica el contexto del enlace.
    /// </summary>
    [StringLength(1000)]
    public string? Observaciones { get; set; }

    /// <summary>
    /// Interfaces existentes para seleccionar ambos extremos; cambiar la lista altera las opciones del formulario.
    /// </summary>
    public List<SelectListItem> InterfacesDisponibles { get; set; } = [];
}
