using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Agrupa los listados documentales de interfaces y enlaces.
/// </summary>
public class ConexionesIndexViewModel
{
    /// <summary>
    /// Switches disponibles con resumen de interfaces y destinos.
    /// </summary>
    public IReadOnlyList<SwitchPuertosResumenViewModel> Switches { get; set; } = [];

    /// <summary>
    /// Puertos registrados; cambiar la lista modifica las filas de interfaces mostradas.
    /// </summary>
    public IReadOnlyList<InterfazListadoViewModel> Interfaces { get; set; } = [];

    /// <summary>
    /// Enlaces registrados; cambiar la lista modifica las relaciones presentadas.
    /// </summary>
    public IReadOnlyList<EnlaceListadoViewModel> Enlaces { get; set; } = [];

    /// <summary>Relaciones con extremo remoto incompleto; cambiar la lista altera las filas pendientes.</summary>
    public IReadOnlyList<RelacionParcialListadoViewModel> RelacionesParciales { get; set; } = [];
}

/// <summary>
/// Presenta un switch y el estado de los puertos registrados en él.
/// </summary>
public class SwitchPuertosResumenViewModel
{
    /// <summary>ID de equipo; cambiarlo abre el frontal de otro switch.</summary>
    public int Identificador { get; set; }
    /// <summary>Nombre documental; cambiarlo altera la etiqueta de la tarjeta.</summary>
    public string Nombre { get; set; } = string.Empty;
    /// <summary>Área registrada; cambiarla modifica el contexto de ubicación.</summary>
    public string? Area { get; set; }
    /// <summary>Modelo documentado; cambiarlo actualiza la descripción del switch.</summary>
    public string? Modelo { get; set; }
    /// <summary>IP registrada; cambiarla altera el dato de administración mostrado.</summary>
    public string? IpAdministracion { get; set; }
    /// <summary>Puertos inventariados; cambiarlo altera el total documentado en la tarjeta.</summary>
    public int TotalPuertos { get; set; }
    /// <summary>Puertos con vínculo completo o parcial; cambiarlo modifica el conteo de ocupados en registros.</summary>
    public int PuertosOcupados { get; set; }
    /// <summary>Puertos marcados libres con vigencia confirmada; cambiarlo modifica el conteo libre.</summary>
    public int PuertosLibres { get; set; }
    /// <summary>Puertos sin relación o estado libre confirmado; cambiarlo modifica el conteo desconocido.</summary>
    public int PuertosDesconocidos { get; set; }
    /// <summary>Destinos expresamente registrados; cambiar la lista altera los extremos resumidos en la tarjeta.</summary>
    public IReadOnlyList<string> DestinosDocumentados { get; set; } = [];
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
    /// Uso del puerto; cambiarlo actualiza la etiqueta de disponibilidad documental.
    /// </summary>
    public string EstadoUso { get; set; } = "Desconocido";

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
    [RegularExpression(@"^(No confirmado|RJ45|SFP|SFP\+|QSFP|QSFP\+|QSFP28|Otro)$", ErrorMessage = "Selecciona un tipo de interfaz válido.")]
    public string TipoInterfaz { get; set; } = "No confirmado";
    /// <summary>Uso confirmado del puerto; cambiarlo define si un extremo sin enlace está libre o desconocido.</summary>
    [Required, StringLength(40)]
    [RegularExpression(@"^(Desconocido|Libre confirmado)$", ErrorMessage = "Selecciona un uso válido.")]
    public string EstadoUso { get; set; } = "Desconocido";
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

/// <summary>Resume una relación con puerto local conocido y remoto opcional.</summary>
public class RelacionParcialListadoViewModel
{
    /// <summary>ID del registro; cambiarlo selecciona otra relación parcial.</summary>
    public int Identificador { get; set; }
    /// <summary>Equipo local; cambiarlo modifica el extremo inventariado.</summary>
    public string EquipoLocal { get; set; } = string.Empty;
    /// <summary>Puerto local; cambiarlo señala otra interfaz conocida.</summary>
    public string PuertoLocal { get; set; } = string.Empty;
    /// <summary>Equipo remoto; cambiarlo modifica la etiqueta del destino documentado.</summary>
    public string? EquipoRemoto { get; set; }
    /// <summary>IP remota; cambiarla actualiza la dirección de destino.</summary>
    public string? IpRemota { get; set; }
    /// <summary>Puerto remoto; cambiarlo actualiza la etiqueta remota conocida.</summary>
    public string? PuertoRemoto { get; set; }
    /// <summary>Medio registrado; cambiarlo modifica la clasificación documental.</summary>
    public string Medio { get; set; } = string.Empty;
    /// <summary>Estado registrado; cambiarlo modifica la señal de revisión.</summary>
    public string EstadoDatos { get; set; } = string.Empty;
    /// <summary>Página de origen; cambiarla cambia la ubicación documental.</summary>
    public string FuentePagina { get; set; } = string.Empty;
    /// <summary>Libro de origen; cambiarlo cambia la tabla Excel referenciada.</summary>
    public string? FuenteLibro { get; set; }
    /// <summary>Fila de origen; cambiarla apunta a otro renglón.</summary>
    public string? FuenteFila { get; set; }
    /// <summary>Notas; cambiarlas modifica el contexto visible del registro.</summary>
    public string? Observaciones { get; set; }
}

/// <summary>Datos y opciones para registrar una relación con extremo remoto incompleto.</summary>
public class NuevaConexionDocumentadaViewModel
{
    /// <summary>Puerto local real; cambiarlo vincula la observación a otra interfaz.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el puerto local conocido.")]
    public int IdentificadorPuertoLocal { get; set; }
    /// <summary>Equipo remoto opcional; cambiarlo actualiza el nombre del destino disponible.</summary>
    [StringLength(160)]
    public string? EquipoRemoto { get; set; }
    /// <summary>IP remota opcional; cambiarla actualiza la dirección si está documentada.</summary>
    [StringLength(45)]
    public string? IpRemota { get; set; }
    /// <summary>Puerto remoto opcional; cambiarlo actualiza la etiqueta si se conoce.</summary>
    [StringLength(100)]
    public string? PuertoRemoto { get; set; }
    /// <summary>Medio; cambiarlo modifica el tipo indicado y queda No confirmado sin evidencia.</summary>
    [Required, StringLength(60)]
    [RegularExpression(@"^(No confirmado|Cobre RJ45|Fibra|DAC|Otro)$", ErrorMessage = "Selecciona un medio válido.")]
    public string Medio { get; set; } = "No confirmado";
    /// <summary>Estado; cambiarlo altera la confianza documental de la relación.</summary>
    [Required, StringLength(60)]
    [RegularExpression(@"^(A verificar|Pendiente de revisión|Confirmado|Desactualizado)$", ErrorMessage = "Selecciona un estado válido.")]
    public string EstadoDatos { get; set; } = "A verificar";
    /// <summary>Fecha comprobada; cambiarla debe coincidir con una revisión real.</summary>
    public DateOnly? FechaUltimaRevision { get; set; }
    /// <summary>Página requerida de origen; cambiarla modifica la referencia al Visio.</summary>
    [Required, StringLength(160)]
    public string FuentePagina { get; set; } = string.Empty;
    /// <summary>Libro Excel opcional; cambiarlo actualiza el nombre de la tabla incrustada.</summary>
    [StringLength(160)]
    public string? FuenteLibro { get; set; }
    /// <summary>Fila opcional; cambiarla apunta al renglón origen.</summary>
    [StringLength(100)]
    public string? FuenteFila { get; set; }
    /// <summary>Observaciones; cambiarlas modifica el contexto de revisión.</summary>
    [StringLength(1000)]
    public string? Observaciones { get; set; }
    /// <summary>Interfaces utilizables; cambiar la lista modifica las opciones del selector local.</summary>
    public List<SelectListItem> InterfacesDisponibles { get; set; } = [];
}

/// <summary>
/// Reutiliza los campos de una relación parcial existente y guarda su identificador para editarla.
/// </summary>
public class EditarConexionDocumentadaViewModel : NuevaConexionDocumentadaViewModel
{
    /// <summary>ID de relación; cambiarlo selecciona otro registro documental.</summary>
    [Range(1, int.MaxValue)]
    public int Identificador { get; set; }

    /// <summary>Puerto local legible; cambiarlo altera la identificación contextual mostrada, no su propietario.</summary>
    public string NombrePuertoLocal { get; set; } = string.Empty;
}
