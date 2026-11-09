namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Registra una conexión observada desde un puerto local, aunque falte el puerto remoto.
/// </summary>
public class ConexionDocumentada
{
    /// <summary>ID del registro; cambiarlo modifica su referencia documental.</summary>
    public int Identificador { get; set; }
    /// <summary>Interfaz local conocida; cambiarla asigna el registro a otro puerto documentado.</summary>
    public int IdentificadorPuertoLocal { get; set; }
    /// <summary>Nombre del equipo remoto; cambiarlo modifica el destino descrito.</summary>
    public string? EquipoRemoto { get; set; }
    /// <summary>IP remota conocida; cambiarla actualiza la dirección asociada al extremo.</summary>
    public string? IpRemota { get; set; }
    /// <summary>Puerto remoto conocido; cambiarlo actualiza la etiqueta del extremo remoto.</summary>
    public string? PuertoRemoto { get; set; }
    /// <summary>Medio documentado; cambiarlo modifica la clasificación física sin asumir tecnología.</summary>
    public string Medio { get; set; } = "No confirmado";
    /// <summary>Estado de verificación; cambiarlo altera si el registro se considera pendiente.</summary>
    public string EstadoDatos { get; set; } = "A verificar";
    /// <summary>Fecha de revisión real; cambiarla debe corresponder a una comprobación efectuada.</summary>
    public DateOnly? FechaUltimaRevision { get; set; }
    /// <summary>Página de origen; cambiarla modifica la ubicación del dato en el Visio.</summary>
    public string FuentePagina { get; set; } = string.Empty;
    /// <summary>Libro incrustado; cambiarlo señala otra tabla Excel de origen.</summary>
    public string? FuenteLibro { get; set; }
    /// <summary>Fila de origen; cambiarla apunta a otra fila de la tabla documentada.</summary>
    public string? FuenteFila { get; set; }
    /// <summary>Notas del registro; cambiarlas modifica el contexto de revisión.</summary>
    public string? Observaciones { get; set; }
}
