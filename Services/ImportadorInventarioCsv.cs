using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Services;

/// <summary>
/// Analiza CSV separado por punto y coma para preparar fichas sin escribir en SQLite.
/// </summary>
public static class ImportadorInventarioCsv
{
    /// <summary>Máximo de equipos por carga; cambiarlo ajusta el volumen permitido por archivo.</summary>
    public const int MaximoEquipos = 500;

    /// <summary>Máximo de puertos que una carga puede generar; cambiarlo limita la expansión de capacidades.</summary>
    public const int MaximoPuertos = 10000;

    // encabezadosCanonicos coincide con el archivo exportado desde Inventario; cambiar su orden cambia la plantilla CSV.
    private static readonly string[] encabezadosCanonicos =
    [
        "Equipo", "Fabricante", "Modelo", "Área", "Piso", "IP documentada", "MAC", "Rol", "Tipo",
        "Puertos RJ45", "Puertos SFP", "Puertos SFP+", "Puertos QSFP", "Puertos QSFP+", "Puertos QSFP28", "Vigencia"
    ];

    /// <summary>
    /// Genera una plantilla vacía compatible con la exportación de inventario.
    /// </summary>
    /// <returns>Encabezado CSV; cambiar nombres u orden modifica la estructura de la plantilla descargable.</returns>
    public static string CrearPlantilla()
    {
        // plantillaCsv usa exactamente las columnas admitidas por el exportador; cambiarla altera la futura lectura.
        return string.Join(';', encabezadosCanonicos) + "\r\n";
    }

