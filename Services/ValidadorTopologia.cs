using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Services;

/// <summary>Gravedad de un hallazgo: informa, avisa sin bloquear o impide guardar.</summary>
public enum SeveridadValidacion
{
    Informacion = 0,
    Aviso = 1,
    Error = 2
}

/// <summary>Un resultado concreto de validación, con un código estable para que la pantalla lo distinga.</summary>
/// <param name="Severidad">Gravedad del hallazgo.</param>
/// <param name="Codigo">Identificador estable, por ejemplo PUERTO_OCUPADO.</param>
/// <param name="Mensaje">Texto en español, claro y accionable, que se muestra al técnico.</param>
public sealed record HallazgoValidacion(SeveridadValidacion Severidad, string Codigo, string Mensaje);

/// <summary>Reúne los hallazgos de una validación.</summary>
public sealed class ResultadoValidacion
{
    // hallazgos conserva el orden en que se detectaron.
    private readonly List<HallazgoValidacion> hallazgos = [];

    /// <summary>Todos los hallazgos.</summary>
    public IReadOnlyList<HallazgoValidacion> Hallazgos => hallazgos;

    /// <summary>Hallazgos que impiden guardar.</summary>
    public IEnumerable<HallazgoValidacion> Errores => hallazgos.Where(h => h.Severidad == SeveridadValidacion.Error);

    /// <summary>Hallazgos que se guardan pero se notifican.</summary>
    public IEnumerable<HallazgoValidacion> Avisos => hallazgos.Where(h => h.Severidad == SeveridadValidacion.Aviso);

    /// <summary>Verdadero si hay al menos un error.</summary>
    public bool TieneErrores => hallazgos.Any(h => h.Severidad == SeveridadValidacion.Error);

    /// <summary>Agrega un hallazgo al resultado.</summary>
    public void Agregar(SeveridadValidacion severidad, string codigo, string mensaje) =>
        hallazgos.Add(new HallazgoValidacion(severidad, codigo, mensaje));
}

/// <summary>
/// Reglas topológicas compartidas por formularios y, más adelante, por el mapa editable.
/// Solo lee el inventario: no consulta ni cambia la red real.
/// </summary>
public static class ValidadorTopologia
{
    /// <summary>
    /// Códigos que el formulario de enlaces ya comprueba por su cuenta; el formulario los omite para no repetir mensajes.
    /// </summary>
    public static readonly IReadOnlySet<string> CodigosYaVerificadosPorFormulario =
        new HashSet<string> { "PUERTO_INEXISTENTE", "MISMO_PUERTO", "PUERTO_OCUPADO", "RELACION_PARCIAL" };

    // prefijosIpHabituales son las redes documentadas del DGC; fuera de ellas solo se avisa.
    private static readonly string[] PrefijosIpHabituales = ["192.168.0.", "192.168.1.", "10.0.1."];

