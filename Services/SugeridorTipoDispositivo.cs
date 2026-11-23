using System.Text.RegularExpressions;

namespace ProyectoConexionRed.Web.Services;

/// <summary>
/// Propone un tipo de dispositivo a partir del nombre, el modelo y el fabricante. Es solo una sugerencia:
/// el técnico decide si la aplica. Usa únicamente los tipos que ya existen en el formulario de equipos.
/// </summary>
public static class SugeridorTipoDispositivo
{
    /// <summary>Tipo propuesto y la razón visible para el técnico.</summary>
    /// <param name="Tipo">Valor del selector de tipo de dispositivo.</param>
    /// <param name="Motivo">Texto que explica por qué se propone ese tipo.</param>
    public sealed record Sugerencia(string Tipo, string Motivo);

    // reglasTexto se evalúan en orden sobre el nombre y el modelo; la primera que coincide gana.
    private static readonly (Regex Patron, string Tipo, string Motivo)[] ReglasTexto =
    [
        (new Regex(@"\b(switch|swtich)\b", Opciones), "Switch", "el nombre o el modelo menciona un switch"),
        (new Regex(@"pf-?sense|firewall|fortigate|sophos", Opciones), "Firewall", "el nombre o el modelo menciona un firewall"),
        (new Regex(@"router|m[oó]dem|mikrotik", Opciones), "Router", "el nombre o el modelo menciona un router o módem"),
        (new Regex(@"\bups\b", Opciones), "UPS", "el nombre o el modelo menciona una UPS"),
        (new Regex(@"impresora|printer|plotter|laserjet|multifunci", Opciones), "Impresora", "el nombre o el modelo menciona una impresora"),
        (new Regex(@"idrac|poweredge|servidor|server|storage|backup", Opciones), "Servidor", "el nombre o el modelo menciona un servidor"),
        (new Regex(@"reloj|biom[eé]tric", Opciones), "Reloj", "el nombre o el modelo menciona un reloj o lector biométrico"),
        (new Regex(@"c[aá]mara|camera|\bcctv\b|\bnvr\b|\bdvr\b", Opciones), "Cámara IP", "el nombre o el modelo menciona una cámara"),
        (new Regex(@"access ?point|punto de acceso|\bap[-_ ]?\d|wifi|wi-fi", Opciones), "Punto de acceso", "el nombre o el modelo menciona un punto de acceso Wi-Fi"),
        (new Regex(@"\bnas\b|almacenamiento|\bsan\b", Opciones), "Almacenamiento", "el nombre o el modelo menciona almacenamiento")
    ];

    // reglasFabricante se evalúan sobre el fabricante informado por el escaneo o escrito en la ficha.
    private static readonly (Regex Patron, string Tipo, string Motivo)[] ReglasFabricante =
    [
        (new Regex(@"american power|\bapc\b|eaton", Opciones), "UPS", "el fabricante fabrica UPS"),
        (new Regex(@"epson|brother|kyocera|lexmark|ricoh|xerox|canon|konica", Opciones), "Impresora", "el fabricante fabrica impresoras"),
        (new Regex(@"hikvision|dahua|axis comm|hanwha|vivotek", Opciones), "Cámara IP", "el fabricante fabrica cámaras IP"),
        (new Regex(@"ubiquiti|aruba instant|ruckus|cambium", Opciones), "Punto de acceso", "el fabricante fabrica puntos de acceso"),
        (new Regex(@"synology|qnap|netapp|western digital", Opciones), "Almacenamiento", "el fabricante fabrica equipos de almacenamiento"),
        (new Regex(@"vmware", Opciones), "Servidor", "el fabricante es VMware, es decir una máquina virtual")
    ];

    // PatronPuesto reconoce el formato área + número usado en los puestos, por ejemplo Sistemas23.
    private static readonly Regex PatronPuesto = new(@"^[A-Za-zÁÉÍÓÚáéíóúñÑ]+\d{1,3}$", Opciones);

    private const RegexOptions Opciones = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    /// <summary>
    /// Devuelve el tipo más probable o nulo si no hay indicios suficientes.
    /// </summary>
    /// <param name="nombre">Nombre de host o etiqueta; cambiarlo cambia las reglas que coinciden.</param>
    /// <param name="fabricante">Fabricante conocido; cambiarlo puede activar las reglas por fabricante.</param>
    /// <param name="modelo">Modelo escrito en la ficha.</param>
    /// <returns>Sugerencia con motivo, o nulo si nada coincide.</returns>
    public static Sugerencia? Sugerir(string? nombre, string? fabricante, string? modelo)
    {
        var texto = $"{nombre} {modelo}".Trim();
        foreach (var (patron, tipo, motivo) in ReglasTexto)
        {
            if (patron.IsMatch(texto)) return new Sugerencia(tipo, motivo);
        }

        foreach (var (patron, tipo, motivo) in ReglasFabricante)
        {
            if (!string.IsNullOrWhiteSpace(fabricante) && patron.IsMatch(fabricante)) return new Sugerencia(tipo, motivo);
        }

        if (!string.IsNullOrWhiteSpace(nombre) && PatronPuesto.IsMatch(nombre.Trim()))
            return new Sugerencia("Puesto de usuario", "el nombre sigue el formato de los puestos (área y número)");

        return null;
    }
}
