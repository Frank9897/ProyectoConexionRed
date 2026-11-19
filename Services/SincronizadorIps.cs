using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Services;

/// <summary>
/// Mantiene la tabla de direcciones IP alineada con las fichas de equipos y las relaciones documentadas.
/// Solo escribe en SQLite; nunca consulta ni cambia la red real.
/// </summary>
public static class SincronizadorIps
{
    /// <summary>La IP no la usa ningún equipo ni está reservada.</summary>
    public const string Libre = "Libre";

    /// <summary>La IP se apartó a mano o la menciona una relación documentada sin equipo propio.</summary>
    public const string Reservada = "Reservada";

    /// <summary>Exactamente un equipo usa la IP.</summary>
    public const string Asignada = "Asignada";

    /// <summary>Dos o más equipos declaran la misma IP.</summary>
    public const string EnConflicto = "En conflicto";

    // PatronIpv4 descarta valores como «Desconocido» que no son una dirección.
    private static readonly Regex PatronIpv4 = new(@"^(\d{1,3}\.){3}\d{1,3}$", RegexOptions.Compiled);

    /// <summary>
    /// Crea las IP de 192.168.0.1-254 y 192.168.1.1-254 y actualiza el estado de todas según los equipos.
    /// Las reservas manuales se conservan; una reserva cuya IP pasa a usar un equipo queda como Asignada.
    /// </summary>
    /// <param name="contexto">Inventario que se sincroniza; cambiarlo sincroniza otra sede.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la sincronización.</param>
    public static async Task SincronizarAsync(InventarioDbContext contexto, CancellationToken cancelacion)
    {
        var filas = await contexto.DireccionesIp.ToListAsync(cancelacion);
        var porDireccion = filas.ToDictionary(fila => fila.Direccion, StringComparer.OrdinalIgnoreCase);
        var ahora = DateTime.UtcNow;

        // Alta del rango habitual: una fila Libre por cada host utilizable.
        foreach (var segundoOcteto in new[] { 0, 1 })
        {
            for (var ultimoOcteto = 1; ultimoOcteto <= 254; ultimoOcteto++)
            {
                var direccion = $"192.168.{segundoOcteto}.{ultimoOcteto}";
                if (porDireccion.ContainsKey(direccion)) continue;
                var nueva = new DireccionIp { Direccion = direccion, Estado = Libre, FechaActualizacionUtc = ahora };
                contexto.DireccionesIp.Add(nueva);
                porDireccion[direccion] = nueva;
            }
        }

        // usoPorIp agrupa los equipos por la IP que declaran; solo cuentan direcciones IPv4 válidas.
        var equipos = await contexto.Equipos.AsNoTracking()
            .Where(equipo => equipo.IpAdministracion != null)
            .Select(equipo => new { equipo.Identificador, equipo.Nombre, equipo.IpAdministracion })
            .ToListAsync(cancelacion);
        var usoPorIp = equipos
            .Select(equipo => new { equipo.Identificador, equipo.Nombre, Ip = equipo.IpAdministracion!.Trim() })
            .Where(equipo => PatronIpv4.IsMatch(equipo.Ip))
            .GroupBy(equipo => equipo.Ip, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList(), StringComparer.OrdinalIgnoreCase);

        // referenciadas son IP que solo aparecen como destino en relaciones documentadas.
        var referenciadas = (await contexto.ConexionesDocumentadas.AsNoTracking()
                .Where(relacion => relacion.IpRemota != null)
                .Select(relacion => relacion.IpRemota!)
                .ToListAsync(cancelacion))
            .Select(ip => ip.Trim())
            .Where(ip => PatronIpv4.IsMatch(ip))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // IP usadas fuera del rango habitual (por ejemplo 10.0.1.1) también se registran.
        foreach (var direccion in usoPorIp.Keys.Concat(referenciadas))
        {
            if (porDireccion.ContainsKey(direccion)) continue;
            var nueva = new DireccionIp { Direccion = direccion, Estado = Libre, FechaActualizacionUtc = ahora };
            contexto.DireccionesIp.Add(nueva);
            porDireccion[direccion] = nueva;
        }

        foreach (var fila in porDireccion.Values)
        {
            string estado;
            int? idEquipo = null;
            string? observaciones = fila.Observaciones;

            if (usoPorIp.TryGetValue(fila.Direccion, out var usuarios))
            {
                if (usuarios.Count == 1)
                {
                    estado = Asignada;
                    idEquipo = usuarios[0].Identificador;
                    observaciones = fila.Estado == EnConflicto ? null : fila.Observaciones;
                }
                else
                {
                    estado = EnConflicto;
                    observaciones = "Compartida por: " + string.Join(", ", usuarios.Select(usuario => usuario.Nombre));
                }
            }
            else if (fila.Estado is Asignada or EnConflicto)
            {
                // Dejó de usarse: vuelve a Libre y pierde las notas automáticas.
                estado = Libre;
                observaciones = null;
            }
            else
            {
                estado = fila.Estado;
                if (estado == Libre && referenciadas.Contains(fila.Direccion))
                {
                    estado = Reservada;
                    observaciones = "Mencionada en una relación documentada sin equipo propio.";
                }
            }

            // Solo se toca la fila si algo cambió, para no generar escrituras ni fechas falsas.
            if (fila.Estado != estado || fila.IdentificadorEquipo != idEquipo || fila.Observaciones != observaciones)
            {
                fila.Estado = estado;
                fila.IdentificadorEquipo = idEquipo;
                fila.Observaciones = observaciones;
                fila.FechaActualizacionUtc = ahora;
            }
        }

        if (contexto.ChangeTracker.HasChanges())
        {
            await contexto.SaveChangesAsync(cancelacion);
        }
    }
}
