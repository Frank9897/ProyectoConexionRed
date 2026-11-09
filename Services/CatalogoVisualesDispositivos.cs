using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Services;

/// <summary>
/// Mantiene perfiles visuales exactos y genera estados de puerto sin inferir conexiones.
/// </summary>
public static class CatalogoVisualesDispositivos
{
    // PerfilesPorModelo conserva solo variantes verificadas; agregar una entrada exige modelo exacto y fuente técnica.
    private static readonly Dictionary<string, PerfilSwitchVisual> PerfilesPorModelo = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dell|S4148U-ON"] = new PerfilSwitchVisual(
            "/images/dispositivos/modelos/dell-s4148u-on.svg", 0, 0, 48, 0, 2, 4,
            "https://www.dell.com/support/manuals/en-uk/networking-s4148f-on/s4100f_t_u_on_install_pub/features?guid=guid-0de00e36-0afd-4bc1-848f-10243d7075ec&lang=en-us")
    };

    /// <summary>
    /// Resuelve el perfil exacto de un modelo; cambiar los datos de entrada puede seleccionar un perfil distinto.
    /// </summary>
    /// <param name="fabricante">Fabricante documentado; cambiarlo puede hacer que no coincida el perfil.</param>
    /// <param name="modelo">Modelo documentado; cambiarlo busca otra variante exacta.</param>
    /// <returns>Perfil verificado o nulo si la combinación no está registrada.</returns>
    public static PerfilSwitchVisual? ObtenerPerfil(string? fabricante, string? modelo)
    {
        // clavePerfil requiere tanto fabricante como modelo para evitar emparejar variantes parciales.
        var clavePerfil = $"{fabricante?.Trim()}|{modelo?.Trim()}";
        return PerfilesPorModelo.TryGetValue(clavePerfil, out var perfil) ? perfil : null;
    }

    /// <summary>
    /// Selecciona un icono SVG local por perfil exacto o por tipo general.
    /// </summary>
    /// <param name="tipoDispositivo">Tipo del equipo; cambiarlo cambia la familia del icono.</param>
    /// <param name="perfil">Perfil exacto; cambiarlo puede seleccionar un recurso de modelo específico.</param>
    /// <returns>Ruta local de la ilustración SVG.</returns>
    public static string ObtenerIcono(string tipoDispositivo, PerfilSwitchVisual? perfil)
    {
        if (perfil is not null) return perfil.Icono;
        if (tipoDispositivo.Contains("switch", StringComparison.OrdinalIgnoreCase))
            return "/images/dispositivos/switch-generico.svg";
        if (tipoDispositivo.Contains("impresora", StringComparison.OrdinalIgnoreCase)
            || tipoDispositivo.Contains("printer", StringComparison.OrdinalIgnoreCase))
            return "/images/dispositivos/impresora.svg";
        if (tipoDispositivo.Contains("router", StringComparison.OrdinalIgnoreCase))
            return "/images/dispositivos/router.svg";
        if (tipoDispositivo.Contains("firewall", StringComparison.OrdinalIgnoreCase))
            return "/images/dispositivos/firewall.svg";
        if (tipoDispositivo.Contains("servidor", StringComparison.OrdinalIgnoreCase)
            || tipoDispositivo.Contains("server", StringComparison.OrdinalIgnoreCase))
            return "/images/dispositivos/servidor.svg";
        if (tipoDispositivo.Contains("ups", StringComparison.OrdinalIgnoreCase))
            return "/images/dispositivos/ups.svg";
        if (tipoDispositivo.Contains("puesto", StringComparison.OrdinalIgnoreCase)
            || tipoDispositivo.Contains("pc", StringComparison.OrdinalIgnoreCase))
            return "/images/dispositivos/pc.svg";
        return "/images/dispositivos/equipo-generico.svg";
    }

    /// <summary>
    /// Crea posiciones y estados visuales usando la capacidad exacta y los puertos registrados.
    /// </summary>
    /// <param name="equipo">Equipo mostrado; cambiarlo modifica el perfil, la capacidad y las interfaces leídas.</param>
    /// <param name="interfacesEquipo">Interfaces del equipo; cambiar la lista cambia etiquetas y estados disponibles.</param>
    /// <param name="identificadoresEnlazados">IDs con enlace; cambiar el conjunto altera qué puertos se ven ocupados.</param>
    /// <returns>Puertos visibles con ocupado documental, libre confirmado o desconocido.</returns>
    public static List<PuertoTopologiaViewModel> CrearPuertos(EquipoRed equipo,
        IReadOnlyList<InterfazEquipo> interfacesEquipo, ISet<int> identificadoresEnlazados,
        IReadOnlyDictionary<int, string> destinosPorInterfaz)
    {
        // perfilExacto aporta capacidad de fabricante solo cuando ambos nombres coinciden literalmente.
        var perfilExacto = string.Equals(equipo.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase)
            ? ObtenerPerfil(equipo.Fabricante, equipo.Modelo) : null;
        // resultadoPuertos acumula espacios de perfiles y las interfaces sin posición mapeada.
        var resultadoPuertos = new List<PuertoTopologiaViewModel>();
        // interfacesUsadas evita asignar una interfaz documental a más de una posición del dibujo.
        var interfacesUsadas = new HashSet<int>();

        // gruposCapacidad distingue conectores de formas distintas sin mezclarlos en un total ambiguo.
        var gruposCapacidad = new[]
        {
            new GrupoPuertos("RJ45", equipo.CantidadPuertosRj45 ?? perfilExacto?.CantidadRj45, ""),
            new GrupoPuertos("SFP", equipo.CantidadPuertosSfp ?? perfilExacto?.CantidadSfp, "SFP "),
            new GrupoPuertos("SFP+", equipo.CantidadPuertosSfpPlus ?? perfilExacto?.CantidadSfpPlus, "SFP+ "),
            new GrupoPuertos("QSFP", equipo.CantidadPuertosQsfp ?? perfilExacto?.CantidadQsfp, "QSFP "),
            new GrupoPuertos("QSFP+", equipo.CantidadPuertosQsfpPlus ?? perfilExacto?.CantidadQsfpPlus, "QSFP+ "),
            new GrupoPuertos("QSFP28", equipo.CantidadPuertosQsfp28 ?? perfilExacto?.CantidadQsfp28, "QSFP28 ")
        };

        foreach (var grupo in gruposCapacidad)
        {
            if (grupo.Cantidad is null) continue;
            for (var numeroPuerto = 1; numeroPuerto <= grupo.Cantidad.Value; numeroPuerto++)
            {
                // etiquetaEsperada representa una posición deducida del perfil exacto o de la capacidad ingresada.
                var etiquetaEsperada = grupo.Prefijo + numeroPuerto;
                // interfazEncontrada requiere coincidencia exacta normalizada; no adivina alias o formatos de rack.
                var interfazEncontrada = interfacesEquipo.FirstOrDefault(interfaz =>
                    !interfacesUsadas.Contains(interfaz.Identificador)
                    && string.Equals(interfaz.TipoInterfaz, grupo.Tipo, StringComparison.OrdinalIgnoreCase)
                    && NormalizarEtiqueta(interfaz.EtiquetaPuerto) == NormalizarEtiqueta(etiquetaEsperada));

                if (interfazEncontrada is not null)
                {
                    interfacesUsadas.Add(interfazEncontrada.Identificador);
                }
                resultadoPuertos.Add(CrearPuertoVisual(interfazEncontrada, etiquetaEsperada, grupo.Tipo, identificadoresEnlazados, destinosPorInterfaz));
            }
        }

        foreach (var interfazSinPosicion in interfacesEquipo.Where(interfaz => !interfacesUsadas.Contains(interfaz.Identificador)))
        {
            resultadoPuertos.Add(CrearPuertoVisual(interfazSinPosicion, interfazSinPosicion.EtiquetaPuerto,
                interfazSinPosicion.TipoInterfaz, identificadoresEnlazados, destinosPorInterfaz));
        }

        return resultadoPuertos;
    }

    /// <summary>
    /// Convierte una interfaz real o una posición de perfil a su estado visual.
    /// </summary>
    /// <param name="interfaz">Interfaz coincidente; nulo significa que no hay registro de esa posición.</param>
    /// <param name="etiqueta">Etiqueta a mostrar; cambiarla modifica el texto de la posición.</param>
    /// <param name="tipo">Tipo de conector; cambiarlo modifica la categoría de la posición.</param>
    /// <param name="identificadoresEnlazados">Conjunto de enlaces; cambiarlo puede marcar ocupado el puerto.</param>
    /// <returns>Modelo de puerto con estado documental y clase de color.</returns>
    private static PuertoTopologiaViewModel CrearPuertoVisual(InterfazEquipo? interfaz, string etiqueta, string tipo,
        ISet<int> identificadoresEnlazados, IReadOnlyDictionary<int, string> destinosPorInterfaz)
    {
        // ocupadoEnDocumento solo significa que existe una relación guardada, no que se midió conectividad actual.
        var ocupadoEnDocumento = interfaz is not null && identificadoresEnlazados.Contains(interfaz.Identificador);
        // libreConfirmado exige uso libre y vigencia confirmada; sin ambas señales el puerto sigue desconocido.
        var libreConfirmado = interfaz is not null && interfaz.EstadoUso == "Libre confirmado"
            && interfaz.EstadoDatos == "Confirmado";
        var claseVisual = ocupadoEnDocumento ? "puerto-ocupado"
            : libreConfirmado ? "puerto-libre" : "puerto-desconocido";
        var estadoVisual = ocupadoEnDocumento ? "Ocupado · enlace registrado"
            : libreConfirmado ? "Libre confirmado" : "Desconocido";

        return new PuertoTopologiaViewModel
        {
            IdentificadorInterfaz = interfaz?.Identificador,
            Etiqueta = etiqueta,
            Tipo = tipo,
            ClaseVisual = claseVisual,
            EstadoVisual = estadoVisual,
            DestinoConexion = interfaz is not null && destinosPorInterfaz.TryGetValue(interfaz.Identificador, out var destino)
                ? destino : null
        };
    }

    /// <summary>
    /// Normaliza espacios y mayúsculas para comparar etiquetas de puerto sin borrar su numeración.
    /// </summary>
    /// <param name="etiqueta">Etiqueta ingresada; cambiarla puede hacer coincidir otra posición.</param>
    /// <returns>Etiqueta compacta en mayúsculas.</returns>
    private static string NormalizarEtiqueta(string etiqueta)
    {
        // etiquetaNormalizada elimina solo espacios; los separadores y números permanecen exactos.
        var etiquetaNormalizada = new string(etiqueta.Where(caracter => !char.IsWhiteSpace(caracter)).ToArray());
        return etiquetaNormalizada.ToUpperInvariant();
    }

    private sealed record GrupoPuertos(string Tipo, int? Cantidad, string Prefijo);
}

/// <summary>
/// Perfil físico de una variante exacta de switch.
/// </summary>
/// <param name="Icono">Ilustración local de la variante.</param>
/// <param name="CantidadRj45">Puertos RJ45, o cero si este modelo no los tiene.</param>
/// <param name="CantidadSfp">Puertos SFP.</param>
/// <param name="CantidadSfpPlus">Puertos SFP+.</param>
/// <param name="CantidadQsfp">Puertos QSFP.</param>
/// <param name="CantidadQsfpPlus">Puertos QSFP+.</param>
/// <param name="CantidadQsfp28">Puertos QSFP28.</param>
/// <param name="Fuente">Documento del fabricante que respalda el perfil.</param>
public sealed record PerfilSwitchVisual(string Icono, int CantidadRj45, int CantidadSfp, int CantidadSfpPlus,
    int CantidadQsfp, int CantidadQsfpPlus, int CantidadQsfp28, string Fuente);
