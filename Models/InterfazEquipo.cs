namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Representa una interfaz física documentada en un equipo de red.
/// </summary>
public class InterfazEquipo
{
    /// <summary>
    /// Identificador interno de la interfaz; cambiarlo altera los enlaces que la referencian.
    /// </summary>
    public int Identificador { get; set; }

    /// <summary>
    /// Equipo propietario; cambiarlo mueve la interfaz a otra ficha.
    /// </summary>
    public int IdentificadorEquipo { get; set; }

    /// <summary>
    /// Etiqueta exacta del puerto; cambiarla modifica cómo se identifica en la documentación.
    /// </summary>
    public string EtiquetaPuerto { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de interfaz documentado; cambiarlo modifica la clasificación física sin cambiar el equipo real.
    /// </summary>
    public string TipoInterfaz { get; set; } = "No confirmado";

    /// <summary>
    /// Estado de vigencia; cambiarlo modifica si el puerto se muestra como confirmado o pendiente de revisión.
    /// </summary>
    public string EstadoDatos { get; set; } = "Pendiente de revisión";

    /// <summary>
    /// Uso confirmado del puerto cuando no tiene enlace; cambiarlo cambia si puede mostrarse como libre.
    /// </summary>
    public string EstadoUso { get; set; } = "Desconocido";

    /// <summary>
    /// Fecha real de revisión; cambiarla actualiza el dato temporal y debe corresponder a una verificación hecha.
    /// </summary>
    public DateOnly? FechaUltimaRevision { get; set; }

    /// <summary>
    /// Notas de documentación del puerto; cambiarlo modifica el contexto asociado a la interfaz.
    /// </summary>
    public string? Observaciones { get; set; }

    /// <summary>
    /// Velocidad nominal del puerto en Mbps (10, 100, 1000, 2500, 10000, etc.); nulo si no está documentada.
    /// Cambiarla modifica los avisos de velocidades distintas entre los extremos de un enlace.
    /// </summary>
    public int? VelocidadMbps { get; set; }

    /// <summary>
    /// Medio físico previsto del puerto (UTP, Fibra o DAC); nulo si no está documentado.
    /// </summary>
    public string? MedioFisico { get; set; }

    /// <summary>
    /// Modo del puerto: Acceso o Troncal. Por defecto es Acceso; todavía no se muestra en pantallas.
    /// </summary>
    public string ModoPuerto { get; set; } = "Acceso";

    /// <summary>
    /// VLAN nativa o de acceso del puerto. Por defecto es la VLAN 1; solo se guarda y todavía no se muestra.
    /// </summary>
    public int Vlan { get; set; } = 1;
}
