using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Controllers;
using ProyectoConexionRed.Web.Models;
using ProyectoConexionRed.Web.Services;

namespace ProyectoConexionRed.Web.Data;

/// <summary>
/// Incorpora DIAGRAMA RED V5.0: renombra los dos switches que el V5 identifica, agrega el switch no administrable de
/// Puerta Agrimensura y, una sola vez por base, elimina las conexiones que no respetan el V5.
/// Solo escribe en SQLite; nunca consulta ni cambia la red real.
/// </summary>
internal static class ActualizacionV5
{
    // UsuarioCarga es el autor que se muestra en el historial.
    private const string UsuarioCarga = "Carga documental DIAGRAMA RED V5.0";

    // CodigoConciliacion marca la base como ya conciliada: la eliminación de conexiones se hace una sola vez, para que
    // los enlaces que el técnico dibuje después en el editor no se borren en cada inicio.
    private const string CodigoConciliacion = "conciliacion-visio-v5-2026-11";

    // NombreSwitchNoAdministrable es el rótulo que usa la tabla del V5 en la relación del puerto 18 de Puerta Agrimensura.
    private const string NombreSwitchNoAdministrable = "Switch no administrable";

    // PatronSoloLetrasYNumeros permite comparar rótulos sin espacios ni signos.
    private static readonly Regex PatronSoloLetrasYNumeros = new(@"[^\p{L}\p{N}]", RegexOptions.Compiled);

    /// <summary>
    /// Aplica la actualización V5 dentro de una transacción; si algo falla no se guarda nada parcial.
    /// </summary>
    /// <param name="contextoInventario">Contexto SQLite; cambiarlo selecciona qué inventario recibe la actualización.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla revierte los cambios pendientes.</param>
    public static async Task AplicarAsync(InventarioDbContext contextoInventario, CancellationToken cancelacion)
    {
        var cambiosAuditados = new List<EventoModificacion>();
        await using var transaccion = await contextoInventario.Database.BeginTransactionAsync(cancelacion);

        await RenombrarSwitchesAsync(contextoInventario, cambiosAuditados, cancelacion);
        await IncorporarSwitchNoAdministrableAsync(contextoInventario, cambiosAuditados, cancelacion);
        await ConciliarConexionesAsync(contextoInventario, cambiosAuditados, cancelacion);

        if (cambiosAuditados.Count > 0)
        {
            await contextoInventario.EventosModificacion.AddRangeAsync(cambiosAuditados, cancelacion);
            await contextoInventario.SaveChangesAsync(cancelacion);
        }

        await transaccion.CommitAsync(cancelacion);
    }

    /// <summary>
    /// El V5 corrige el nombre de dos switches: el de la IP 192.168.1.40 es NOTIFICADORES (no «RD») y el de la
    /// 192.168.1.17 es REGIMEN. Se renombran sus fichas solo si el nombre nuevo no existe todavía.
    /// </summary>
    private static async Task RenombrarSwitchesAsync(
        InventarioDbContext contexto, List<EventoModificacion> cambios, CancellationToken cancelacion)
    {
        // renombres relaciona nombre anterior, nombre nuevo y área que corresponde al switch.
        var renombres = new[]
        {
            (Anterior: "Regularización Dominial", Nuevo: "Notificadores", Area: "Notificadores"),
            (Anterior: "Switch de área sin identificar (2)", Nuevo: "Switch Régimen", Area: "Régimen")
        };

        foreach (var renombre in renombres)
        {
            var equipo = await contexto.Equipos.FirstOrDefaultAsync(e => e.Nombre == renombre.Anterior, cancelacion);
            if (equipo is null || await contexto.Equipos.AnyAsync(e => e.Nombre == renombre.Nuevo, cancelacion)) continue;

            UtilidadesConexiones.AgregarCambio(cambios, equipo.Identificador, renombre.Nuevo, UsuarioCarga,
                "Nombre (Visio V5)", renombre.Anterior, renombre.Nuevo);
            equipo.Nombre = renombre.Nuevo;
            if (string.IsNullOrWhiteSpace(equipo.Area) || equipo.Area == renombre.Anterior)
            {
                UtilidadesConexiones.AgregarCambio(cambios, equipo.Identificador, equipo.Nombre, UsuarioCarga,
                    "Área (Visio V5)", equipo.Area, renombre.Area);
                equipo.Area = renombre.Area;
            }
        }

        await contexto.SaveChangesAsync(cancelacion);
    }