    /// <summary>
    /// Valida un enlace entre dos puertos: existencia, ocupación, conectores, velocidades y posibles bucles.
    /// </summary>
    /// <param name="contexto">Inventario consultado; cambiarlo valida contra otra sede.</param>
    /// <param name="idPuertoA">Primer puerto del enlace.</param>
    /// <param name="idPuertoB">Segundo puerto del enlace.</param>
    /// <param name="idEnlaceExcluido">Enlace existente que se está editando y no debe contar como ocupación; nulo al crear.</param>
    /// <param name="cancelacion">Señal de cancelación.</param>
    /// <returns>Errores (no guardar), avisos (guardar y notificar) e información.</returns>
    public static async Task<ResultadoValidacion> ValidarEnlaceAsync(
        InventarioDbContext contexto, int idPuertoA, int idPuertoB, int? idEnlaceExcluido, CancellationToken cancelacion)
    {
        var resultado = new ResultadoValidacion();
        if (idPuertoA == idPuertoB)
        {
            resultado.Agregar(SeveridadValidacion.Error, "MISMO_PUERTO", "El origen y el destino deben ser puertos distintos.");
            return resultado;
        }

        // extremos une cada puerto con su equipo para poder nombrarlos en los mensajes.
        var extremos = await (
            from interfaz in contexto.InterfacesEquipo.AsNoTracking()
            join equipo in contexto.Equipos.AsNoTracking() on interfaz.IdentificadorEquipo equals equipo.Identificador
            where interfaz.Identificador == idPuertoA || interfaz.Identificador == idPuertoB
            select new { Interfaz = interfaz, Equipo = equipo }).ToListAsync(cancelacion);
        var extremoA = extremos.FirstOrDefault(x => x.Interfaz.Identificador == idPuertoA);
        var extremoB = extremos.FirstOrDefault(x => x.Interfaz.Identificador == idPuertoB);
        if (extremoA is null || extremoB is null)
        {
            resultado.Agregar(SeveridadValidacion.Error, "PUERTO_INEXISTENTE", "Selecciona dos puertos existentes.");
            return resultado;
        }

        if (extremoA.Equipo.Identificador == extremoB.Equipo.Identificador)
        {
            resultado.Agregar(SeveridadValidacion.Error, "MISMO_EQUIPO",
                $"Los dos puertos pertenecen a {extremoA.Equipo.Nombre}: un cable entre dos puertos del mismo equipo genera un bucle.");
        }

        // excluido evita contar el propio enlace cuando se edita uno existente.
        var excluido = idEnlaceExcluido ?? -1;
        var enlacesEnPuertos = await contexto.EnlacesFisicos.AsNoTracking()
            .Where(e => e.Identificador != excluido
                && (e.IdentificadorPuertoOrigen == idPuertoA || e.IdentificadorPuertoDestino == idPuertoA
                    || e.IdentificadorPuertoOrigen == idPuertoB || e.IdentificadorPuertoDestino == idPuertoB))
            .ToListAsync(cancelacion);
        foreach (var extremo in new[] { extremoA, extremoB })
        {
            if (enlacesEnPuertos.Any(e => e.IdentificadorPuertoOrigen == extremo.Interfaz.Identificador
                || e.IdentificadorPuertoDestino == extremo.Interfaz.Identificador))
            {
                resultado.Agregar(SeveridadValidacion.Error, "PUERTO_OCUPADO",
                    $"El puerto {extremo.Interfaz.EtiquetaPuerto} de {extremo.Equipo.Nombre} ya tiene un enlace. "
                    + "Un puerto no puede conectarse a dos equipos a la vez, salvo mediante un switch no administrable intermedio.");
            }

            if (await contexto.ConexionesDocumentadas.AsNoTracking()
                    .AnyAsync(r => r.IdentificadorPuertoLocal == extremo.Interfaz.Identificador, cancelacion))
            {
                resultado.Agregar(SeveridadValidacion.Error, "RELACION_PARCIAL",
                    $"El puerto {extremo.Interfaz.EtiquetaPuerto} de {extremo.Equipo.Nombre} ya tiene una relación documental parcial.");
            }
        }

        AgregarHallazgosDeConectores(resultado, extremoA.Interfaz, extremoA.Equipo, extremoB.Interfaz, extremoB.Equipo);

        if (extremoA.Equipo.Identificador != extremoB.Equipo.Identificador
            && await ExisteCaminoAsync(contexto, extremoA.Equipo.Identificador, extremoB.Equipo.Identificador, excluido, cancelacion))
        {
            // sinStp indica que un extremo es un switch no administrable: no tiene STP y el bucle sería real.
            var sinStp = extremoA.Equipo.EsAdministrable == false || extremoB.Equipo.EsAdministrable == false;
            resultado.Agregar(SeveridadValidacion.Aviso, "POSIBLE_BUCLE", sinStp
                ? $"Ya existe un camino entre {extremoA.Equipo.Nombre} y {extremoB.Equipo.Nombre} y uno de los extremos es un switch no administrable, que no tiene STP: este enlace crearía un bucle real."
                : $"Ya existe un camino entre {extremoA.Equipo.Nombre} y {extremoB.Equipo.Nombre}. Con STP o RSTP un enlace redundante es normal, pero documenta qué enlace queda activo y cuál bloqueado para evitar bucles.");
        }

        return resultado;
    }

    /// <summary>
    /// Valida el formato, el rango y la unicidad de la IP de un equipo.
    /// </summary>
    /// <param name="ip">IP escrita; vacía no genera hallazgos.</param>
    /// <param name="idEquipoActual">Equipo que se edita, para no compararlo consigo mismo; nulo al crear.</param>
    /// <param name="equipos">Equipos existentes contra los que se compara.</param>
    /// <returns>Error si el formato es inválido o la IP está duplicada; aviso si está fuera de las redes habituales.</returns>
    public static ResultadoValidacion ValidarIpEquipo(string? ip, int? idEquipoActual, IEnumerable<EquipoRed> equipos)
    {
        var resultado = new ResultadoValidacion();
        var texto = ip?.Trim();
        if (string.IsNullOrEmpty(texto)) return resultado;

        var partes = texto.Split('.');
        if (partes.Length != 4 || partes.Any(p => !byte.TryParse(p, NumberStyles.None, CultureInfo.InvariantCulture, out _)))
        {
            resultado.Agregar(SeveridadValidacion.Error, "IP_FORMATO", $"«{texto}» no es una dirección IPv4 válida (por ejemplo 192.168.0.25).");
            return resultado;
        }

        if (!PrefijosIpHabituales.Any(prefijo => texto.StartsWith(prefijo, StringComparison.Ordinal)))
        {
            resultado.Agregar(SeveridadValidacion.Aviso, "IP_FUERA_DE_RANGO",
                $"La IP {texto} está fuera de las redes habituales (192.168.0.x, 192.168.1.x y 10.0.1.x). Confírmala antes de guardar.");
        }

        var duplicado = equipos.FirstOrDefault(e => e.Identificador != idEquipoActual
            && string.Equals(e.IpAdministracion?.Trim(), texto, StringComparison.Ordinal));
        if (duplicado is not null)
        {
            resultado.Agregar(SeveridadValidacion.Error, "IP_DUPLICADA", $"La IP {texto} ya está asignada a {duplicado.Nombre}.");
        }

        return resultado;
    }

