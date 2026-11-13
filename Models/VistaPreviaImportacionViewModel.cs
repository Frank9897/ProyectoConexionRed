namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Presenta la carga preliminar de equipos CSV y requiere una confirmación explícita.
/// </summary>
public sealed class VistaPreviaImportacionViewModel
{
    /// <summary>Nombre de la sede nueva; cambiarlo define el nombre visible de la base creada.</summary>
    public string NombreInventario { get; set; } = string.Empty;

    /// <summary>CSV codificado para volver a analizar en el POST final; cambiarlo cambia todos los datos importados.</summary>
    public string ContenidoCsvCodificado { get; set; } = string.Empty;

    /// <summary>Indica que se completó una vista previa; cambiarlo muestra u oculta la tabla de revisión.</summary>
    public bool TieneVistaPrevia { get; set; }

    /// <summary>Errores del análisis; cambiar la lista bloquea o habilita la confirmación.</summary>
    public List<string> Errores { get; set; } = [];

    /// <summary>Requiere repetir la confirmación de avisos; cambiarlo muestra el motivo para volver a enviar el formulario.</summary>
    public bool ConfirmacionAvisosPendiente { get; set; }

    /// <summary>Avisos de revisión; cambiar la lista altera las advertencias visibles antes de crear la base.</summary>
    public List<string> Avisos { get; set; } = [];

    /// <summary>Filas candidatas; cambiar la lista modifica la muestra y la cantidad de equipos a crear.</summary>
    public List<FilaPreviaImportacionViewModel> Filas { get; set; } = [];

    /// <summary>Total de fichas que se crearían; cambiarlo actualiza el resumen de la carga.</summary>
    public int CantidadEquipos { get; set; }

    /// <summary>Filas válidas omitidas de la tabla resumida; cambiarlo altera la nota de vista previa.</summary>
    public int FilasNoMostradas { get; set; }

    /// <summary>Total de puertos provisionales que se generarían desde capacidades; cambiarlo altera el resumen.</summary>
    public int CantidadPuertosProvisionales { get; set; }
}

/// <summary>
/// Resume una fila candidata, sin mostrar ni ejecutar acciones sobre el equipo real.
/// </summary>
public sealed class FilaPreviaImportacionViewModel
{
    /// <summary>Nombre del equipo; cambiarlo modifica la ficha que se crearía.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Tipo del equipo; cambiarlo modifica su clasificación documental.</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>IP documentada; cambiarla modifica el valor que se importaría.</summary>
    public string? Ip { get; set; }

    /// <summary>Área; cambiarla modifica la agrupación visible de la ficha.</summary>
    public string? Area { get; set; }

    /// <summary>Fabricante; cambiarlo modifica la marca de la ficha que se crearía.</summary>
    public string? Fabricante { get; set; }

    /// <summary>Modelo; cambiarlo modifica la identificación técnica importada.</summary>
    public string? Modelo { get; set; }

    /// <summary>MAC; cambiarla modifica el identificador documentado importado.</summary>
    public string? Mac { get; set; }

    /// <summary>Rol; cambiarlo modifica la función documental del equipo.</summary>
    public string Rol { get; set; } = string.Empty;

    /// <summary>Resumen de capacidades; cambiarlo modifica la descripción de puertos derivables.</summary>
    public string Capacidad { get; set; } = string.Empty;

    /// <summary>Estado documental; cambiarlo modifica la etiqueta de vigencia de la ficha.</summary>
    public string EstadoDatos { get; set; } = string.Empty;
}
