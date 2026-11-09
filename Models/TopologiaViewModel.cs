namespace ProyectoConexionRed.Web.Models;

/// <summary>
/// Reúne indicadores, dispositivos, puertos y enlaces para dibujar el mapa documental.
/// </summary>
public class TopologiaViewModel
{
    /// <summary>Total de fichas de equipo; cambiarlo actualiza el indicador global.</summary>
    public int TotalEquipos { get; set; }
    /// <summary>Total de switches clasificados; cambiarlo actualiza el resumen de switching.</summary>
    public int TotalSwitches { get; set; }
    /// <summary>Total de puestos; cambiarlo actualiza el resumen de acceso.</summary>
    public int TotalPuestos { get; set; }
    /// <summary>Puestos con IP documentada; cambiarlo actualiza el resumen de direccionamiento.</summary>
    public int PuestosConIp { get; set; }
    /// <summary>Switches marcados como inaccesibles; cambiarlo actualiza el indicador administrativo.</summary>
    public int SwitchesInaccesibles { get; set; }
    /// <summary>Interfaces registradas; cambiarlo actualiza el resumen de puertos.</summary>
    public int TotalInterfaces { get; set; }
    /// <summary>Enlaces guardados; cambiarlo actualiza el resumen de conexiones.</summary>
    public int TotalEnlaces { get; set; }
    /// <summary>Áreas documentadas; cambiar la lista modifica las opciones del filtro.</summary>
    public IReadOnlyList<string> Areas { get; set; } = [];
    /// <summary>Equipos que se dibujan como nodos independientes.</summary>
    public IReadOnlyList<DispositivoTopologiaViewModel> Dispositivos { get; set; } = [];
    /// <summary>Enlaces cuyos dos extremos existen en la documentación.</summary>
    public IReadOnlyList<EnlaceTopologiaViewModel> Enlaces { get; set; } = [];
}

/// <summary>Representa un nodo de equipo y los puertos que pueden verse en su figura.</summary>
public class DispositivoTopologiaViewModel
{
    /// <summary>ID de la ficha; cambiarlo enlaza el nodo a otra ficha.</summary>
    public int Identificador { get; set; }
    /// <summary>Nombre visible; cambiarlo modifica la etiqueta del nodo.</summary>
    public string Nombre { get; set; } = string.Empty;
    /// <summary>Tipo del equipo; cambiarlo selecciona otro icono local.</summary>
    public string TipoDispositivo { get; set; } = string.Empty;
    /// <summary>Modelo documentado; cambiarlo selecciona otro perfil si existe.</summary>
    public string? Modelo { get; set; }
    /// <summary>Área de ubicación; cambiarla modifica la agrupación y el filtro.</summary>
    public string? Area { get; set; }
    /// <summary>Función jerárquica; cambiarla modifica la etiqueta de rol del equipo.</summary>
    public string RolEnArea { get; set; } = "No indicado";
    /// <summary>IP documentada; cambiarla modifica el detalle del nodo.</summary>
    public string? IpAdministracion { get; set; }
    /// <summary>Ruta de un recurso SVG local; cambiarla selecciona otro icono del catálogo.</summary>
    public string Icono { get; set; } = "/images/dispositivos/equipo-generico.svg";
    /// <summary>Origen de la ficha visual del modelo, si el perfil es exacto y tiene fuente.</summary>
    public string? FuentePerfil { get; set; }
    /// <summary>Estado del perfil; cambiarlo modifica el aviso sobre el origen de la capacidad.</summary>
    public string EstadoCapacidad { get; set; } = "Capacidad sin documentar";
    /// <summary>Marca si el nodo puede mostrar una capacidad de switch.</summary>
    public bool EsSwitch { get; set; }
    /// <summary>Puertos del frontal; cambiar la lista cambia las posiciones y estados mostrados.</summary>
    public IReadOnlyList<PuertoTopologiaViewModel> Puertos { get; set; } = [];
}

