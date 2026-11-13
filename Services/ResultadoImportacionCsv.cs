using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Services;

/// <summary>
/// Resultado del análisis documental de un CSV sin guardar equipos en la base.
/// </summary>
public sealed class ResultadoImportacionCsv
{
    /// <summary>Equipos válidos leídos; cambiar la lista altera los registros que se podrían importar.</summary>
    public List<EquipoRed> Equipos { get; init; } = [];

    /// <summary>Errores que bloquean la carga; cambiar la lista modifica si la confirmación se habilita.</summary>
    public List<string> Errores { get; init; } = [];

    /// <summary>Avisos para revisión humana; cambiar la lista altera las advertencias de la vista previa.</summary>
    public List<string> Avisos { get; init; } = [];
}
