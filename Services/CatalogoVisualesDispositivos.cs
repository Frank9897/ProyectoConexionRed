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
        // modeloNormalizado admite el identificador comercial con sufijo 48G del mismo Dell S4148U-ON.
        var modeloNormalizado = string.Equals(fabricante?.Trim(), "Dell", StringComparison.OrdinalIgnoreCase)
            && string.Equals(modelo?.Trim(), "S4148U-ON 48G", StringComparison.OrdinalIgnoreCase)
                ? "S4148U-ON" : modelo?.Trim();
        // clavePerfil requiere fabricante y modelo completo reconocido para no emparejar variantes parciales.
        var clavePerfil = $"{fabricante?.Trim()}|{modeloNormalizado}";
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
            new GrupoPuertos("RJ45", perfilExacto?.CantidadRj45 ?? equipo.CantidadPuertosRj45, ""),
            new GrupoPuertos("SFP", perfilExacto?.CantidadSfp ?? equipo.CantidadPuertosSfp, "SFP "),
            new GrupoPuertos("SFP+", perfilExacto?.CantidadSfpPlus ?? equipo.CantidadPuertosSfpPlus, "SFP+ "),
            new GrupoPuertos("QSFP", perfilExacto?.CantidadQsfp ?? equipo.CantidadPuertosQsfp, "QSFP "),
            new GrupoPuertos("QSFP+", perfilExacto?.CantidadQsfpPlus ?? equipo.CantidadPuertosQsfpPlus, "QSFP+ "),
            new GrupoPuertos("QSFP28", perfilExacto?.CantidadQsfp28 ?? equipo.CantidadPuertosQsfp28, "QSFP28 ")
        };

        foreach (var grupo in gruposCapacidad)
        {
            if (grupo.Cantidad is null) continue;
            for (var numeroPuerto = 1; numeroPuerto <= grupo.Cantidad.Value; numeroPuerto++)
            {
                // etiquetaEsperada representa la boca física del grupo sin mezclar la numeración de otras jaulas.
                var etiquetaEsperada = grupo.Prefijo + numeroPuerto;
                // interfazEncontrada coteja el tipo y número exactos tras normalizar etiquetas abreviadas de la fuente.
                var interfazEncontrada = interfacesEquipo.FirstOrDefault(interfaz =>
                    !interfacesUsadas.Contains(interfaz.Identificador)
                    && string.Equals(interfaz.TipoInterfaz, grupo.Tipo, StringComparison.OrdinalIgnoreCase)
                    && NormalizarEtiqueta(NormalizarEtiquetaPuertoVisual(equipo.Nombre, interfaz.EtiquetaPuerto, interfaz.TipoInterfaz))
                        == NormalizarEtiqueta(etiquetaEsperada));

                if (interfazEncontrada is not null)
                {
                    interfacesUsadas.Add(interfazEncontrada.Identificador);
                }
                resultadoPuertos.Add(CrearPuertoVisual(equipo, interfazEncontrada, etiquetaEsperada, grupo.Tipo,
                    identificadoresEnlazados, destinosPorInterfaz));
            }
        }

        // interfacesSinPosicion ordena números como 2 antes de 10 y conserva después las etiquetas no numéricas.
        var interfacesSinPosicion = interfacesEquipo
            .Where(interfaz => !interfacesUsadas.Contains(interfaz.Identificador))
            // Las filas SFP adicionales del HP 1920 se conservan en SQLite, pero no se dibujan como jaulas fuera de capacidad.
            .Where(interfaz => !(string.Equals(equipo.Nombre, "Switch HP 1920", StringComparison.OrdinalIgnoreCase)
                && string.Equals(interfaz.TipoInterfaz, "SFP", StringComparison.OrdinalIgnoreCase)
                && interfaz.Observaciones?.Contains("Excel6", StringComparison.OrdinalIgnoreCase) == true
                && !new[] { "SFP 1", "SFP 2", "SFP 3", "SFP 4", "SFP 25", "SFP 26", "SFP 27", "SFP 28" }
                    .Contains(interfaz.EtiquetaPuerto, StringComparer.OrdinalIgnoreCase)))
            .Where(interfaz => !EsEtiquetaFueraCapacidadS4148(equipo, interfaz))
            .OrderBy(interfaz => ObtenerOrdenEtiquetaPuerto(interfaz.EtiquetaPuerto))
            .ThenBy(interfaz => interfaz.EtiquetaPuerto, StringComparer.OrdinalIgnoreCase);

        foreach (var interfazSinPosicion in interfacesSinPosicion)
        {
            // etiquetaSinPosicion muestra el formato de interfaz conservando la numeración local del conector.
            var etiquetaSinPosicion = NormalizarEtiquetaPuertoVisual(
                equipo.Nombre, interfazSinPosicion.EtiquetaPuerto, interfazSinPosicion.TipoInterfaz);
            resultadoPuertos.Add(CrearPuertoVisual(equipo, interfazSinPosicion, etiquetaSinPosicion,
                interfazSinPosicion.TipoInterfaz, identificadoresEnlazados, destinosPorInterfaz));
        }

        return resultadoPuertos;
    }

    /// <summary>
    /// Normaliza etiquetas del HP 1920 y etiquetas numéricas de puertos por tipo de interfaz.
    /// </summary>
    /// <param name="nombreEquipo">Equipo propietario; cambiarlo puede activar una regla visual específica.</param>
    /// <param name="etiqueta">Rótulo registrado; cambiarlo altera la numeración que se muestra.</param>
    /// <returns>Etiqueta de puerto normalizada según la familia de interfaz.</returns>
    public static string NormalizarEtiquetaPuertoVisual(string nombreEquipo, string etiqueta, string? tipoInterfaz = null)
    {
        // etiquetaNormalizada elimina espacios externos para comparar las etiquetas guardadas.
        var etiquetaNormalizada = etiqueta.Trim();
        if (string.Equals(nombreEquipo, "Switch HP 1920", StringComparison.OrdinalIgnoreCase)
            && etiquetaNormalizada.StartsWith("SFP ", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(etiquetaNormalizada.Substring(4).Trim(), out var numeroSfp)
            && numeroSfp is >= 25 and <= 28)
        {
            // puertoSfpLocal traduce el conteo consecutivo del Visio a las cuatro jaulas SFP del HP 1920.
            var puertoSfpLocal = numeroSfp - 24;
            return "SFP " + puertoSfpLocal;
        }

        // textoNumero permite comparar una etiqueta "1" con el grupo de conectores SFP+ o QSFP correspondiente.
        var textoNumero = new string(etiquetaNormalizada.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        if (!string.IsNullOrWhiteSpace(tipoInterfaz) && int.TryParse(textoNumero, out var numeroInterfaz))
        {
            // tipoNormalizado mantiene numeración local independiente para cada familia de jaulas físicas.
            var tipoNormalizado = tipoInterfaz.Trim().ToUpperInvariant();
            return tipoNormalizado == "RJ45" ? numeroInterfaz.ToString() : tipoNormalizado + " " + numeroInterfaz;
        }

        return etiquetaNormalizada;
    }

    /// <summary>Filtra filas que exceden la capacidad física documentada del Dell S4148U-ON.</summary>
    /// <param name="equipo">Switch propietario; cambiarlo puede activar o desactivar el filtro.</param>
    /// <param name="interfaz">Interfaz registrada; cambiarla modifica si queda visible como posición extra.</param>
    /// <returns>Verdadero cuando la etiqueta supera el máximo físico de su grupo.</returns>
    private static bool EsEtiquetaFueraCapacidadS4148(EquipoRed equipo, InterfazEquipo interfaz)
    {
        if (!equipo.Nombre.Contains("S4148U", StringComparison.OrdinalIgnoreCase))
            return false;

        // textoNumero extrae el sufijo numérico sin confundir el tipo de jaula.
        var textoNumero = new string(interfaz.EtiquetaPuerto.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        if (!int.TryParse(textoNumero, out var numeroInterfaz))
            return false;

        // tipoInterfaz determina el máximo real de puertos que puede presentar esta familia de conectores.
        var tipoInterfaz = interfaz.TipoInterfaz.Trim().ToUpperInvariant();
        return tipoInterfaz switch
        {
            "SFP+" => numeroInterfaz is < 1 or > 48,
            "QSFP+" => numeroInterfaz is < 1 or > 2,
            "QSFP28" => numeroInterfaz is < 1 or > 4,
            _ => false
        };
    }

    /// <summary>
    /// Obtiene el primer número de una etiqueta para ordenar puertos de manera natural.
    /// </summary>
    /// <param name="etiquetaPuerto">Texto del puerto; cambiarlo puede modificar su posición en el listado.</param>
    /// <returns>Número encontrado o máximo valor para etiquetas sin número.</returns>
    private static int ObtenerOrdenEtiquetaPuerto(string etiquetaPuerto)
    {
        // digitosPuerto conserva el primer grupo numérico encontrado sin depender de un formato único.
        var digitosPuerto = new string(etiquetaPuerto.SkipWhile(caracter => !char.IsDigit(caracter))
            .TakeWhile(char.IsDigit).ToArray());
        // numeroPuerto coloca al final las etiquetas que no incluyen dígitos.
        return int.TryParse(digitosPuerto, out var numeroPuerto) ? numeroPuerto : int.MaxValue;
    }

    /// <summary>
    /// Convierte una interfaz real o una posición de perfil a su estado visual.
    /// </summary>
    /// <param name="interfaz">Interfaz coincidente; nulo significa que no hay registro de esa posición.</param>
    /// <param name="etiqueta">Etiqueta a mostrar; cambiarla modifica el texto de la posición.</param>
    /// <param name="tipo">Tipo de conector; cambiarlo modifica la categoría de la posición.</param>
    /// <param name="identificadoresEnlazados">Conjunto de enlaces; cambiarlo puede marcar ocupado el puerto.</param>
    /// <returns>Modelo de puerto con estado documental y clase de color.</returns>
    private static PuertoTopologiaViewModel CrearPuertoVisual(EquipoRed equipo, InterfazEquipo? interfaz, string etiqueta, string tipo,
        ISet<int> identificadoresEnlazados, IReadOnlyDictionary<int, string> destinosPorInterfaz)
    {
        // ocupadoEnDocumento solo significa que existe una relación guardada, no que se midió conectividad actual.
        var ocupadoEnDocumento = interfaz is not null && identificadoresEnlazados.Contains(interfaz.Identificador);
        // libreConfirmado exige uso libre y vigencia confirmada; sin ambas señales el puerto sigue desconocido.
        var libreConfirmado = interfaz is not null && interfaz.EstadoUso == "Libre confirmado"
            && interfaz.EstadoDatos == "Confirmado";
        var claseVisual = ocupadoEnDocumento ? "puerto-ocupado"
            : libreConfirmado ? "puerto-libre" : "puerto-desconocido";
        // moduloInstalado refleja la indicación del usuario para cada interfaz ocupada del Dell S4148U.
        var moduloInstalado = ocupadoEnDocumento && equipo.Nombre.Contains("S4148U", StringComparison.OrdinalIgnoreCase)
            ? "Transceptor RJ45" : null;
        var estadoVisual = ocupadoEnDocumento
            ? moduloInstalado is null ? "Ocupado · relación documentada" : "Ocupado · transceptor RJ45"
            : libreConfirmado ? "Libre confirmado" : "Desconocido";

        return new PuertoTopologiaViewModel
        {
            IdentificadorInterfaz = interfaz?.Identificador,
            Etiqueta = etiqueta,
            Tipo = tipo,
            ClaseVisual = claseVisual,
            EstadoVisual = estadoVisual,
            ModuloInstalado = moduloInstalado,
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