/// <summary>Representa un puerto real o una posición física todavía sin interfaz inventariada.</summary>
public class PuertoTopologiaViewModel
{
    /// <summary>ID real de interfaz; nulo indica una posición sintética del perfil físico.</summary>
    public int? IdentificadorInterfaz { get; set; }
    /// <summary>Etiqueta del puerto; cambiarla altera la identificación visible y su mapeo.</summary>
    public string Etiqueta { get; set; } = string.Empty;
    /// <summary>Tipo físico del puerto; cambiarlo modifica la agrupación del frontal.</summary>
    public string Tipo { get; set; } = string.Empty;
    /// <summary>Clase visual derivada del enlace o del uso explícito.</summary>
    public string ClaseVisual { get; set; } = "puerto-desconocido";
    /// <summary>Texto de estado; cambiarlo modifica el significado accesible del color.</summary>
    public string EstadoVisual { get; set; } = "Desconocido";
    /// <summary>Destino documentado; cambiarlo modifica la referencia emergente del puerto.</summary>
    public string? DestinoConexion { get; set; }
}

/// <summary>Presenta los extremos exactos y el estado de un enlace físico.</summary>
public class EnlaceTopologiaViewModel
{
    /// <summary>ID del equipo de origen; cambiarlo conecta el dibujo a otro nodo.</summary>
    public int IdentificadorEquipoOrigen { get; set; }
    /// <summary>ID del puerto de origen; cambiarlo señala otra interfaz documentada.</summary>
    public int IdentificadorPuertoOrigen { get; set; }
    /// <summary>Nombre del equipo origen; cambiarlo altera su etiqueta en la lista de relaciones.</summary>
    public string EquipoOrigen { get; set; } = string.Empty;
    /// <summary>Etiqueta exacta del puerto origen; cambiarla señala otra boca física.</summary>
    public string PuertoOrigen { get; set; } = string.Empty;
    /// <summary>ID del equipo de destino; cambiarlo conecta el dibujo a otro nodo.</summary>
    public int IdentificadorEquipoDestino { get; set; }
    /// <summary>ID del puerto destino; cambiarlo señala otra interfaz documentada.</summary>
    public int IdentificadorPuertoDestino { get; set; }
    /// <summary>Nombre del equipo destino; cambiarlo altera su etiqueta en la lista de relaciones.</summary>
    public string EquipoDestino { get; set; } = string.Empty;
    /// <summary>Etiqueta exacta del puerto destino; cambiarla señala otra boca física.</summary>
    public string PuertoDestino { get; set; } = string.Empty;
    /// <summary>Medio registrado; cambiarlo modifica la descripción del enlace.</summary>
    public string Medio { get; set; } = string.Empty;
    /// <summary>Estado documental; cambiarlo modifica la confianza visual de la arista.</summary>
    public string EstadoDatos { get; set; } = string.Empty;
    /// <summary>Verdadero si el enlace está confirmado; cambiarlo altera el trazo sólido o discontinuo.</summary>
    public bool Confirmado { get; set; }
    /// <summary>Fuente de respaldo; cambiarla modifica la referencia mostrada.</summary>
    public string? Fuente { get; set; }
    /// <summary>Fecha real de revisión; cambiarla modifica la fecha mostrada.</summary>
    public DateOnly? FechaUltimaRevision { get; set; }
}

/// <summary>Reúne un switch, su frontal y las conexiones guardadas para la página de detalle.</summary>
public class DetalleTopologiaViewModel
{
    /// <summary>Switch seleccionado; cambiarlo abre el detalle de otra ficha.</summary>
    public DispositivoTopologiaViewModel Dispositivo { get; set; } = new();
    /// <summary>Enlaces documentados del switch; cambiar la lista altera la tabla de conexiones.</summary>
    public IReadOnlyList<EnlaceTopologiaViewModel> Enlaces { get; set; } = [];
}
