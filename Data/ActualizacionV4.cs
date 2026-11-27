using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Models;
using ProyectoConexionRed.Web.Services;

namespace ProyectoConexionRed.Web.Data;

/// <summary>
/// Incorpora DIAGRAMA RED V4.0 (cuatro SFP por switch, equipos del esquema de internet) y completa las fichas
/// con el escaneo de la LAN. Todo es idempotente, solo escribe en SQLite y nunca consulta ni cambia la red real.
/// </summary>
internal static class ActualizacionV4
{
    // UsuarioImportacion es el autor que se muestra en el historial; cambiarlo modifica la atribución de los eventos.
    private const string UsuarioImportacion = "Carga documental DIAGRAMA RED V4.0";

    // CantidadSfpPorSwitch es la regla confirmada por el usuario: todos los switches del Visio, salvo el Dell S4148U-ON, tienen 4 SFP.
    private const int CantidadSfpPorSwitch = 4;

    // CrearEquiposDetectadosSinDocumentar decide si los hosts del escaneo que no están en el inventario se dan de alta
    // como «A verificar»; ponerlo en false deja el escaneo solo para completar fichas existentes.
    private static readonly bool CrearEquiposDetectadosSinDocumentar = true;

    // SwitchesDe24Puertos lista los switches cuya tabla del Visio o cuyo nombre indica 24 puertos RJ45; si el equipo
    // no tiene cantidad RJ45 guardada se le asigna 24. Un nombre ausente de la lista no recibe cantidad RJ45.
    private static readonly HashSet<string> SwitchesDe24Puertos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Switch Aruba 1930 (WDGC)", "HP ProCurve 29100I-24G", "Switch HP 5500", "Switch HP 1920", "Switch HP 1920S",
        "Aruba 1930 (reserva)", "Switch Aruba 1930 (Reserva 2)", "Switch principal de Mesa de Entradas",
        "Switch Agrimensura 1 HP", "Switch Agrimensura 3Com", "Asuntos Jurídicos", "Cartografía", "Inmuebles",
        "Mesa de Entrada (No principal)", "Puerta Agrimensura", "Notificadores", "Secretaría", "Sistemas",
        "Valuación R0", "Valuación R1", "Valuación R2"
    };

    // PatronMac valida el formato AA:BB:CC:DD:EE:FF; un valor distinto no se copia al inventario.
    private static readonly Regex PatronMac = new(@"^([0-9A-F]{2}:){5}[0-9A-F]{2}$", RegexOptions.Compiled);

    // PatronIp reconoce nombres que en realidad son una IP; el escáner nombra así a los equipos que no resuelve.
    private static readonly Regex PatronIp = new(@"^(\d{1,3}\.){3}\d{1,3}$", RegexOptions.Compiled);

    /// <summary>Equipo nuevo que aparece definido en las páginas RACK 1 y ESQUEMA CONEXIÓN INTERNET del V4.</summary>
    private sealed record EquipoEsquemaV4(
        string Nombre, string? Fabricante, string? Modelo, string? Ip, string Tipo, string Rol,
        string Observaciones, int? PuertosRj45, string[] Interfaces);

    /// <summary>Enlace dibujado en el esquema de internet; Estado indica si la fuente lo respalda de forma directa.</summary>
    private sealed record EnlaceEsquemaV4(
        string EquipoA, string PuertoA, string EquipoB, string PuertoB, string Estado, string Referencia,
        string? RelacionParcialPorReemplazar);

    // EquiposEsquema reúne solo lo que el V4 rotula de forma legible; IP y modelos no escritos quedan vacíos.
    private static readonly EquipoEsquemaV4[] EquiposEsquema =
    [
        new("Intellinet 8 puertos", "Intellinet", null, null, "Switch", "Internet (WAN)",
            "DIAGRAMA RED V4.0, ESQUEMA CONEXIÓN INTERNET: switch de 8 puertos rotulado INTELLINET. P-8 va al módem Claro, P-1 a WAN3 de PF y P-5 a WAN3 de F. Modelo e IP no documentados.",
            8, []),
        new("Módem Claro", null, null, null, "Router", "Internet (WAN)",
            "DIAGRAMA RED V4.0: módem Claro conectado al puerto P-8 del Intellinet.",
            null, ["Puerto LAN"]),
        new("Módem Personal", null, null, null, "Router", "Internet (WAN)",
            "DIAGRAMA RED V4.0: módem Personal; el esquema lo une con la interfaz WAN de PF.",
            null, ["Puerto LAN"]),
        new("PF-SENSE Subdominio", null, null, "192.168.0.9", "Firewall", "Internet (WAN)",
            "Equipo PF del V4 (PC LINUX 2.7 en el rack). LAN al puerto 7 del HP 1920 (Excel5: PF-SENSE Subdominio, 192.168.0.9). Dibujado con interfaces WAN, WAN 3 y LAN.",
            null, ["WAN", "WAN 3", "LAN"]),
        new("PF-SENSE INTERNET", null, null, "192.168.0.182", "Firewall", "Internet (WAN)",
            "Equipo F del V4 (el rack lo rotula PC CAMARAS y el esquema PC LINUX 2.6; confirmar cuál es). LAN al puerto 10 del HP 1920 (Excel5: PF-SENSE INTERNET, 192.168.0.182). Dibujado con interfaces WAN, WAN 3 y LAN.",
            null, ["WAN", "WAN 3", "LAN"]),
        new("Dell R740 (O)", "Dell", "R740", null, "Servidor", "Servidor",
            "DIAGRAMA RED V4.0 dibuja dos R740 rotulados O y P. Los iDRAC 192.168.0.249 y 192.168.0.251 figuran en el HP 5500 (puertos 4 y 3), pero el Visio no dice a cuál R740 corresponde cada uno; no se atribuyen.",
            null, []),
        new("Dell R740 (P)", "Dell", "R740", null, "Servidor", "Servidor",
            "DIAGRAMA RED V4.0 dibuja dos R740 rotulados O y P. Los iDRAC 192.168.0.249 y 192.168.0.251 figuran en el HP 5500 (puertos 4 y 3), pero el Visio no dice a cuál R740 corresponde cada uno; no se atribuyen.",
            null, [])
    ];

    // EnlacesEsquema reúne las conexiones rotuladas en la página ESQUEMA CONEXIÓN INTERNET del V4.
    private static readonly EnlaceEsquemaV4[] EnlacesEsquema =
    [
        new("Switch HP 1920", "7", "PF-SENSE Subdominio", "LAN", "Confirmado",
            "Excel5 fila 8 y rótulo E-P07 del esquema", "PF-SENSE"),
        new("Switch HP 1920", "10", "PF-SENSE INTERNET", "LAN", "Confirmado",
            "Excel5 fila 11 y rótulo E-P10 del esquema", "PF-SENSE"),
        new("Intellinet 8 puertos", "8", "Módem Claro", "Puerto LAN", "Confirmado",
            "Conector pegado entre INTELLINET P-8 y MODEM CLARO", null),
        new("Intellinet 8 puertos", "1", "PF-SENSE Subdominio", "WAN 3", "A verificar",
            "Rótulos INTELLINET P-1 y PF-WAN3", null),
        new("Intellinet 8 puertos", "5", "PF-SENSE INTERNET", "WAN 3", "A verificar",
            "Rótulos INTELLINET P-5 y F-WAN3", null),
        new("PF-SENSE Subdominio", "WAN", "Módem Personal", "Puerto LAN", "A verificar",
            "Flecha del esquema entre la WAN de PF y MODEM PERSONAL", null)
    ];

    /// <summary>
    /// Aplica la actualización V4 completa dentro de una transacción; si algo falla no se guarda nada parcial.
    /// </summary>
    /// <param name="contextoInventario">Contexto SQLite; cambiarlo selecciona qué inventario recibe la actualización.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla revierte los cambios pendientes.</param>
    public static async Task AplicarAsync(InventarioDbContext contextoInventario, CancellationToken cancelacion)
    {
        // cambiosAuditados acumula los eventos que se guardan junto con los datos.
        var cambiosAuditados = new List<EventoModificacion>();
        await using var transaccion = await contextoInventario.Database.BeginTransactionAsync(cancelacion);

        await AplicarCapacidadesSwitchAsync(contextoInventario, cambiosAuditados, cancelacion);
        await IncorporarEsquemaInternetAsync(contextoInventario, cambiosAuditados, cancelacion);
        await IncorporarEscaneoLanAsync(contextoInventario, cambiosAuditados, cancelacion);

        if (cambiosAuditados.Count > 0)
        {
            await contextoInventario.EventosModificacion.AddRangeAsync(cambiosAuditados, cancelacion);
            await contextoInventario.SaveChangesAsync(cancelacion);
        }

        await transaccion.CommitAsync(cancelacion);
    }

    /// <summary>
    /// Fija cuatro SFP por switch (salvo S4148U, Cisco y el de 8 puertos), retira las filas SFP sobrantes sin enlace
    /// y completa las jaulas que falten.
    /// </summary>
    private static async Task AplicarCapacidadesSwitchAsync(
        InventarioDbContext contextoInventario, List<EventoModificacion> cambiosAuditados, CancellationToken cancelacion)
    {
        // switches son las fichas de tipo Switch; el resto de equipos no se toca.
        var switches = await contextoInventario.Equipos
            .Where(equipo => equipo.TipoDispositivo == "Switch").ToListAsync(cancelacion);
        // todasLasInterfaces permite revisar y completar puertos sin una consulta por switch.
        var todasLasInterfaces = await contextoInventario.InterfacesEquipo.ToListAsync(cancelacion);
        // puertosConRegistro protege los puertos que ya participan en un enlace o relación documentada.
        var puertosConRegistro = (await contextoInventario.EnlacesFisicos
                .Select(enlace => new { enlace.IdentificadorPuertoOrigen, enlace.IdentificadorPuertoDestino })
                .ToListAsync(cancelacion))
            .SelectMany(enlace => new[] { enlace.IdentificadorPuertoOrigen, enlace.IdentificadorPuertoDestino })
            .Concat(await contextoInventario.ConexionesDocumentadas
                .Select(relacion => relacion.IdentificadorPuertoLocal).ToListAsync(cancelacion))
            .ToHashSet();

        foreach (var equipoSwitch in switches)
        {
            if (EstaExcluidoDeLaReglaSfp(equipoSwitch)) continue;

            // sfpAnterior conserva el valor previo para dejar el cambio trazable.
            var sfpAnterior = equipoSwitch.CantidadPuertosSfp;
            if (sfpAnterior != CantidadSfpPorSwitch)
            {
                equipoSwitch.CantidadPuertosSfp = CantidadSfpPorSwitch;
                UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoSwitch.Identificador, equipoSwitch.Nombre,
                    UsuarioImportacion, "Cantidad de puertos SFP",
                    sfpAnterior?.ToString(CultureInfo.InvariantCulture), CantidadSfpPorSwitch.ToString(CultureInfo.InvariantCulture));
            }

            if (SwitchesDe24Puertos.Contains(equipoSwitch.Nombre) && equipoSwitch.CantidadPuertosRj45 is null)
            {
                equipoSwitch.CantidadPuertosRj45 = 24;
                UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoSwitch.Identificador, equipoSwitch.Nombre,
                    UsuarioImportacion, "Cantidad de puertos RJ45", null, "24");
            }

            // interfacesDelSwitch son los puertos ya registrados de esta ficha.
            var interfacesDelSwitch = todasLasInterfaces
                .Where(interfaz => interfaz.IdentificadorEquipo == equipoSwitch.Identificador).ToList();

            if (interfacesDelSwitch.Count == 0)
            {
                // Sin puertos documentados se materializan RJ45 y las 4 SFP provisionales, como en el alta manual.
                var interfacesGeneradas = GeneradorInterfacesSwitch.CrearInterfacesFaltantes(equipoSwitch, interfacesDelSwitch);
                if (interfacesGeneradas.Count > 0)
                {
                    await contextoInventario.InterfacesEquipo.AddRangeAsync(interfacesGeneradas, cancelacion);
                    todasLasInterfaces.AddRange(interfacesGeneradas);
                    UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoSwitch.Identificador, equipoSwitch.Nombre,
                        UsuarioImportacion, "Puertos generados por capacidad V4", null,
                        GeneradorInterfacesSwitch.DescribirInterfacesCreadas(interfacesGeneradas));
                }
                continue;
            }

            // sfpSobrantes son filas SFP cuyo número local no está entre 1-4 ni 25-28 y que no tienen enlace ni relación.
            var sfpSobrantes = interfacesDelSwitch
                .Where(interfaz => NumeroSfpLocal(interfaz) is int numero && numero is < 1 or > 4
                    && !puertosConRegistro.Contains(interfaz.Identificador))
                .ToList();
            if (sfpSobrantes.Count > 0)
            {
                contextoInventario.InterfacesEquipo.RemoveRange(sfpSobrantes);
                foreach (var sobrante in sfpSobrantes) todasLasInterfaces.Remove(sobrante);
                interfacesDelSwitch = interfacesDelSwitch.Except(sfpSobrantes).ToList();
                UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoSwitch.Identificador, equipoSwitch.Nombre,
                    UsuarioImportacion, "Filas SFP sobrantes retiradas",
                    string.Join(", ", sfpSobrantes.Select(sobrante => sobrante.EtiquetaPuerto)),
                    $"{CantidadSfpPorSwitch} jaulas SFP por switch según DIAGRAMA RED V4.0");
            }

            // numerosPresentes marca qué jaulas SFP (1-4, con 25-28 equivalentes a 1-4) ya existen.
            var numerosPresentes = interfacesDelSwitch
                .Select(NumeroSfpLocal).Where(numero => numero is >= 1 and <= 4).Select(numero => numero!.Value).ToHashSet();
            for (var numeroJaula = 1; numeroJaula <= CantidadSfpPorSwitch; numeroJaula++)
            {
                if (numerosPresentes.Contains(numeroJaula)) continue;
                // jaulaNueva es una posición declarada: no es una conexión ni confirma un transceptor instalado.
                var jaulaNueva = new InterfazEquipo
                {
                    IdentificadorEquipo = equipoSwitch.Identificador,
                    EtiquetaPuerto = $"SFP {numeroJaula}",
                    TipoInterfaz = "SFP",
                    EstadoDatos = "A verificar",
                    EstadoUso = "Desconocido",
                    Observaciones = "Jaula SFP agregada por la regla de DIAGRAMA RED V4.0 (cuatro SFP por switch, salvo Dell S4148U-ON); transceptor no confirmado."
                };
                await contextoInventario.InterfacesEquipo.AddAsync(jaulaNueva, cancelacion);
                todasLasInterfaces.Add(jaulaNueva);
                UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoSwitch.Identificador, equipoSwitch.Nombre,
                    UsuarioImportacion, "Jaula SFP incorporada", null, jaulaNueva.EtiquetaPuerto);
            }
        }

        await contextoInventario.SaveChangesAsync(cancelacion);
    }

    /// <summary>
    /// Indica los switches a los que no se aplica la regla de cuatro SFP: Dell S4148U-ON (perfil propio),
    /// el Cisco (el V4 lo dibuja con 8 bocas) y el switch de 8 puertos de Inmuebles o el Intellinet.
    /// </summary>
    private static bool EstaExcluidoDeLaReglaSfp(EquipoRed equipo) =>
        equipo.Nombre.Contains("S4148", StringComparison.OrdinalIgnoreCase)
        || equipo.Modelo?.Contains("S4148", StringComparison.OrdinalIgnoreCase) == true
        || equipo.Nombre.Contains("Cisco", StringComparison.OrdinalIgnoreCase)
        || equipo.Nombre.Contains("Intellinet", StringComparison.OrdinalIgnoreCase)
        || equipo.Nombre.Equals("SWITCH DE 8 PUERTOS", StringComparison.OrdinalIgnoreCase)
        || equipo.Nombre.Contains("no administrable", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Devuelve el número de jaula SFP local (25-28 equivalen a 1-4) o nulo si la interfaz no es de tipo SFP.
    /// </summary>
    private static int? NumeroSfpLocal(InterfazEquipo interfaz)
    {
        if (!string.Equals(interfaz.TipoInterfaz, "SFP", StringComparison.OrdinalIgnoreCase)) return null;
        var coincidencia = Regex.Match(interfaz.EtiquetaPuerto.Trim(), @"(\d+)\s*$");
        if (!coincidencia.Success || !int.TryParse(coincidencia.Groups[1].Value, out var numero)) return null;
        return numero is >= 25 and <= 28 ? numero - 24 : numero;
    }

    /// <summary>
    /// Da de alta los equipos del esquema de internet del V4, sus interfaces y los enlaces dibujados.
    /// </summary>
    private static async Task IncorporarEsquemaInternetAsync(
        InventarioDbContext contextoInventario, List<EventoModificacion> cambiosAuditados, CancellationToken cancelacion)
    {
        // equiposPorNombre resuelve fichas ya existentes para no duplicarlas.
        var equiposPorNombre = (await contextoInventario.Equipos.ToListAsync(cancelacion))
            .GroupBy(equipo => equipo.Nombre.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var definicion in EquiposEsquema)
        {
            if (!equiposPorNombre.TryGetValue(definicion.Nombre, out var equipo))
            {
                // equipoNuevo documenta solo lo escrito en el V4; la vigencia queda «A verificar» porque no hay revisión física.
                equipo = new EquipoRed
                {
                    Nombre = definicion.Nombre,
                    Fabricante = definicion.Fabricante,
                    Modelo = definicion.Modelo,
                    IpAdministracion = definicion.Ip,
                    Area = "Rack",
                    TipoDispositivo = definicion.Tipo,
                    RolEnArea = definicion.Rol,
                    CantidadPuertosRj45 = definicion.PuertosRj45,
                    EstadoDatos = "A verificar",
                    EstadoAdministrativo = "Sin información",
                    FuenteDatos = "DIAGRAMA RED V4.0, ESQUEMA CONEXIÓN INTERNET",
                    Observaciones = definicion.Observaciones
                };
                await contextoInventario.Equipos.AddAsync(equipo, cancelacion);
                await contextoInventario.SaveChangesAsync(cancelacion);
                equiposPorNombre[definicion.Nombre] = equipo;
                UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipo.Identificador, equipo.Nombre, UsuarioImportacion,
                    "Alta de equipo", null, $"{definicion.Tipo}; {definicion.Observaciones}");
            }

            // interfacesActuales permite agregar solo las interfaces que falten.
            var interfacesActuales = await contextoInventario.InterfacesEquipo
                .Where(interfaz => interfaz.IdentificadorEquipo == equipo.Identificador).ToListAsync(cancelacion);

            if (definicion.PuertosRj45 is not null)
            {
                var interfacesGeneradas = GeneradorInterfacesSwitch.CrearInterfacesFaltantes(equipo, interfacesActuales);
                if (interfacesGeneradas.Count > 0)
                {
                    await contextoInventario.InterfacesEquipo.AddRangeAsync(interfacesGeneradas, cancelacion);
                    UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipo.Identificador, equipo.Nombre, UsuarioImportacion,
                        "Puertos generados por capacidad V4", null, GeneradorInterfacesSwitch.DescribirInterfacesCreadas(interfacesGeneradas));
                }
            }

            foreach (var etiqueta in definicion.Interfaces)
            {
                if (interfacesActuales.Any(interfaz => string.Equals(interfaz.EtiquetaPuerto.Trim(), etiqueta, StringComparison.OrdinalIgnoreCase)))
                    continue;
                // interfazNueva no afirma el tipo de conector: el V4 solo rotula la boca.
                await contextoInventario.InterfacesEquipo.AddAsync(new InterfazEquipo
                {
                    IdentificadorEquipo = equipo.Identificador,
                    EtiquetaPuerto = etiqueta,
                    TipoInterfaz = "No confirmado",
                    EstadoDatos = "A verificar",
                    EstadoUso = "Desconocido",
                    Observaciones = "Fuente DIAGRAMA RED V4.0, ESQUEMA CONEXIÓN INTERNET; conector no confirmado."
                }, cancelacion);
            }
        }

        await contextoInventario.SaveChangesAsync(cancelacion);

        foreach (var enlace in EnlacesEsquema)
        {
            if (!equiposPorNombre.TryGetValue(enlace.EquipoA, out var equipoA)
                || !equiposPorNombre.TryGetValue(enlace.EquipoB, out var equipoB))
                continue;

            var puertoA = await contextoInventario.InterfacesEquipo.FirstOrDefaultAsync(interfaz =>
                interfaz.IdentificadorEquipo == equipoA.Identificador && interfaz.EtiquetaPuerto == enlace.PuertoA, cancelacion);
            var puertoB = await contextoInventario.InterfacesEquipo.FirstOrDefaultAsync(interfaz =>
                interfaz.IdentificadorEquipo == equipoB.Identificador && interfaz.EtiquetaPuerto == enlace.PuertoB, cancelacion);
            if (puertoA is null || puertoB is null) continue;

            // enlaceEnPuertos detecta cualquier enlace completo ya guardado en alguno de los dos extremos.
            var enlaceEnPuertos = await contextoInventario.EnlacesFisicos.AnyAsync(existente =>
                existente.IdentificadorPuertoOrigen == puertoA.Identificador || existente.IdentificadorPuertoDestino == puertoA.Identificador
                || existente.IdentificadorPuertoOrigen == puertoB.Identificador || existente.IdentificadorPuertoDestino == puertoB.Identificador,
                cancelacion);
            if (enlaceEnPuertos) continue;

            // relacionesEnPuertos son las notas de un solo extremo guardadas antes para estos puertos.
            var relacionesEnPuertos = await contextoInventario.ConexionesDocumentadas.Where(relacion =>
                relacion.IdentificadorPuertoLocal == puertoA.Identificador || relacion.IdentificadorPuertoLocal == puertoB.Identificador)
                .ToListAsync(cancelacion);
            // Solo se reemplaza la nota parcial que nombra al mismo equipo; cualquier otra relación bloquea el enlace.
            var hayRelacionAjena = relacionesEnPuertos.Any(relacion =>
                enlace.RelacionParcialPorReemplazar is null
                || relacion.EquipoRemoto?.Contains(enlace.RelacionParcialPorReemplazar, StringComparison.OrdinalIgnoreCase) != true);
            if (hayRelacionAjena) continue;

            foreach (var relacionReemplazada in relacionesEnPuertos)
            {
                contextoInventario.ConexionesDocumentadas.Remove(relacionReemplazada);
                UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoA.Identificador, equipoA.Nombre, UsuarioImportacion,
                    "Relación parcial reemplazada por enlace completo",
                    $"{relacionReemplazada.EquipoRemoto} {relacionReemplazada.IpRemota}".Trim(),
                    $"{enlace.PuertoA} ↔ {enlace.EquipoB} {enlace.PuertoB}");
            }

            await contextoInventario.EnlacesFisicos.AddAsync(new EnlaceFisico
            {
                IdentificadorPuertoOrigen = puertoA.Identificador,
                IdentificadorPuertoDestino = puertoB.Identificador,
                Medio = "No confirmado",
                EstadoDatos = enlace.Estado,
                Fuente = $"DIAGRAMA RED V4.0 · ESQUEMA CONEXIÓN INTERNET · {enlace.Referencia}",
                Observaciones = "Medio y transceptores no confirmados."
            }, cancelacion);
            UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipoA.Identificador, equipoA.Nombre, UsuarioImportacion,
                "Enlace del esquema de internet", null,
                $"{enlace.PuertoA} ↔ {enlace.EquipoB} {enlace.PuertoB} · {enlace.Estado}");
        }

        await contextoInventario.SaveChangesAsync(cancelacion);
    }

    /// <summary>
    /// Completa MAC, fabricante, nombre e IP vacíos con el escaneo de la LAN. Empareja primero por IP y luego por
    /// nombre de host; nunca sobrescribe un dato ya cargado y deja una nota cuando hay discrepancias.
    /// </summary>
    private static async Task IncorporarEscaneoLanAsync(
        InventarioDbContext contextoInventario, List<EventoModificacion> cambiosAuditados, CancellationToken cancelacion)
    {
        // filasEscaneo son los hosts detectados; cambiar el catálogo cambia qué datos se completan.
        var filasEscaneo = CatalogoEscaneoLan.ObtenerFilas();
        var equipos = await contextoInventario.Equipos.ToListAsync(cancelacion);

        // equiposPorIp y equiposPorNombre indexan el inventario; puede haber varias fichas con la misma IP (p. ej. 192.168.1.19).
        var equiposPorIp = new Dictionary<string, List<EquipoRed>>();
        var equiposPorNombre = new Dictionary<string, List<EquipoRed>>();
        foreach (var equipoExistente in equipos)
        {
            if (!string.IsNullOrWhiteSpace(equipoExistente.IpAdministracion))
                AgregarAIndice(equiposPorIp, equipoExistente.IpAdministracion.Trim(), equipoExistente);
            AgregarAIndice(equiposPorNombre, NormalizarNombreHost(equipoExistente.Nombre), equipoExistente);
        }

        // altasDelEscaneo guarda los hosts nuevos hasta que SQLite les asigne identificador para auditarlos.
        var altasDelEscaneo = new List<EquipoRed>();

        foreach (var fila in filasEscaneo)
        {
            var nombreHost = string.IsNullOrWhiteSpace(fila.Nombre) ? null : fila.Nombre.Trim();
            var macEscaneo = fila.Mac is not null && PatronMac.IsMatch(fila.Mac) ? fila.Mac : null;
            var marcaNota = $"[Escaneo LAN {fila.Ip}]";

            // candidatos son las fichas que ya usan esta IP.
            equiposPorIp.TryGetValue(fila.Ip, out var candidatos);
            EquipoRed? equipo = null;
            if (candidatos is { Count: 1 })
            {
                equipo = candidatos[0];
            }
            else if (candidatos is { Count: > 1 })
            {
                // Con varias fichas se elige la que ya tiene esa MAC o ese nombre; si ninguna coincide no se asigna nada.
                equipo = candidatos.FirstOrDefault(c => macEscaneo is not null
                        && string.Equals(c.DireccionMac, macEscaneo, StringComparison.OrdinalIgnoreCase))
                    ?? candidatos.FirstOrDefault(c => nombreHost is not null
                        && NormalizarNombreHost(c.Nombre) == NormalizarNombreHost(nombreHost));
                if (equipo is null) continue;
            }

            // Un host puede tener una segunda IP: si su nombre ya existe en otra ficha se completa esa ficha.
            var emparejadoPorNombre = false;
            if (equipo is null && nombreHost is not null
                && equiposPorNombre.TryGetValue(NormalizarNombreHost(nombreHost), out var porNombre) && porNombre.Count == 1)
            {
                equipo = porNombre[0];
                emparejadoPorNombre = true;
            }

            if (equipo is null)
            {
                if (!CrearEquiposDetectadosSinDocumentar) continue;
                // equipoDetectado se da de alta «A verificar», sin tipo ni área, porque el escaneo no los acredita.
                var equipoDetectado = new EquipoRed
                {
                    Nombre = nombreHost ?? fila.Ip,
                    Fabricante = fila.Fabricante,
                    IpAdministracion = fila.Ip,
                    DireccionMac = macEscaneo,
                    TipoDispositivo = "No especificado",
                    EstadoDatos = "A verificar",
                    EstadoAdministrativo = "Sin información",
                    FuenteDatos = "Escaneo LAN (datos_ip.html)",
                    Observaciones = $"{marcaNota} Detectado en el escaneo y ausente del Visio y del PDF de ubicación; "
                        + (fila.Activo ? "activo" : "inactivo") + " al escanear."
                };
                altasDelEscaneo.Add(equipoDetectado);
                AgregarAIndice(equiposPorIp, fila.Ip, equipoDetectado);
                AgregarAIndice(equiposPorNombre, NormalizarNombreHost(equipoDetectado.Nombre), equipoDetectado);
                continue;
            }

            // El escáner nombra por IP lo que no resuelve: solo se renombra una ficha cuyo nombre es una IP.
            if (nombreHost is not null && PatronIp.IsMatch(equipo.Nombre.Trim())
                && !equiposPorNombre.ContainsKey(NormalizarNombreHost(nombreHost)))
            {
                var nombreAnterior = equipo.Nombre;
                equipo.Nombre = nombreHost;
                AgregarAIndice(equiposPorNombre, NormalizarNombreHost(nombreHost), equipo);
                UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipo.Identificador, equipo.Nombre, UsuarioImportacion,
                    "Nombre (escaneo LAN)", nombreAnterior, nombreHost);
            }

            // IP principal: solo se completa si la ficha no tiene ninguna y el host está en la red de usuarios 192.168.0.x.
            if (string.IsNullOrWhiteSpace(equipo.IpAdministracion) && fila.Ip.StartsWith("192.168.0.", StringComparison.Ordinal))
            {
                equipo.IpAdministracion = fila.Ip;
                AgregarAIndice(equiposPorIp, fila.Ip, equipo);
                UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipo.Identificador, equipo.Nombre, UsuarioImportacion,
                    "IP (escaneo LAN)", null, fila.Ip);
            }

            if (macEscaneo is not null && string.IsNullOrWhiteSpace(equipo.DireccionMac))
            {
                equipo.DireccionMac = macEscaneo;
                UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipo.Identificador, equipo.Nombre, UsuarioImportacion,
                    "Dirección MAC (escaneo LAN)", null, macEscaneo);
            }
            else if (macEscaneo is not null
                && !string.Equals(equipo.DireccionMac?.Trim(), macEscaneo, StringComparison.OrdinalIgnoreCase)
                && equipo.Observaciones?.Contains(marcaNota, StringComparison.Ordinal) != true)
            {
                equipo.Observaciones = AgregarNota(equipo.Observaciones,
                    $"{marcaNota} MAC distinta de la guardada: {macEscaneo}.");
            }

            if (!string.IsNullOrWhiteSpace(fila.Fabricante) && string.IsNullOrWhiteSpace(equipo.Fabricante))
            {
                equipo.Fabricante = fila.Fabricante;
                UtilidadesConexiones.AgregarCambio(cambiosAuditados, equipo.Identificador, equipo.Nombre, UsuarioImportacion,
                    "Fabricante (escaneo LAN)", null, fila.Fabricante);
            }

            // Notas que no cambian un campo pero evitan perder información del escaneo; la marca con la IP evita repetirlas.
            if (equipo.Observaciones?.Contains(marcaNota, StringComparison.Ordinal) != true)
            {
                var notas = new List<string>();
                if (emparejadoPorNombre && !string.Equals(equipo.IpAdministracion?.Trim(), fila.Ip, StringComparison.Ordinal))
                    notas.Add($"también responde en {fila.Ip} con la misma MAC");
                else if (nombreHost is not null && !PatronIp.IsMatch(equipo.Nombre.Trim())
                    && NormalizarNombreHost(nombreHost) != NormalizarNombreHost(equipo.Nombre))
                    notas.Add($"nombre de host {nombreHost}");
                if (!fila.Activo) notas.Add("inactivo al escanear");
                if (notas.Count > 0)
                    equipo.Observaciones = AgregarNota(equipo.Observaciones, $"{marcaNota} {string.Join("; ", notas)}.");
            }
        }

        if (altasDelEscaneo.Count > 0)
        {
            await contextoInventario.Equipos.AddRangeAsync(altasDelEscaneo, cancelacion);
        }

        await contextoInventario.SaveChangesAsync(cancelacion);

        foreach (var alta in altasDelEscaneo)
        {
            UtilidadesConexiones.AgregarCambio(cambiosAuditados, alta.Identificador, alta.Nombre, UsuarioImportacion,
                "Alta desde escaneo LAN", null, $"IP {alta.IpAdministracion}; MAC {alta.DireccionMac ?? "sin dato"}; {alta.Fabricante ?? "fabricante sin identificar"}");
        }
    }

    /// <summary>Agrega una ficha a un índice de listas, creando la lista si todavía no existe.</summary>
    private static void AgregarAIndice(Dictionary<string, List<EquipoRed>> indice, string clave, EquipoRed equipo)
    {
        if (!indice.TryGetValue(clave, out var lista))
        {
            lista = [];
            indice[clave] = lista;
        }

        if (!lista.Contains(equipo)) lista.Add(equipo);
    }

    /// <summary>Normaliza un nombre de host: sin marcas invisibles, sin el sufijo .catastro.lan y en minúsculas.</summary>
    private static string NormalizarNombreHost(string? nombre)
    {
        var limpio = (nombre ?? string.Empty).Replace("\u200e", string.Empty).Trim();
        if (limpio.EndsWith(".catastro.lan", StringComparison.OrdinalIgnoreCase)) limpio = limpio[..^".catastro.lan".Length];
        return limpio.ToLowerInvariant();
    }

    /// <summary>Agrega una nota al final de las observaciones sin perder el texto anterior.</summary>
    private static string AgregarNota(string? observaciones, string nota) =>
        string.IsNullOrWhiteSpace(observaciones) ? nota : $"{observaciones.TrimEnd()} {nota}";
}
