using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Services;

/// <summary>
/// Detecta IP, MAC y nombres repetidos entre fichas y los separa en pendientes y justificados.
/// Solo lee el inventario.
/// </summary>
public static class ConflictosService
{
    // PatronIpv4 descarta valores como «Desconocido» que no son una dirección.
    private static readonly Regex PatronIpv4 = new(@"^(\d{1,3}\.){3}\d{1,3}$", RegexOptions.Compiled);

    /// <summary>Devuelve la clave normalizada de una MAC (12 dígitos en mayúsculas con dos puntos), o nulo si no es válida o es todo ceros.</summary>
    /// <param name="mac">MAC escrita con cualquier separador.</param>
    public static string? NormalizarMac(string? mac)
    {
        if (string.IsNullOrWhiteSpace(mac)) return null;
        var digitos = new string(mac.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
        if (digitos.Length != 12 || digitos.All(caracter => caracter == '0')) return null;
        return string.Join(':', Enumerable.Range(0, 6).Select(indice => digitos.Substring(indice * 2, 2)));
    }

    /// <summary>Clave normalizada de un conflicto según su tipo; las IP y los nombres se recortan y los nombres van en minúsculas.</summary>
    /// <param name="tipo">IP, MAC o Nombre.</param>
    /// <param name="valor">Valor tal como figura en la ficha.</param>
    public static string? Clave(string tipo, string? valor) => tipo switch
    {
        ConflictosViewModel.TipoIp => valor is not null && PatronIpv4.IsMatch(valor.Trim()) ? valor.Trim() : null,
        ConflictosViewModel.TipoMac => NormalizarMac(valor),
        ConflictosViewModel.TipoNombre => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim().ToLowerInvariant(),
        _ => null
    };

    /// <summary>
    /// Arma la lista de conflictos y las IP libres que se ofrecen para corregirlos.
    /// </summary>
    /// <param name="contexto">Inventario consultado; cambiarlo analiza otra sede.</param>
    /// <param name="cancelacion">Señal de cancelación.</param>
    /// <returns>Conflictos pendientes y justificados, ordenados por tipo y valor.</returns>
    public static async Task<ConflictosViewModel> ObtenerAsync(InventarioDbContext contexto, CancellationToken cancelacion)
    {
        var equipos = await contexto.Equipos.AsNoTracking().ToListAsync(cancelacion);
        var justificaciones = (await contexto.ConflictosJustificados.AsNoTracking().ToListAsync(cancelacion))
            .ToDictionary(justificacion => (justificacion.Tipo, justificacion.Clave));
        var resultado = new ConflictosViewModel();

        foreach (var tipo in new[] { ConflictosViewModel.TipoIp, ConflictosViewModel.TipoMac, ConflictosViewModel.TipoNombre })
        {
            var grupos = equipos
                .Select(equipo => (Equipo: equipo, Clave: Clave(tipo, tipo switch
                {
                    ConflictosViewModel.TipoIp => equipo.IpAdministracion,
                    ConflictosViewModel.TipoMac => equipo.DireccionMac,
                    _ => equipo.Nombre
                })))
                .Where(par => par.Clave is not null)
                .GroupBy(par => par.Clave!, StringComparer.Ordinal)
                .Where(grupo => grupo.Count() > 1)
                .OrderBy(grupo => grupo.Key, StringComparer.Ordinal);

            foreach (var grupo in grupos)
            {
                var conflicto = new ConflictoDetectado
                {
                    Tipo = tipo,
                    Clave = grupo.Key,
                    Equipos = grupo.Select(par => new EquipoEnConflicto
                    {
                        Identificador = par.Equipo.Identificador,
                        Nombre = par.Equipo.Nombre,
                        Tipo = par.Equipo.TipoDispositivo,
                        Area = par.Equipo.Area,
                        Ip = par.Equipo.IpAdministracion,
                        Mac = par.Equipo.DireccionMac
                    }).OrderBy(equipo => equipo.Nombre, StringComparer.OrdinalIgnoreCase).ToList()
                };
                if (justificaciones.TryGetValue((tipo, grupo.Key), out var justificacion))
                {
                    conflicto.Justificacion = justificacion;
                    resultado.Justificados.Add(conflicto);
                }
                else
                {
                    resultado.Pendientes.Add(conflicto);
                }
            }
        }

        var libres = await UtilidadesConexiones.ObtenerIpsLibresAsync(contexto, null, cancelacion);
        resultado.IpsLibres = libres.Select(opcion => opcion.Value).Where(valor => !string.IsNullOrEmpty(valor)).Take(250).Select(valor => valor!).ToList();
        return resultado;
    }
}
