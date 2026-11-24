using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Controllers;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Services;

/// <summary>
/// Calcula los indicadores del estado de la red documentada: IP, administración, STP, conexiones y puertos.
/// Solo lee el inventario.
/// </summary>
public static class ResumenRedService
{
    // PrefijosRed son las redes que se resumen por separado; el nombre visible termina en «x».
    private static readonly (string Prefijo, string Nombre)[] PrefijosRed = [("192.168.0.", "192.168.0.x"), ("192.168.1.", "192.168.1.x")];

    /// <summary>
    /// Arma el resumen completo desde el inventario.
    /// </summary>
    /// <param name="contexto">Inventario consultado; cambiarlo resume otra sede.</param>
    /// <param name="cancelacion">Señal de cancelación.</param>
    /// <returns>Indicadores listos para mostrar.</returns>
    public static async Task<ResumenRed> CalcularAsync(InventarioDbContext contexto, CancellationToken cancelacion)
    {
        var equipos = await contexto.Equipos.AsNoTracking().ToListAsync(cancelacion);
        var interfaces = await contexto.InterfacesEquipo.AsNoTracking().ToListAsync(cancelacion);
        var enlaces = await contexto.EnlacesFisicos.AsNoTracking().ToListAsync(cancelacion);
        var relaciones = await contexto.ConexionesDocumentadas.AsNoTracking().ToListAsync(cancelacion);
        var direcciones = await contexto.DireccionesIp.AsNoTracking().ToListAsync(cancelacion);
        var resumen = new ResumenRed();

        // ---------- Direcciones IP ----------
        foreach (var (prefijo, nombre) in PrefijosRed)
        {
            var filas = direcciones.Where(direccion => direccion.Direccion.StartsWith(prefijo, StringComparison.Ordinal)).ToList();
            resumen.Redes.Add(new ResumenIpRed
            {
                Nombre = nombre,
                Total = filas.Count,
                EnUso = filas.Count(fila => fila.Estado == SincronizadorIps.Asignada),
                Reservadas = filas.Count(fila => fila.Estado == SincronizadorIps.Reservada),
                EnConflicto = filas.Count(fila => fila.Estado == SincronizadorIps.EnConflicto),
                Libres = filas.Count(fila => fila.Estado == SincronizadorIps.Libre)
            });
        }

        resumen.IpFueraDeRango = direcciones.Count(direccion =>
            !PrefijosRed.Any(red => direccion.Direccion.StartsWith(red.Prefijo, StringComparison.Ordinal))
            && direccion.Estado is SincronizadorIps.Asignada or SincronizadorIps.EnConflicto);
        // Las IP compartidas que el técnico justificó no cuentan como pendientes.
        var ipsJustificadas = (await contexto.ConflictosJustificados.AsNoTracking()
                .Where(justificacion => justificacion.Tipo == "IP").Select(justificacion => justificacion.Clave).ToListAsync(cancelacion))
            .ToHashSet(StringComparer.Ordinal);
        resumen.IpConflictosJustificados = direcciones.Count(direccion =>
            direccion.Estado == SincronizadorIps.EnConflicto && ipsJustificadas.Contains(direccion.Direccion));
        resumen.IpEnConflicto = direcciones
            .Where(direccion => direccion.Estado == SincronizadorIps.EnConflicto && !ipsJustificadas.Contains(direccion.Direccion))
            .OrderBy(direccion => direccion.Direccion, StringComparer.Ordinal)
            .Select(direccion => $"{direccion.Direccion}: {direccion.Observaciones}").ToList();

        // ---------- Switches ----------
        var switches = equipos.Where(equipo => string.Equals(equipo.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase)).ToList();
        resumen.TotalSwitches = switches.Count;
        resumen.SwitchesAdministrables = switches.Count(equipo => equipo.EsAdministrable == true);
        resumen.SwitchesNoAdministrables = switches.Count(equipo => equipo.EsAdministrable == false);
        resumen.SwitchesSinConfirmar = switches.Count(equipo => equipo.EsAdministrable is null);
        resumen.EquiposSinIp = equipos.Count(equipo => string.IsNullOrWhiteSpace(equipo.IpAdministracion));
        resumen.EquiposSinMac = equipos.Count(equipo => string.IsNullOrWhiteSpace(equipo.DireccionMac));

        // ---------- Enlaces y STP ----------
        var equipoPorPuerto = interfaces.ToDictionary(interfaz => interfaz.Identificador, interfaz => interfaz.IdentificadorEquipo);
        resumen.EnlacesCompletos = enlaces.Count;
        resumen.EnlacesConfirmados = enlaces.Count(enlace => enlace.EstadoDatos == "Confirmado");
        resumen.EnlacesPorRevisar = enlaces.Count - resumen.EnlacesConfirmados;
        resumen.EnlacesStpDocumentado = enlaces.Count(enlace => enlace.EstadoStp != "No documentado");
        resumen.EnlacesStpSinDocumentar = enlaces.Count - resumen.EnlacesStpDocumentado;

        foreach (var equipoSwitch in switches)
        {
            // propios son los enlaces completos que terminan en este switch.
            var propios = enlaces.Where(enlace =>
                (equipoPorPuerto.TryGetValue(enlace.IdentificadorPuertoOrigen, out var origen) && origen == equipoSwitch.Identificador)
                || (equipoPorPuerto.TryGetValue(enlace.IdentificadorPuertoDestino, out var destino) && destino == equipoSwitch.Identificador)).ToList();
            var sinDocumentar = propios.Count(enlace => enlace.EstadoStp == "No documentado");
            if (propios.Count == 0) resumen.SwitchesSinEnlacesCompletos++;
            else if (sinDocumentar == 0) resumen.SwitchesStpDocumentado++;
            else if (sinDocumentar == propios.Count) resumen.SwitchesStpSinDocumentar++;
            else resumen.SwitchesStpParcial++;
            if (sinDocumentar > 0)
            {
                resumen.SwitchesPendientesStp.Add(new SwitchStpResumen { Nombre = equipoSwitch.Nombre, Enlaces = propios.Count, SinDocumentar = sinDocumentar });
            }
        }

        resumen.SwitchesPendientesStp = resumen.SwitchesPendientesStp
            .OrderByDescending(item => item.SinDocumentar).ThenBy(item => item.Nombre, StringComparer.OrdinalIgnoreCase).Take(12).ToList();

        // ---------- Relaciones parciales ----------
        var equipoPorId = equipos.ToDictionary(equipo => equipo.Identificador);
        var parcialesResueltas = new List<(int Local, int Remoto)>();
        foreach (var relacion in relaciones)
        {
            if (!equipoPorPuerto.TryGetValue(relacion.IdentificadorPuertoLocal, out var idLocal)) continue;
            var remoto = TopologiaController.ResolverEquipoRelacionado(equipos, relacion.EquipoRemoto, relacion.IpRemota);
            if (remoto is null) { resumen.RelacionesSinEquipo++; continue; }
            if (remoto.Identificador != idLocal) parcialesResueltas.Add((idLocal, remoto.Identificador));
        }

        resumen.RelacionesParciales = relaciones.Count;

        // ---------- Ciclos: enlaces completos primero y, entre switches, una relación parcial por par sin enlace completo ----------
        var padre = new Dictionary<int, int>();
        int Raiz(int nodo)
        {
            if (!padre.TryGetValue(nodo, out _)) { padre[nodo] = nodo; return nodo; }
            while (padre[nodo] != nodo) { padre[nodo] = padre[padre[nodo]]; nodo = padre[nodo]; }
            return nodo;
        }

        var paresConEnlaceCompleto = new HashSet<(int, int)>();
        foreach (var enlace in enlaces.OrderBy(enlace => enlace.Identificador))
        {
            if (!equipoPorPuerto.TryGetValue(enlace.IdentificadorPuertoOrigen, out var a)
                || !equipoPorPuerto.TryGetValue(enlace.IdentificadorPuertoDestino, out var b)) continue;
            paresConEnlaceCompleto.Add((Math.Min(a, b), Math.Max(a, b)));
            var raizA = Raiz(a);
            var raizB = Raiz(b);
            if (raizA != raizB) { padre[raizA] = raizB; continue; }
            if (enlace.EstadoStp == "No documentado") resumen.CiclosSinDocumentar++;
            else resumen.CiclosDocumentados++;
        }

        var paresParciales = new HashSet<(int, int)>();
        foreach (var (local, remoto) in parcialesResueltas)
        {
            var par = (Math.Min(local, remoto), Math.Max(local, remoto));
            if (paresConEnlaceCompleto.Contains(par) || !paresParciales.Add(par)) continue;
            if (!equipoPorId.TryGetValue(local, out var equipoLocal) || !equipoPorId.TryGetValue(remoto, out var equipoRemoto)) continue;
            if (!string.Equals(equipoLocal.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(equipoRemoto.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase)) continue;
            var raizLocal = Raiz(local);
            var raizRemoto = Raiz(remoto);
            if (raizLocal != raizRemoto) padre[raizLocal] = raizRemoto;
            else resumen.CiclosEnRelacionesParciales++;
        }

        // ---------- Puertos ----------
        var puertosEnlazados = enlaces.SelectMany(enlace => new[] { enlace.IdentificadorPuertoOrigen, enlace.IdentificadorPuertoDestino }).ToHashSet();
        var puertosConRelacion = relaciones.Select(relacion => relacion.IdentificadorPuertoLocal).ToHashSet();
        resumen.PuertosTotal = interfaces.Count;
        resumen.PuertosConEnlace = interfaces.Count(interfaz => puertosEnlazados.Contains(interfaz.Identificador));
        resumen.PuertosConRelacionParcial = interfaces.Count(interfaz =>
            !puertosEnlazados.Contains(interfaz.Identificador) && puertosConRelacion.Contains(interfaz.Identificador));
        resumen.PuertosSinDocumentar = resumen.PuertosTotal - resumen.PuertosConEnlace - resumen.PuertosConRelacionParcial;
        return resumen;
    }
}
