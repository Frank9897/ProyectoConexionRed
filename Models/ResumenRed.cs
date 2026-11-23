namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Indicadores del estado de la red documentada que se muestran en la página de Topología.
/// Todos se calculan desde el inventario; no se consulta la red real.
/// </summary>
public sealed class ResumenRed
{
    /// <summary>Estado de las direcciones IP de cada red habitual (192.168.0.x y 192.168.1.x).</summary>
    public List<ResumenIpRed> Redes { get; set; } = [];

    /// <summary>Direcciones en uso fuera de las redes habituales (por ejemplo 10.0.1.1).</summary>
    public int IpFueraDeRango { get; set; }

    /// <summary>Descripción de las IP que usan dos o más equipos, ya redactada para mostrarla.</summary>
    public List<string> IpEnConflicto { get; set; } = [];

    /// <summary>Total de switches; los tres siguientes lo reparten según su administrabilidad.</summary>
    public int TotalSwitches { get; set; }
    public int SwitchesAdministrables { get; set; }
    public int SwitchesNoAdministrables { get; set; }
    public int SwitchesSinConfirmar { get; set; }

    /// <summary>Enlaces físicos completos y su estado de confirmación.</summary>
    public int EnlacesCompletos { get; set; }
    public int EnlacesConfirmados { get; set; }
    public int EnlacesPorRevisar { get; set; }

    /// <summary>Relaciones documentadas de un solo extremo y cuántas no se pudieron asociar a una ficha.</summary>
    public int RelacionesParciales { get; set; }
    public int RelacionesSinEquipo { get; set; }

    /// <summary>Enlaces completos con y sin estado STP o RSTP documentado.</summary>
    public int EnlacesStpDocumentado { get; set; }
    public int EnlacesStpSinDocumentar { get; set; }

    /// <summary>Switches según cuánto de su STP está documentado en sus enlaces completos.</summary>
    public int SwitchesStpDocumentado { get; set; }
    public int SwitchesStpParcial { get; set; }
    public int SwitchesStpSinDocumentar { get; set; }
    public int SwitchesSinEnlacesCompletos { get; set; }

    /// <summary>Enlaces que cierran un ciclo: los completos sin STP documentado, los completos ya documentados y los que solo existen como relación parcial.</summary>
    public int CiclosSinDocumentar { get; set; }
    public int CiclosDocumentados { get; set; }
    public int CiclosEnRelacionesParciales { get; set; }

    /// <summary>Switches con documentación STP pendiente, los que más enlaces sin documentar tienen primero.</summary>
    public List<SwitchStpResumen> SwitchesPendientesStp { get; set; } = [];

    /// <summary>Puertos registrados y cuántos están enlazados, tienen relación parcial o no tienen conexión documentada.</summary>
    public int PuertosTotal { get; set; }
    public int PuertosConEnlace { get; set; }
    public int PuertosConRelacionParcial { get; set; }
    public int PuertosSinDocumentar { get; set; }

    /// <summary>Datos de identificación faltantes en el inventario.</summary>
    public int EquiposSinIp { get; set; }
    public int EquiposSinMac { get; set; }
}

/// <summary>Estado de las direcciones de una red /24.</summary>
public sealed class ResumenIpRed
{
    /// <summary>Texto de la red, por ejemplo 192.168.0.x.</summary>
    public string Nombre { get; set; } = string.Empty;
    public int Total { get; set; }
    public int EnUso { get; set; }
    public int Reservadas { get; set; }
    public int EnConflicto { get; set; }
    public int Libres { get; set; }
}

/// <summary>Documentación STP de un switch concreto.</summary>
public sealed class SwitchStpResumen
{
    public string Nombre { get; set; } = string.Empty;
    /// <summary>Enlaces físicos completos del switch.</summary>
    public int Enlaces { get; set; }
    /// <summary>Enlaces del switch sin estado STP documentado.</summary>
    public int SinDocumentar { get; set; }
}
