using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Models;
using ProyectoConexionRed.Web.Services;

namespace ProyectoConexionRed.Web.Data;

/// <summary>
/// Completa los datos nuevos de la Fase 1b al iniciar: administrabilidad de los switches y tabla de IP.
/// Es idempotente y solo escribe en SQLite.
/// </summary>
internal static class CargaFase1b
{
    // UsuarioCarga es el autor que se muestra en el historial.
    private const string UsuarioCarga = "Carga Fase 1b";

    // PatronIpv4 decide si una ficha tiene IP: «Desconocido» o vacío cuentan como sin IP.
    private static readonly Regex PatronIpv4 = new(@"^(\d{1,3}\.){3}\d{1,3}$", RegexOptions.Compiled);

    /// <summary>
    /// Marca cada switch sin dato como administrable si tiene IP y como no administrable si no la tiene
    /// (criterio confirmado por el usuario); nunca cambia un valor ya cargado. Después sincroniza la tabla de IP.
    /// </summary>
    /// <param name="contexto">Inventario destino; cambiarlo aplica la carga a otra sede.</param>
    /// <param name="cancelacion">Señal de cancelación.</param>
    public static async Task AplicarAsync(InventarioDbContext contexto, CancellationToken cancelacion)
    {
        var switchesSinDato = await contexto.Equipos
            .Where(equipo => equipo.TipoDispositivo == "Switch" && equipo.EsAdministrable == null)
            .ToListAsync(cancelacion);
        if (switchesSinDato.Count > 0)
        {
            var cambios = new List<EventoModificacion>();
            foreach (var equipoSwitch in switchesSinDato)
            {
                // tieneIp es la regla: con IP se puede administrar; sin IP se trata como no administrable.
                var tieneIp = !string.IsNullOrWhiteSpace(equipoSwitch.IpAdministracion)
                    && PatronIpv4.IsMatch(equipoSwitch.IpAdministracion.Trim());
                equipoSwitch.EsAdministrable = tieneIp;
                UtilidadesConexiones.AgregarCambio(cambios, equipoSwitch.Identificador, equipoSwitch.Nombre, UsuarioCarga,
                    "Switch administrable", "Sin confirmar", tieneIp ? "Sí (tiene IP)" : "No (sin IP)");
            }

            await contexto.EventosModificacion.AddRangeAsync(cambios, cancelacion);
            await contexto.SaveChangesAsync(cancelacion);
        }

        await SincronizadorIps.SincronizarAsync(contexto, cancelacion);
    }
}
