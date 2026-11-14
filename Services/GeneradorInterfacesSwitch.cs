using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Services;

/// <summary>
/// Materializa conectores declarados por capacidad en interfaces documentales editables.
/// </summary>
public static class GeneradorInterfacesSwitch
{
    /// <summary>
    /// Crea los puertos numerados que falten según las cantidades guardadas en el equipo.
    /// </summary>
    /// <param name="equipo">Switch con capacidades; cambiar sus cantidades cambia qué interfaces se proponen.</param>
    /// <param name="interfacesExistentes">Puertos ya registrados; cambiar la lista puede crear o evitar posiciones.</param>
    /// <returns>Interfaces nuevas sin enlaces, todas en estado desconocido y a verificar.</returns>
    public static List<InterfazEquipo> CrearInterfacesFaltantes(
        EquipoRed equipo,
        IReadOnlyCollection<InterfazEquipo> interfacesExistentes)
    {
        if (!string.Equals(equipo.TipoDispositivo, "Switch", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        // gruposCapacidad asocia el tipo del conector con su prefijo visual y cantidad declarada.
        var gruposCapacidad = new[]
        {
            (Tipo: "RJ45", Prefijo: "", Cantidad: equipo.CantidadPuertosRj45),
            (Tipo: "SFP", Prefijo: "SFP ", Cantidad: equipo.CantidadPuertosSfp),
            (Tipo: "SFP+", Prefijo: "SFP+ ", Cantidad: equipo.CantidadPuertosSfpPlus),
            (Tipo: "QSFP", Prefijo: "QSFP ", Cantidad: equipo.CantidadPuertosQsfp),
            (Tipo: "QSFP+", Prefijo: "QSFP+ ", Cantidad: equipo.CantidadPuertosQsfpPlus),
            (Tipo: "QSFP28", Prefijo: "QSFP28 ", Cantidad: equipo.CantidadPuertosQsfp28)
        };

        // interfacesNuevas acumula posiciones sin reutilizar ni modificar puertos existentes.
        var interfacesNuevas = new List<InterfazEquipo>();
        // grupoCapacidad recorre cada formato de conector con una cantidad definida.
        foreach (var grupoCapacidad in gruposCapacidad)
        {
            if (grupoCapacidad.Cantidad is null)
            {
                continue;
            }

            // interfacesDelGrupo cuenta posiciones ya guardadas para no superar la capacidad indicada.
            var interfacesDelGrupo = interfacesExistentes.Concat(interfacesNuevas)
                .Where(interfaz => interfaz.IdentificadorEquipo == equipo.Identificador
                    && CoincideTipoCapacidad(interfaz.TipoInterfaz, grupoCapacidad.Tipo))
                .ToList();
            // cantidadFaltante conserva los puertos existentes aunque sus etiquetas físicas sean distintas al orden genérico.
            var cantidadFaltante = Math.Max(0, grupoCapacidad.Cantidad.Value - interfacesDelGrupo.Count);
            // numeroPuerto propone las etiquetas secuenciales faltantes sin superar el total definido.
            for (var numeroPuerto = 1; numeroPuerto <= grupoCapacidad.Cantidad.Value && cantidadFaltante > 0; numeroPuerto++)
            {
                // etiquetaEsperada sigue las etiquetas que usa el frontal visual de switches.
                var etiquetaEsperada = grupoCapacidad.Prefijo + numeroPuerto;
                // yaExiste compara equipo, tipo y etiqueta para que ejecutar el generador más de una vez sea seguro.
                var yaExiste = interfacesExistentes.Concat(interfacesNuevas).Any(interfaz =>
                    interfaz.IdentificadorEquipo == equipo.Identificador
                    && CoincideTipoCapacidad(interfaz.TipoInterfaz, grupoCapacidad.Tipo)
                    && string.Equals(interfaz.EtiquetaPuerto.Trim(), etiquetaEsperada, StringComparison.OrdinalIgnoreCase));
                if (yaExiste)
                {
                    continue;
                }

                // interfazNueva representa un conector declarado, no una conexión ni una medición de disponibilidad.
                interfacesNuevas.Add(new InterfazEquipo
                {
                    IdentificadorEquipo = equipo.Identificador,
                    EtiquetaPuerto = etiquetaEsperada,
                    TipoInterfaz = grupoCapacidad.Tipo,
                    EstadoDatos = "A verificar",
                    EstadoUso = "Desconocido",
                    Observaciones = "Etiqueta y posición iniciales generadas desde la capacidad declarada; verificar con el frontal físico antes de asignar equipos."
                });
                cantidadFaltante--;
            }
        }

        return interfacesNuevas;
    }

    /// <summary>
    /// Cuenta RJ45 y RJ45 con PoE como conectores de cobre de la misma capacidad.
    /// </summary>
    /// <param name="tipoInterfaz">Tipo guardado; cambiarlo puede hacer que el puerto compute dentro de otra capacidad.</param>
    /// <param name="tipoCapacidad">Capacidad declarada; cambiarla selecciona otra familia de conectores.</param>
    /// <returns>Verdadero si el tipo de interfaz pertenece a la capacidad consultada.</returns>
    private static bool CoincideTipoCapacidad(string tipoInterfaz, string tipoCapacidad)
    {
        // tipoNormalizado evita diferencias de espacios y mayúsculas en las clases manuales.
        var tipoNormalizado = tipoInterfaz.Trim();
        if (string.Equals(tipoCapacidad, "RJ45", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(tipoNormalizado, "RJ45", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tipoNormalizado, "RJ45 + PoE", StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(tipoNormalizado, tipoCapacidad, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resume cuántos puertos nuevos se crearon por tipo para un único evento de historial.
    /// </summary>
    /// <param name="interfacesNuevas">Puertos recién generados; cambiar la lista modifica el resumen auditado.</param>
    /// <returns>Resumen compacto; una lista vacía devuelve texto vacío.</returns>
    public static string DescribirInterfacesCreadas(IReadOnlyCollection<InterfazEquipo> interfacesNuevas)
    {
        // resumenPorTipo agrupa cantidades para no escribir un evento de historial por cada conector.
        var resumenPorTipo = interfacesNuevas
            .GroupBy(interfaz => interfaz.TipoInterfaz, StringComparer.OrdinalIgnoreCase)
            .OrderBy(grupo => grupo.Key, StringComparer.OrdinalIgnoreCase)
            .Select(grupo => $"{grupo.Key}: {grupo.Count()}");
        return string.Join(", ", resumenPorTipo);
    }
}