    /// <summary>
    /// Da de alta el switch no administrable de cuatro puertos que el V5 dibuja detrás del puerto 18 de Puerta Agrimensura.
    /// Sus puertos y relaciones los importa el catálogo de puertos.
    /// </summary>
    private static async Task IncorporarSwitchNoAdministrableAsync(
        InventarioDbContext contexto, List<EventoModificacion> cambios, CancellationToken cancelacion)
    {
        if (await contexto.Equipos.AnyAsync(e => e.Nombre == NombreSwitchNoAdministrable, cancelacion)) return;

        // equipoNuevo no tiene IP ni MAC porque el V5 no los documenta; al no tener IP se trata como no administrable.
        var equipoNuevo = new EquipoRed
        {
            Nombre = NombreSwitchNoAdministrable,
            Area = "Puerta Agrimensura",
            TipoDispositivo = "Switch",
            RolEnArea = "Switch no administrable",
            EsAdministrable = false,
            CantidadPuertosRj45 = 4,
            EstadoDatos = "A verificar",
            EstadoAdministrativo = "Sin información",
            FuenteDatos = "DIAGRAMA RED V5.0, PUERTA AGRIMENSURA (Excel20)",
            Observaciones = "Switch de cuatro puertos sin administración conectado al puerto 18 de Puerta Agrimensura. Marca, modelo y puerto de enlace no documentados."
        };
        await contexto.Equipos.AddAsync(equipoNuevo, cancelacion);
        await contexto.SaveChangesAsync(cancelacion);
        UtilidadesConexiones.AgregarCambio(cambios, equipoNuevo.Identificador, equipoNuevo.Nombre, UsuarioCarga,
            "Alta de equipo", null, "Switch no administrable de Puerta Agrimensura (V5.0)");
    }

    /// <summary>
    /// Elimina, una sola vez por base, las relaciones documentadas y los enlaces que contradicen lo que el V5 dice de
    /// cada puerto con tabla propia. Las relaciones que el V5 sí define se vuelven a crear después con el importador.
    /// </summary>
    private static async Task ConciliarConexionesAsync(
        InventarioDbContext contexto, List<EventoModificacion> cambios, CancellationToken cancelacion)
    {
        if (await contexto.MantenimientosEjecutados.AnyAsync(m => m.Codigo == CodigoConciliacion, cancelacion)) return;

        var equipos = await contexto.Equipos.AsNoTracking().ToListAsync(cancelacion);
        var interfaces = await contexto.InterfacesEquipo.AsNoTracking().ToListAsync(cancelacion);
        var equipoPorId = equipos.ToDictionary(equipo => equipo.Identificador);
        var interfazPorId = interfaces.ToDictionary(interfaz => interfaz.Identificador);

        // filaPorPuerto indexa lo que el V5 dice de cada puerto con tabla: solo esos puertos se concilian.
        var filaPorPuerto = new Dictionary<(string Equipo, string Puerto), FilaPuertoAreaV3>();
        foreach (var fila in CatalogoPuertosAreasV3.ObtenerFilas())
        {
            filaPorPuerto[(Normalizar(fila.EquipoLocal), Normalizar(fila.EtiquetaPuerto))] = fila;
        }

        bool TieneFila(int idPuerto, out FilaPuertoAreaV3? fila, out EquipoRed? equipoDuenoDelPuerto)
        {
            fila = null;
            equipoDuenoDelPuerto = null;
            if (!interfazPorId.TryGetValue(idPuerto, out var puerto) || !equipoPorId.TryGetValue(puerto.IdentificadorEquipo, out var dueno)) return false;
            equipoDuenoDelPuerto = dueno;
            return filaPorPuerto.TryGetValue((Normalizar(dueno.Nombre), Normalizar(puerto.EtiquetaPuerto)), out fila);
        }

        // ---------- Relaciones documentadas ----------
        var relaciones = await contexto.ConexionesDocumentadas.ToListAsync(cancelacion);
        foreach (var relacion in relaciones)
        {
            if (!TieneFila(relacion.IdentificadorPuertoLocal, out var fila, out var duenoRelacion) || fila is null || duenoRelacion is null) continue;
            if (RelacionCoincide(relacion, fila, equipos)) continue;

            contexto.ConexionesDocumentadas.Remove(relacion);
            UtilidadesConexiones.AgregarCambio(cambios, duenoRelacion.Identificador, duenoRelacion.Nombre, UsuarioCarga,
                "Relación eliminada: no coincide con el Visio V5",
                $"{relacion.EquipoRemoto} {relacion.IpRemota}".Trim(), $"V5: {DescribirDestino(fila)}");
        }

        // ---------- Enlaces físicos ----------
        var enlaces = await contexto.EnlacesFisicos.ToListAsync(cancelacion);
        foreach (var enlace in enlaces)
        {
            if (!interfazPorId.TryGetValue(enlace.IdentificadorPuertoOrigen, out var puertoOrigen)
                || !interfazPorId.TryGetValue(enlace.IdentificadorPuertoDestino, out var puertoDestino)
                || !equipoPorId.TryGetValue(puertoOrigen.IdentificadorEquipo, out var equipoOrigen)
                || !equipoPorId.TryGetValue(puertoDestino.IdentificadorEquipo, out var equipoDestino)) continue;

            // Cada extremo con tabla en el V5 dice con qué equipo debería estar conectado.
            string? motivo = null;
            if (TieneFila(puertoOrigen.Identificador, out var filaOrigen, out _) && filaOrigen is not null && !EnlaceCoincide(filaOrigen, equipoDestino, equipos))
                motivo = $"{equipoOrigen.Nombre} · {puertoOrigen.EtiquetaPuerto} debería ser {DescribirDestino(filaOrigen)}";
            else if (TieneFila(puertoDestino.Identificador, out var filaDestino, out _) && filaDestino is not null && !EnlaceCoincide(filaDestino, equipoOrigen, equipos))
                motivo = $"{equipoDestino.Nombre} · {puertoDestino.EtiquetaPuerto} debería ser {DescribirDestino(filaDestino)}";
            if (motivo is null) continue;

            contexto.EnlacesFisicos.Remove(enlace);
            UtilidadesConexiones.AgregarCambio(cambios, equipoOrigen.Identificador, equipoOrigen.Nombre, UsuarioCarga,
                "Enlace eliminado: no coincide con el Visio V5",
                $"{equipoOrigen.Nombre} · {puertoOrigen.EtiquetaPuerto} ↔ {equipoDestino.Nombre} · {puertoDestino.EtiquetaPuerto}", $"V5: {motivo}");
        }

        contexto.MantenimientosEjecutados.Add(new MantenimientoEjecutado
        {
            Codigo = CodigoConciliacion,
            FechaUtc = DateTime.UtcNow.ToString("O")
        });
        await contexto.SaveChangesAsync(cancelacion);
    }

