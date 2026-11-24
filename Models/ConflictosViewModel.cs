namespace ProyectoConexionRed.Web.Models;

/// <summary>Conflictos de datos detectados en el inventario, separados entre pendientes y justificados.</summary>
public sealed class ConflictosViewModel
{
    /// <summary>Tipos de conflicto que se detectan.</summary>
    public const string TipoIp = "IP";
    public const string TipoMac = "MAC";
    public const string TipoNombre = "Nombre";

    /// <summary>Motivos que se ofrecen al justificar, por tipo; «Otro» exige escribir una explicación.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> MotivosPorTipo = new Dictionary<string, string[]>
    {
        [TipoIp] = ["IP compartida a propósito (alias, IP virtual o clúster)", "El mismo equipo está cargado en dos fichas (se unificará)", "Dato desactualizado (se corregirá más adelante)", "Otro"],
        [TipoMac] = ["El mismo equipo tiene dos interfaces o dos IP", "El mismo equipo está cargado en dos fichas (se unificará)", "Dato desactualizado (se corregirá más adelante)", "Otro"],
        [TipoNombre] = ["Son equipos distintos y se renombrarán", "El mismo equipo está cargado en dos fichas (se unificará)", "Otro"]
    };

    /// <summary>Conflictos sin resolver ni justificar.</summary>
    public List<ConflictoDetectado> Pendientes { get; set; } = [];

    /// <summary>Conflictos que el técnico justificó.</summary>
    public List<ConflictoDetectado> Justificados { get; set; } = [];

    /// <summary>IP libres de 192.168.0.x y 192.168.1.x que se ofrecen al cambiar una dirección.</summary>
    public List<string> IpsLibres { get; set; } = [];
}

/// <summary>Un valor repetido y los equipos que lo comparten.</summary>
public sealed class ConflictoDetectado
{
    public string Tipo { get; set; } = string.Empty;
    /// <summary>Valor repetido normalizado; se usa para identificar la justificación.</summary>
    public string Clave { get; set; } = string.Empty;
    public List<EquipoEnConflicto> Equipos { get; set; } = [];
    /// <summary>Justificación guardada, si existe.</summary>
    public ConflictoJustificado? Justificacion { get; set; }
}

/// <summary>Datos de un equipo que participa en un conflicto, para decidir cuál corregir.</summary>
public sealed class EquipoEnConflicto
{
    public int Identificador { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string? Ip { get; set; }
    public string? Mac { get; set; }
}