    /// <summary>
    /// Valida encabezados, campos, cantidades y duplicados del archivo CSV sin persistir sus datos.
    /// </summary>
    /// <param name="contenidoCsv">Texto delimitado por punto y coma; cambiarlo altera las fichas candidatas y sus avisos.</param>
    /// <returns>Equipos, errores y avisos; corregir el texto de entrada puede cambiar el resultado.</returns>
    public static ResultadoImportacionCsv Analizar(string contenidoCsv)
    {
        // resultadoCsv reúne los hallazgos del análisis; cambiar sus listas cambia los datos aceptables.
        var resultadoCsv = new ResultadoImportacionCsv();
        // filasCsv conserva campos y registros ya separados respetando comillas.
        if (!LeerFilas(contenidoCsv, out var filasCsv, out var errorFormato))
        {
            resultadoCsv.Errores.Add(errorFormato);
            return resultadoCsv;
        }

        if (filasCsv.Count < 2)
        {
            resultadoCsv.Errores.Add("El CSV debe incluir una fila de encabezados y al menos un equipo.");
            return resultadoCsv;
        }

        // encabezadosLeidos normaliza espacios y mayúsculas para resolver columnas conocidas.
        var encabezadosLeidos = filasCsv[0].Select(NormalizarEncabezado).ToList();
        if (encabezadosLeidos.Distinct(StringComparer.OrdinalIgnoreCase).Count() != encabezadosLeidos.Count)
        {
            resultadoCsv.Errores.Add("Hay encabezados repetidos. Deja una sola columna por cada campo.");
            return resultadoCsv;
        }

        // posicionesCampos mapea cada columna por nombre para permitir reordenar la plantilla.
        var posicionesCampos = encabezadosLeidos
            .Select((encabezado, indice) => new { encabezado, indice })
            .ToDictionary(par => par.encabezado, par => par.indice, StringComparer.OrdinalIgnoreCase);
        foreach (var encabezadoRequerido in new[] { "equipo", "rol", "tipo", "vigencia" })
        {
            if (!posicionesCampos.ContainsKey(encabezadoRequerido))
            {
                resultadoCsv.Errores.Add($"Falta la columna obligatoria «{encabezadoRequerido}».");
            }
        }

        if (resultadoCsv.Errores.Count > 0)
        {
            return resultadoCsv;
        }

        if (filasCsv.Count - 1 > MaximoEquipos)
        {
            resultadoCsv.Errores.Add($"La carga admite hasta {MaximoEquipos} equipos por archivo.");
            return resultadoCsv;
        }

        // nombresVistos detecta fichas repetidas que no se podrían distinguir sin una revisión manual.
        var nombresVistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // ipsVistas agrupa direcciones repetidas como aviso sin asumir que sean un error de la red.
        var ipsVistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // puertosProvisionales suma las capacidades solicitadas para controlar el tamaño final del inventario.
        var puertosProvisionales = 0;

        for (var indiceFila = 1; indiceFila < filasCsv.Count; indiceFila++)
        {
            // filaCsv contiene las celdas originales de la línea lógica actual.
            var filaCsv = filasCsv[indiceFila];
            var numeroFila = indiceFila + 1;
            if (filaCsv.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (filaCsv.Count != encabezadosLeidos.Count)
            {
                resultadoCsv.Errores.Add($"Fila {numeroFila}: contiene {filaCsv.Count} campos y se esperaban {encabezadosLeidos.Count}.");
                continue;
            }

            // ObtenerCampo devuelve un texto opcional de la columna nombrada sin depender de su posición.
            string ObtenerCampo(string nombre)
            {
                return posicionesCampos.TryGetValue(NormalizarEncabezado(nombre), out var indice)
                    ? filaCsv[indice].Trim()
                    : string.Empty;
            }

            // formularioEquipo permite aplicar las mismas restricciones de longitud y campos requeridos del alta manual.
            var formularioEquipo = new EditarEquipoViewModel
            {
                Nombre = ObtenerCampo("Equipo"),
                Fabricante = ObtenerCampo("Fabricante"),
                Modelo = ObtenerCampo("Modelo"),
                IpAdministracion = ObtenerCampo("IP documentada"),
                DireccionMac = ObtenerCampo("MAC"),
                Area = ObtenerCampo("Área"),
                Piso = ObtenerCampo("Piso"),
                RolEnArea = ObtenerCampo("Rol"),
                TipoDispositivo = ObtenerCampo("Tipo"),
                EstadoDatos = ObtenerCampo("Vigencia")
            };

            // erroresFila conserva cuántas validaciones ya se emitieron para omitir una ficha inválida.
            var cantidadErroresAntes = resultadoCsv.Errores.Count;
            var capacidades = new[]
            {
                LeerCantidad(ObtenerCampo("Puertos RJ45"), "Puertos RJ45", numeroFila, resultadoCsv.Errores),
                LeerCantidad(ObtenerCampo("Puertos SFP"), "Puertos SFP", numeroFila, resultadoCsv.Errores),
                LeerCantidad(ObtenerCampo("Puertos SFP+"), "Puertos SFP+", numeroFila, resultadoCsv.Errores),
                LeerCantidad(ObtenerCampo("Puertos QSFP"), "Puertos QSFP", numeroFila, resultadoCsv.Errores),
                LeerCantidad(ObtenerCampo("Puertos QSFP+"), "Puertos QSFP+", numeroFila, resultadoCsv.Errores),
                LeerCantidad(ObtenerCampo("Puertos QSFP28"), "Puertos QSFP28", numeroFila, resultadoCsv.Errores)
            };

            // contextoValidacion aplica Required, StringLength y Range usados en el alta individual.
            var contextoValidacion = new ValidationContext(formularioEquipo);
            var erroresValidacion = new List<ValidationResult>();
            Validator.TryValidateObject(formularioEquipo, contextoValidacion, erroresValidacion, true);
            resultadoCsv.Errores.AddRange(erroresValidacion.Select(error =>
                $"Fila {numeroFila}: {error.ErrorMessage ?? "dato inválido"}"));

            if (capacidades.Sum(cantidad => cantidad ?? 0) > MaximoPuertos)
            {
                resultadoCsv.Errores.Add($"Fila {numeroFila}: supera el máximo total de {MaximoPuertos} puertos por carga.");
            }

            if (!string.IsNullOrWhiteSpace(formularioEquipo.Nombre)
                && !nombresVistos.Add(formularioEquipo.Nombre.Trim()))
            {
                resultadoCsv.Errores.Add($"Fila {numeroFila}: el equipo «{formularioEquipo.Nombre}» está repetido en el archivo.");
            }

            if (!string.IsNullOrWhiteSpace(formularioEquipo.IpAdministracion)
                && !ipsVistas.Add(formularioEquipo.IpAdministracion.Trim()))
            {
                resultadoCsv.Avisos.Add($"IP repetida en el archivo: {formularioEquipo.IpAdministracion.Trim()}. Verifica si corresponde a un duplicado real.");
            }

            if (resultadoCsv.Errores.Count > cantidadErroresAntes)
            {
                continue;
            }

            // equipoImportado contiene únicamente campos documentales expresos en el CSV.
            resultadoCsv.Equipos.Add(new EquipoRed
            {
                Nombre = formularioEquipo.Nombre.Trim(),
                Fabricante = NormalizarTexto(formularioEquipo.Fabricante),
                Modelo = NormalizarTexto(formularioEquipo.Modelo),
                IpAdministracion = NormalizarTexto(formularioEquipo.IpAdministracion),
                DireccionMac = NormalizarTexto(formularioEquipo.DireccionMac),
                Area = NormalizarTexto(formularioEquipo.Area),
                Piso = NormalizarTexto(formularioEquipo.Piso),
                RolEnArea = formularioEquipo.RolEnArea.Trim(),
                TipoDispositivo = formularioEquipo.TipoDispositivo.Trim(),
                CantidadPuertosRj45 = capacidades[0],
                CantidadPuertosSfp = capacidades[1],
                CantidadPuertosSfpPlus = capacidades[2],
                CantidadPuertosQsfp = capacidades[3],
                CantidadPuertosQsfpPlus = capacidades[4],
                CantidadPuertosQsfp28 = capacidades[5],
                EstadoDatos = formularioEquipo.EstadoDatos.Trim()
            });

            puertosProvisionales += capacidades.Sum(cantidad => cantidad ?? 0);
        }

        if (puertosProvisionales > MaximoPuertos)
        {
            resultadoCsv.Errores.Add($"El total supera el máximo permitido de {MaximoPuertos} puertos.");
        }

        if (resultadoCsv.Equipos.Count == 0 && resultadoCsv.Errores.Count == 0)
        {
            resultadoCsv.Errores.Add("No se encontraron filas de equipos para importar.");
        }

        return resultadoCsv;
    }

    /// <summary>
    /// Separa filas CSV y conserva separadores y saltos de línea contenidos entre comillas.
    /// </summary>
    /// <param name="contenidoCsv">Texto delimitado; cambiarlo cambia las celdas resultantes.</param>
    /// <param name="filasCsv">Lista de salida; cambiarla modifica todas las filas analizadas.</param>
    /// <param name="errorFormato">Descripción de sintaxis incorrecta; cambiarla altera el aviso para corregir el archivo.</param>
    /// <returns>Verdadero si el CSV tiene comillas balanceadas.</returns>
    private static bool LeerFilas(string contenidoCsv, out List<List<string>> filasCsv, out string errorFormato)
    {
        // filasCsv acumula los registros resultantes de leer el flujo CSV.
        filasCsv = [];
        // errorFormato informa una comilla abierta o un encabezado mal formado.
        errorFormato = string.Empty;
        // filaActual y campoActual acumulan celdas sin perder el contenido entrecomillado.
        var filaActual = new List<string>();
        var campoActual = new StringBuilder();
        // dentroDeComillas distingue separadores literales de los separadores de columnas.
        var dentroDeComillas = false;

        for (var indiceCaracter = 0; indiceCaracter < contenidoCsv.Length; indiceCaracter++)
        {
            // caracterActual procesa un símbolo de la celda actual.
            var caracterActual = contenidoCsv[indiceCaracter];
            if (caracterActual == '"')
            {
                if (dentroDeComillas && indiceCaracter + 1 < contenidoCsv.Length && contenidoCsv[indiceCaracter + 1] == '"')
                {
                    campoActual.Append('"');
                    indiceCaracter++;
                }
                else if (dentroDeComillas)
                {
                    dentroDeComillas = false;
                }
                else if (campoActual.Length == 0)
                {
                    dentroDeComillas = true;
                }
                else
                {
                    errorFormato = $"Hay una comilla fuera de lugar cerca del carácter {indiceCaracter + 1}.";
                    return false;
                }

                continue;
            }

            if (!dentroDeComillas && caracterActual == ';')
            {
                filaActual.Add(campoActual.ToString());
                campoActual.Clear();
                continue;
            }

            if (!dentroDeComillas && caracterActual is '\r' or '\n')
            {
                if (caracterActual == '\r' && indiceCaracter + 1 < contenidoCsv.Length && contenidoCsv[indiceCaracter + 1] == '\n')
                {
                    indiceCaracter++;
                }

                filaActual.Add(campoActual.ToString());
                campoActual.Clear();
                AgregarFilaNoVacia(filasCsv, filaActual);
                filaActual = [];
                continue;
            }

            campoActual.Append(caracterActual);
        }

        if (dentroDeComillas)
        {
            errorFormato = "El CSV termina con una celda entrecomillada sin cerrar.";
            return false;
        }

        if (campoActual.Length > 0 || filaActual.Count > 0)
        {
            filaActual.Add(campoActual.ToString());
            AgregarFilaNoVacia(filasCsv, filaActual);
        }

        return true;
    }

    /// <summary>
    /// Agrega filas con contenido y omite líneas vacías.
    /// </summary>
    /// <param name="filasCsv">Colección destino; cambiarla modifica los registros analizados.</param>
    /// <param name="filaActual">Celdas de la línea; cambiarla cambia el registro agregado.</param>
    private static void AgregarFilaNoVacia(List<List<string>> filasCsv, List<string> filaActual)
    {
        if (filaActual.Any(celda => !string.IsNullOrWhiteSpace(celda)))
        {
            filasCsv.Add(filaActual);
        }
    }

    /// <summary>
    /// Convierte una capacidad opcional y agrega un error cuando el valor no es válido.
    /// </summary>
    /// <param name="texto">Cantidad textual; cambiarlo cambia el número de interfaces que se generan.</param>
    /// <param name="nombreCampo">Nombre de columna; cambiarlo modifica el mensaje de error.</param>
    /// <param name="numeroFila">Fila del CSV; cambiarlo cambia la ubicación del aviso.</param>
    /// <param name="errores">Errores acumulados; cambiar la lista altera el resultado del análisis.</param>
    /// <returns>Número válido o nulo si la celda está vacía.</returns>
    private static int? LeerCantidad(string texto, string nombreCampo, int numeroFila, List<string> errores)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        if (!int.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out var cantidad) || cantidad is < 0 or > 512)
        {
            errores.Add($"Fila {numeroFila}: «{nombreCampo}» debe ser un número entero entre 0 y 512.");
            return null;
        }

        return cantidad;
    }

    /// <summary>
    /// Normaliza el nombre de una columna para permitir tildes, espacios y mayúsculas equivalentes.
    /// </summary>
    /// <param name="encabezado">Texto del encabezado; cambiarlo cambia su clave de búsqueda.</param>
    /// <returns>Clave minúscula sin tildes.</returns>
    private static string NormalizarEncabezado(string encabezado)
    {
        // encabezadoDescompuesto separa marcas diacríticas para aceptar variaciones de tildes.
        var encabezadoDescompuesto = encabezado.Trim().Normalize(NormalizationForm.FormD);
        // encabezadoNormalizado conserva solo caracteres base y espacios interiores.
        var encabezadoNormalizado = new string(encabezadoDescompuesto
            .Where(caracter => CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        return encabezadoNormalizado.ToLowerInvariant();
    }

    /// <summary>
    /// Convierte textos vacíos en nulos sin alterar los datos no vacíos del usuario.
    /// </summary>
    /// <param name="texto">Valor del CSV; cambiarlo cambia el valor guardado.</param>
    /// <returns>Nulo para celdas vacías o el texto recortado.</returns>
    private static string? NormalizarTexto(string texto)
    {
        return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    }
}