    /// <summary>
    /// Indica si una relación guardada coincide con la fila del V5: mismo destino o ambos resuelven al mismo equipo.
    /// </summary>
    private static bool RelacionCoincide(ConexionDocumentada relacion, FilaPuertoAreaV3 fila, IReadOnlyList<EquipoRed> equipos)
    {
        if (EsSinDestino(fila.Destino)) return false;
        var mismoRotulo = Normalizar(relacion.EquipoRemoto) == Normalizar(fila.Destino);
        var ipRelacion = relacion.IpRemota?.Trim() ?? string.Empty;
        var ipFila = fila.IpRemota?.Trim() ?? string.Empty;
        // Sin IP guardada no hay contradicción (el importador la completa); una IP guardada que el V5 ya no tiene o que
        // es distinta de la del V5 es un dato viejo.
        var mismaIp = ipRelacion.Length == 0 || string.Equals(ipRelacion, ipFila, StringComparison.OrdinalIgnoreCase);
        if (mismoRotulo && mismaIp) return true;

        // Rótulos escritos distinto pueden ser el mismo equipo: se comparan las fichas a las que apuntan.
        var guardado = TopologiaController.ResolverEquipoRelacionado(equipos, relacion.EquipoRemoto, relacion.IpRemota);
        var esperado = TopologiaController.ResolverEquipoRelacionado(equipos, fila.Destino, fila.IpRemota);
        return guardado is not null && esperado is not null && guardado.Identificador == esperado.Identificador;
    }

    /// <summary>
    /// Indica si un enlace es compatible con la fila del V5 de uno de sus puertos; si el destino del V5 no se puede
    /// asociar a una ficha no se decide nada y el enlace se conserva.
    /// </summary>
    private static bool EnlaceCoincide(FilaPuertoAreaV3 fila, EquipoRed otroExtremo, IReadOnlyList<EquipoRed> equipos)
    {
        if (EsSinDestino(fila.Destino)) return false;
        var esperado = TopologiaController.ResolverEquipoRelacionado(equipos, fila.Destino, fila.IpRemota);
        return esperado is null || esperado.Identificador == otroExtremo.Identificador;
    }

    /// <summary>Destinos del Excel que indican un puerto sin equipo remoto.</summary>
    private static bool EsSinDestino(string destino)
    {
        var limpio = destino.Trim();
        return limpio.Equals("LIBRE", StringComparison.OrdinalIgnoreCase) || limpio.Equals("DESCONOCIDO", StringComparison.OrdinalIgnoreCase)
            || limpio.Equals("SIN SFP", StringComparison.OrdinalIgnoreCase) || limpio.Equals("SFP LIBRE", StringComparison.OrdinalIgnoreCase);
    }

    private static string DescribirDestino(FilaPuertoAreaV3 fila) =>
        EsSinDestino(fila.Destino) ? fila.Destino.Trim() : $"{fila.Destino.Trim()} {fila.IpRemota}".Trim();

    /// <summary>Normaliza un rótulo para compararlo: sin espacios ni signos y en minúsculas.</summary>
    private static string Normalizar(string? texto) => PatronSoloLetrasYNumeros.Replace(texto ?? string.Empty, string.Empty).ToLowerInvariant();
}
