namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Registra el estado de una dirección IPv4 de la red: libre, reservada, asignada o en conflicto.
/// Sirve para ofrecer solo IP libres en los formularios y para detectar duplicados.
/// </summary>
public class DireccionIp
{
    /// <summary>Identificador interno de la fila.</summary>
    public int Identificador { get; set; }

    /// <summary>Dirección IPv4 con cuatro octetos, por ejemplo 192.168.0.25; es única en la tabla.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Estado: Libre, Reservada, Asignada o En conflicto. Reservada se cambia a mano; los demás los calcula la sincronización.</summary>
    public string Estado { get; set; } = "Libre";

    /// <summary>Equipo que usa la IP cuando el estado es Asignada; nulo en los demás casos.</summary>
    public int? IdentificadorEquipo { get; set; }

    /// <summary>Motivo de la reserva o equipos que comparten la IP; texto de apoyo para el técnico.</summary>
    public string? Observaciones { get; set; }

    /// <summary>Momento UTC de la última modificación de la fila.</summary>
    public DateTime FechaActualizacionUtc { get; set; }
}
