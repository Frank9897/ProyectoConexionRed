using System.Globalization;

namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Reúne las listas cerradas que usarán los formularios guiados y el mapa para puertos y enlaces.
/// </summary>
public static class OpcionesTopologia
{
    /// <summary>Medios físicos admitidos para un puerto.</summary>
    public static readonly string[] MediosFisicos = ["UTP", "Fibra", "DAC"];

    /// <summary>Modos de puerto admitidos; hoy solo se guarda, no se muestra.</summary>
    public static readonly string[] ModosPuerto = ["Acceso", "Troncal"];

    /// <summary>Estados STP o RSTP con los que se documenta la topología activa.</summary>
    public static readonly string[] EstadosStp = ["No documentado", "Reenvío", "Bloqueado", "Redundante"];

    /// <summary>Velocidades nominales ofrecidas en Mbps; 1000 es la habitual en switches administrables actuales.</summary>
    public static readonly int[] VelocidadesMbps = [10, 100, 1000, 2500, 5000, 10000, 25000, 40000, 100000];

    /// <summary>
    /// Convierte una velocidad en Mbps a un texto legible.
    /// </summary>
    /// <param name="mbps">Velocidad; cambiarla cambia la unidad mostrada (Mbps o Gbps).</param>
    /// <returns>Texto como «100 Mbps» o «2.5 Gbps».</returns>
    public static string EtiquetaVelocidad(int mbps) =>
        mbps >= 1000
            ? (mbps / 1000.0).ToString("0.#", CultureInfo.InvariantCulture) + " Gbps"
            : mbps.ToString(CultureInfo.InvariantCulture) + " Mbps";
}
