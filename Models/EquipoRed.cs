namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Representa un equipo documentado en el inventario de la red.
/// </summary>
public class EquipoRed
{
    /// <summary>
    /// Identificador interno del registro; cambiarlo altera la referencia del equipo en la base.
    /// </summary>
    public int Identificador { get; set; }

    /// <summary>
    /// Nombre o etiqueta del equipo; cambiarlo actualiza cómo se identifica en el inventario.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Fabricante indicado o confirmado; cambiarlo modifica el filtro y los datos visibles del fabricante.
    /// </summary>
    public string? Fabricante { get; set; }

    /// <summary>
    /// Modelo documentado; cambiarlo actualiza la identificación técnica sin alterar el nombre del equipo.
    /// </summary>
    public string? Modelo { get; set; }

    /// <summary>
    /// Dirección IP de administración; cambiarla modifica el dato de conexión documentado, no configura el equipo real.
    /// </summary>
    public string? IpAdministracion { get; set; }

    /// <summary>
    /// Dirección MAC documentada; cambiarla actualiza el identificador de interfaz almacenado.
    /// </summary>
    public string? DireccionMac { get; set; }

    /// <summary>
    /// Área o ubicación lógica; cambiarla traslada el equipo a otra agrupación del inventario.
    /// </summary>
    public string? Area { get; set; }

    /// <summary>
    /// Función jerárquica conocida; cambiarla puede presentar al equipo como principal o secundario en vistas futuras.
    /// </summary>
    public string RolEnArea { get; set; } = "No indicado";

    /// <summary>
    /// Tipo conocido; cambiarlo modifica la clasificación del equipo en búsquedas y vistas.
    /// </summary>
    public string TipoDispositivo { get; set; } = "No especificado";

    /// <summary>
    /// Estado de acceso administrativo; cambiarlo describe la documentación de acceso y no el estado físico de la red.
    /// </summary>
    public string EstadoAdministrativo { get; set; } = "Sin información";

    /// <summary>
    /// Estado de vigencia de los datos del equipo; cambiarlo modifica cómo se presentan los avisos de revisión.
    /// </summary>
    public string EstadoDatos { get; set; } = "Vigente";

    /// <summary>
    /// Fecha real de la última revisión; cambiarla actualiza la fecha mostrada y debe basarse en una revisión efectuada.
    /// </summary>
    public DateOnly? FechaUltimaRevision { get; set; }

    /// <summary>
    /// Notas de inventario; cambiar su contenido modifica el contexto documental asociado al equipo.
    /// </summary>
    public string? Observaciones { get; set; }
}