    /// <summary>Agrega avisos sobre tipo de conector y velocidad entre los dos extremos.</summary>
    private static void AgregarHallazgosDeConectores(
        ResultadoValidacion resultado, InterfazEquipo interfazA, EquipoRed equipoA, InterfazEquipo interfazB, EquipoRed equipoB)
    {
        var familiaA = FamiliaConector(interfazA.TipoInterfaz);
        var familiaB = FamiliaConector(interfazB.TipoInterfaz);
        if (familiaA != "?" && familiaB != "?" && familiaA != familiaB)
        {
            var hayRj45 = familiaA == "RJ45" || familiaB == "RJ45";
            resultado.Agregar(SeveridadValidacion.Aviso, hayRj45 ? "RJ45_CON_OPTICO" : "CONECTORES_DISTINTOS", hayRj45
                ? $"Se une un puerto RJ45 con uno {(familiaA == "RJ45" ? familiaB : familiaA)}: solo funciona con un transceptor RJ45 en el puerto óptico. Indícalo en el módulo del enlace."
                : $"Los conectores son distintos ({familiaA} y {familiaB}): confirma que los módulos sean compatibles; la velocidad será la del más lento.");
        }

        if (interfazA.VelocidadMbps is int velocidadA && interfazB.VelocidadMbps is int velocidadB && velocidadA != velocidadB)
        {
            resultado.Agregar(SeveridadValidacion.Aviso, "VELOCIDAD_DISTINTA",
                $"Las velocidades declaradas difieren ({OpcionesTopologia.EtiquetaVelocidad(velocidadA)} en {equipoA.Nombre} y {OpcionesTopologia.EtiquetaVelocidad(velocidadB)} en {equipoB.Nombre}); el enlace negociará {OpcionesTopologia.EtiquetaVelocidad(Math.Min(velocidadA, velocidadB))}.");
        }
    }

    /// <summary>Clasifica el tipo de interfaz guardado en una familia de conector; «?» si no se puede saber.</summary>
    private static string FamiliaConector(string? tipoInterfaz)
    {
        var tipo = (tipoInterfaz ?? string.Empty).ToUpperInvariant();
        if (tipo.Contains("QSFP28", StringComparison.Ordinal)) return "QSFP28";
        if (tipo.Contains("QSFP+", StringComparison.Ordinal)) return "QSFP+";
        if (tipo.Contains("QSFP", StringComparison.Ordinal)) return "QSFP";
        if (tipo.Contains("SFP+", StringComparison.Ordinal)) return "SFP+";
        if (tipo.Contains("SFP", StringComparison.Ordinal)) return "SFP";
        if (tipo.Contains("RJ45", StringComparison.Ordinal) || tipo.Contains("UTP", StringComparison.Ordinal)
            || tipo.Contains("ETHERNET", StringComparison.Ordinal)) return "RJ45";
        return "?";
    }

    /// <summary>
    /// Indica si ya hay un camino entre dos equipos usando los enlaces guardados, sin contar el enlace excluido.
    /// </summary>
    private static async Task<bool> ExisteCaminoAsync(
        InventarioDbContext contexto, int equipoInicio, int equipoFin, int enlaceExcluido, CancellationToken cancelacion)
    {
        var enlaces = await contexto.EnlacesFisicos.AsNoTracking()
            .Where(e => e.Identificador != enlaceExcluido)
            .Select(e => new { e.IdentificadorPuertoOrigen, e.IdentificadorPuertoDestino })
            .ToListAsync(cancelacion);
        var equipoPorPuerto = await contexto.InterfacesEquipo.AsNoTracking()
            .Select(i => new { i.Identificador, i.IdentificadorEquipo })
            .ToDictionaryAsync(i => i.Identificador, i => i.IdentificadorEquipo, cancelacion);

        // adyacencia une equipos con enlace directo; un puerto sin ficha se ignora.
        var adyacencia = new Dictionary<int, List<int>>();
        foreach (var enlace in enlaces)
        {
            if (!equipoPorPuerto.TryGetValue(enlace.IdentificadorPuertoOrigen, out var origen)
                || !equipoPorPuerto.TryGetValue(enlace.IdentificadorPuertoDestino, out var destino)) continue;
            if (!adyacencia.TryGetValue(origen, out var vecinosOrigen)) adyacencia[origen] = vecinosOrigen = [];
            if (!adyacencia.TryGetValue(destino, out var vecinosDestino)) adyacencia[destino] = vecinosDestino = [];
            vecinosOrigen.Add(destino);
            vecinosDestino.Add(origen);
        }

        // Recorrido en anchura desde el inicio; si alcanza el fin ya había un camino.
        var visitados = new HashSet<int> { equipoInicio };
        var pendientes = new Queue<int>();
        pendientes.Enqueue(equipoInicio);
        while (pendientes.Count > 0)
        {
            var actual = pendientes.Dequeue();
            if (actual == equipoFin) return true;
            if (!adyacencia.TryGetValue(actual, out var vecinos)) continue;
            foreach (var vecino in vecinos.Where(visitados.Add)) pendientes.Enqueue(vecino);
        }

        return false;
    }
}
