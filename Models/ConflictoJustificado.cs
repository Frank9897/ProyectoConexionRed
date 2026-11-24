namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Conflicto de datos (IP, MAC o nombre repetidos) que el técnico revisó y decidió justificar en lugar de corregir.
/// Mientras exista esta fila, el conflicto no se cuenta como pendiente.
/// </summary>
public class ConflictoJustificado
{
    /// <summary>Identificador interno de la justificación.</summary>
    public int Identificador { get; set; }

    /// <summary>Tipo de conflicto: IP, MAC o Nombre.</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Valor repetido normalizado: la IP, la MAC en mayúsculas con dos puntos o el nombre en minúsculas.</summary>
    public string Clave { get; set; } = string.Empty;

    /// <summary>Motivo elegido de una lista cerrada, por ejemplo «IP compartida a propósito».</summary>
    public string Motivo { get; set; } = string.Empty;

    /// <summary>Explicación libre; es obligatoria cuando el motivo es «Otro».</summary>
    public string? Detalle { get; set; }

    /// <summary>Usuario que justificó el conflicto.</summary>
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>Momento UTC en que se justificó.</summary>
    public DateTime FechaUtc { get; set; }
}
