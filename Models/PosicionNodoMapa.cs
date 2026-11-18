namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Guarda en la base la posición de un equipo en el mapa, para que todos los técnicos vean el mismo diseño.
/// Reemplazará al almacenamiento local del navegador cuando exista el mapa editable.
/// </summary>
public class PosicionNodoMapa
{
    /// <summary>Identificador interno de la posición.</summary>
    public int Identificador { get; set; }

    /// <summary>Equipo que se dibuja; borrar el equipo no borra automáticamente su posición.</summary>
    public int IdentificadorEquipo { get; set; }

    /// <summary>Vista del mapa a la que pertenece la posición (por ejemplo general); permite varios diseños.</summary>
    public string Vista { get; set; } = "general";

    /// <summary>Coordenada horizontal del nodo en el lienzo.</summary>
    public double X { get; set; }

    /// <summary>Coordenada vertical del nodo en el lienzo.</summary>
    public double Y { get; set; }

    /// <summary>Momento UTC de la última modificación, para auditar quién movió el nodo.</summary>
    public DateTime FechaActualizacionUtc { get; set; }

    /// <summary>Usuario que guardó la posición.</summary>
    public string NombreUsuario { get; set; } = string.Empty;
}
