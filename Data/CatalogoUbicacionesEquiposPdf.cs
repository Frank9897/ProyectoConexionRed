namespace ProyectoConexionRed.Web.Data;

/// <summary>
/// Conserva registros tipados de puestos y ubicaciones transcritos del PDF de octubre de 2026.
/// Estos datos se importan a SQLite al iniciar la aplicación y no son una base paralela.
/// </summary>
internal static class CatalogoUbicacionesEquiposPdf
{
    /// <summary>
    /// Devuelve puestos con octetos, páginas y ubicación documentada.
    /// </summary>
    /// <returns>Registros de origen; modificar una fila cambia el contenido importado a SQLite.</returns>
    public static List<FilaUbicacionEquipo> ObtenerFilas()
    {
        // filasUbicacion conserva el catálogo tipado del PDF; modificar sus filas cambia la carga idempotente.
        var filasUbicacion = new List<FilaUbicacionEquipo>
        {
            new()
            {
                Nombre = "Admin02",
                OctetosPdf = [71],
                Paginas = [4],
                Area = "Gestión, Administración y Régimen",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Admin03",
                OctetosPdf = [193],
                Paginas = [4],
                Area = "Gestión, Administración y Régimen",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Admin05",
                OctetosPdf = [78],
                Paginas = [4],
                Area = "Gestión, Administración y Régimen",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Agrimensura02",
                OctetosPdf = [36],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura04",
                OctetosPdf = [38],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura05",
                OctetosPdf = [147],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura06",
                OctetosPdf = [171],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura07",
                OctetosPdf = [40],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura10",
                OctetosPdf = [61],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura13",
                OctetosPdf = [47],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura14",
                OctetosPdf = [162],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura15",
                OctetosPdf = [48],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura16",
                OctetosPdf = [163],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura17",
                OctetosPdf = [50],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura19",
                OctetosPdf = [52],
                Paginas = [6],
                Area = "Agrimensura - Sector Gráfico",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura20",
                OctetosPdf = [74],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura21",
                OctetosPdf = [207],
                Paginas = [6],
                Area = "Agrimensura - Sector Gráfico",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura22",
                OctetosPdf = [55],
                Paginas = [6],
                Area = "Agrimensura - Sector Gráfico",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura23",
                OctetosPdf = [37],
                Paginas = [6],
                Area = "Agrimensura - Sector Gráfico",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura24",
                OctetosPdf = [110],
                Paginas = [6],
                Area = "Agrimensura - Sector Gráfico",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura27",
                OctetosPdf = [59],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura28",
                OctetosPdf = [59],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura29",
                OctetosPdf = [195],
                Paginas = [6],
                Area = "Agrimensura - Sector Gráfico",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura30",
                OctetosPdf = [62],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura31",
                OctetosPdf = [54],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura60",
                OctetosPdf = [141],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Agrimensura600",
                OctetosPdf = [151],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Asesoria04",
                OctetosPdf = [44],
                Paginas = [7],
                Area = "Asuntos Jurídicos",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Asesoria05",
                OctetosPdf = [119],
                Paginas = [7],
                Area = "Asuntos Jurídicos",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Asesoria06",
                OctetosPdf = [180],
                Paginas = [7],
                Area = "Asuntos Jurídicos",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Asesoria07",
                OctetosPdf = [90],
                Paginas = [7],
                Area = "Asuntos Jurídicos",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Asesoria09",
                OctetosPdf = [82],
                Paginas = [7],
                Area = "Asuntos Jurídicos",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Cartografia04",
                OctetosPdf = [35],
                Paginas = [3],
                Area = "Cartografía",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Cartografia05",
                OctetosPdf = [133],
                Paginas = [3],
                Area = "Cartografía",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Cartografia06",
                OctetosPdf = [157],
                Paginas = [3],
                Area = "Cartografía",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Cartografia07",
                OctetosPdf = [109],
                Paginas = [3],
                Area = "Cartografía",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Cartografia09",
                OctetosPdf = [160],
                Paginas = [3],
                Area = "Cartografía",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Cartografia11",
                OctetosPdf = [107],
                Paginas = [3],
                Area = "Cartografía",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Cartografia12",
                OctetosPdf = [100],
                Paginas = [3],
                Area = "Cartografía",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Cartografia13",
                OctetosPdf = [164],
                Paginas = [3],
                Area = "Cartografía",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Cartografia14",
                OctetosPdf = [165],
                Paginas = [3],
                Area = "Cartografía",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Cartografia16",
                OctetosPdf = [169],
                Paginas = [3],
                Area = "Cartografía",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Cartografia17",
                OctetosPdf = [161],
                Paginas = [3],
                Area = "Cartografía",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Cartografia19",
                OctetosPdf = [168],
                Paginas = [3],
                Area = "Cartografía",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Cartografia20",
                OctetosPdf = [184],
                Paginas = [3],
                Area = "Cartografía",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Cartografia21",
                OctetosPdf = [170],
                Paginas = [3],
                Area = "Cartografía",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Direccion01",
                OctetosPdf = [232],
                Paginas = [13],
                Area = "Dirección y áreas administrativas",
                Piso = "3.er piso"
            },
            new()
            {
                Nombre = "Direccion02",
                OctetosPdf = [85],
                Paginas = [13],
                Area = "Dirección y áreas administrativas",
                Piso = "3.er piso"
            },
            new()
            {
                Nombre = "Direccion03",
                OctetosPdf = [106],
                Paginas = [13],
                Area = "Dirección y áreas administrativas",
                Piso = "3.er piso"
            },
            new()
            {
                Nombre = "Dominial03",
                OctetosPdf = [57],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Entrada01",
                OctetosPdf = [190],
                Paginas = [1],
                Area = "Mesa de Entradas",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Gestion02",
                OctetosPdf = [71],
                Paginas = [4],
                Area = "Gestión, Administración y Régimen",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Inmuebles02",
                OctetosPdf = [133],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles03",
                OctetosPdf = [64],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles04",
                OctetosPdf = [138],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles07",
                OctetosPdf = [135],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles09",
                OctetosPdf = [144],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles10",
                OctetosPdf = [132],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles11",
                OctetosPdf = [173],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles12",
                OctetosPdf = [125],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles13",
                OctetosPdf = [152],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles14",
                OctetosPdf = [142],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles15",
                OctetosPdf = [206],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles16",
                OctetosPdf = [139],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles17",
                OctetosPdf = [236],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles18",
                OctetosPdf = [43],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles19",
                OctetosPdf = [56],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles20",
                OctetosPdf = [145],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles22",
                OctetosPdf = [150],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles23",
                OctetosPdf = [97],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles24",
                OctetosPdf = [153],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Inmuebles26",
                OctetosPdf = [98],
                Paginas = [12],
                Area = "Inmuebles Fiscales",
                Piso = "2.º piso"
            },
            new()
            {
                Nombre = "Intendencia01",
                OctetosPdf = [230],
                Paginas = [2],
                Area = "Entrada y Personal",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Mentrada01",
                OctetosPdf = [69],
                Paginas = [1],
                Area = "Mesa de Entradas",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Mentrada02",
                OctetosPdf = [136],
                Paginas = [1],
                Area = "Mesa de Entradas",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Mentrada03",
                OctetosPdf = [219],
                Paginas = [1],
                Area = "Mesa de Entradas",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Mentrada04",
                OctetosPdf = [216],
                Paginas = [1],
                Area = "Mesa de Entradas",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Mentrada05",
                OctetosPdf = [201],
                Paginas = [1],
                Area = "Mesa de Entradas",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Mentrada06",
                OctetosPdf = [149],
                Paginas = [1],
                Area = "Mesa de Entradas",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Mentrada09",
                OctetosPdf = [140],
                Paginas = [1],
                Area = "Mesa de Entradas",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Municipios01",
                OctetosPdf = [235],
                Paginas = [7],
                Area = "Asuntos Jurídicos",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Personal01",
                OctetosPdf = [90],
                Paginas = [2],
                Area = "Entrada y Personal",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Personal03",
                OctetosPdf = [224],
                Paginas = [2],
                Area = "Entrada y Personal",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Personal04",
                OctetosPdf = [191],
                Paginas = [2],
                Area = "Entrada y Personal",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Personal05",
                OctetosPdf = [95],
                Paginas = [2],
                Area = "Entrada y Personal",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Regimen05",
                OctetosPdf = [238],
                Paginas = [14],
                Area = "Régimen",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Regimen07",
                OctetosPdf = [111],
                Paginas = [14],
                Area = "Régimen",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Regimen09",
                OctetosPdf = [113],
                Paginas = [14],
                Area = "Régimen",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Regimen10",
                OctetosPdf = [197],
                Paginas = [4],
                Area = "Gestión, Administración y Régimen",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Regimen12",
                OctetosPdf = [116],
                Paginas = [14],
                Area = "Régimen",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Regimen13",
                OctetosPdf = [102],
                Paginas = [14],
                Area = "Régimen",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Regimen14",
                OctetosPdf = [118],
                Paginas = [14],
                Area = "Régimen",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Regimen16",
                OctetosPdf = [120],
                Paginas = [14],
                Area = "Régimen",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Regimen17",
                OctetosPdf = [104],
                Paginas = [14],
                Area = "Régimen",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Regimen18",
                OctetosPdf = [121],
                Paginas = [14],
                Area = "Régimen",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Regimen20",
                OctetosPdf = [63],
                Paginas = [14],
                Area = "Régimen",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Regimen21",
                OctetosPdf = [174],
                Paginas = [14],
                Area = "Régimen",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Regimen23",
                OctetosPdf = [126],
                Paginas = [14],
                Area = "Régimen",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Regimen27",
                OctetosPdf = [95],
                Paginas = [14],
                Area = "Régimen",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Regimen28",
                OctetosPdf = [212],
                Paginas = [4],
                Area = "Gestión, Administración y Régimen",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Reloj01",
                OctetosPdf = [240],
                Paginas = [1],
                Area = "Mesa de Entradas",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Reloj02",
                OctetosPdf = [241],
                Paginas = [1],
                Area = "Mesa de Entradas",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Reloj03",
                OctetosPdf = [242],
                Paginas = [1],
                Area = "Mesa de Entradas",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Reloj04",
                OctetosPdf = [234],
                Paginas = [1],
                Area = "Mesa de Entradas",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Secretaria01",
                OctetosPdf = [167],
                Paginas = [13],
                Area = "Dirección y áreas administrativas",
                Piso = "3.er piso"
            },
            new()
            {
                Nombre = "Secretaria02",
                OctetosPdf = [220],
                Paginas = [4, 12],
                Area = "Gestión, Administración y Régimen",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Secretaria03",
                OctetosPdf = [221],
                Paginas = [13],
                Area = "Dirección y áreas administrativas",
                Piso = "3.er piso"
            },
            new()
            {
                Nombre = "Secretaria04",
                OctetosPdf = [131],
                Paginas = [13],
                Area = "Dirección y áreas administrativas",
                Piso = "3.er piso"
            },
            new()
            {
                Nombre = "Secretaria05",
                OctetosPdf = [185],
                Paginas = [13],
                Area = "Dirección y áreas administrativas",
                Piso = "3.er piso"
            },
            new()
            {
                Nombre = "Secretaria07",
                OctetosPdf = [215],
                Paginas = [13],
                Area = "Dirección y áreas administrativas",
                Piso = "3.er piso"
            },
            new()
            {
                Nombre = "Secretaria08",
                OctetosPdf = [114],
                Paginas = [13],
                Area = "Dirección y áreas administrativas",
                Piso = "3.er piso"
            },
            new()
            {
                Nombre = "Secretaria09",
                OctetosPdf = [226],
                Paginas = [4],
                Area = "Gestión, Administración y Régimen",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Secretaria11",
                OctetosPdf = [228],
                Paginas = [4],
                Area = "Gestión, Administración y Régimen",
                Piso = "Planta baja"
            },
            new()
            {
                Nombre = "Secretaria12",
                OctetosPdf = [175],
                Paginas = [13],
                Area = "Dirección y áreas administrativas",
                Piso = "3.er piso"
            },
            new()
            {
                Nombre = "Secretaria13",
                OctetosPdf = [229],
                Paginas = [13],
                Area = "Dirección y áreas administrativas",
                Piso = "3.er piso"
            },
            new()
            {
                Nombre = "Secretaria15",
                OctetosPdf = [73],
                Paginas = [13],
                Area = "Dirección y áreas administrativas",
                Piso = "3.er piso"
            },
            new()
            {
                Nombre = "Sistemas01",
                OctetosPdf = [11],
                Paginas = [8, 9],
                Area = "Sistemas",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sistemas02",
                OctetosPdf = [45],
                Paginas = [9],
                Area = "Sistemas - Desarrollo",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sistemas03",
                OctetosPdf = [96],
                Paginas = [8, 9],
                Area = "Sistemas",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sistemas05",
                OctetosPdf = [15],
                Paginas = [8],
                Area = "Sistemas",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sistemas06",
                OctetosPdf = [127, 189],
                Paginas = [8, 9],
                Area = "Sistemas",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sistemas07",
                OctetosPdf = [53],
                Paginas = [8, 9],
                Area = "Sistemas",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sistemas08",
                OctetosPdf = [117],
                Paginas = [9],
                Area = "Sistemas - Desarrollo",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sistemas09",
                OctetosPdf = [20],
                Paginas = [9],
                Area = "Sistemas - Desarrollo",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sistemas10",
                OctetosPdf = [16],
                Paginas = [8],
                Area = "Sistemas",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sistemas12",
                OctetosPdf = [130],
                Paginas = [9],
                Area = "Sistemas - Desarrollo",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sistemas13",
                OctetosPdf = [76],
                Paginas = [9],
                Area = "Sistemas - Desarrollo",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sistemas19",
                OctetosPdf = [41],
                Paginas = [9],
                Area = "Sistemas - Desarrollo",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sistemas23",
                OctetosPdf = [64],
                Paginas = [9],
                Area = "Sistemas - Desarrollo",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sistemas44",
                OctetosPdf = [187],
                Paginas = [9],
                Area = "Sistemas - Desarrollo",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sumarios03",
                OctetosPdf = [211],
                Paginas = [7],
                Area = "Asuntos Jurídicos",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sumarios04",
                OctetosPdf = [101],
                Paginas = [7],
                Area = "Asuntos Jurídicos",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sumarios05",
                OctetosPdf = [42],
                Paginas = [7],
                Area = "Asuntos Jurídicos",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Sumarios07",
                OctetosPdf = [231],
                Paginas = [7],
                Area = "Asuntos Jurídicos",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion01",
                OctetosPdf = [66],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion03",
                OctetosPdf = [68],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion04",
                OctetosPdf = [218],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion05",
                OctetosPdf = [70],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion06",
                OctetosPdf = [194],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion07",
                OctetosPdf = [72],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion08",
                OctetosPdf = [131],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion09",
                OctetosPdf = [128],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion10",
                OctetosPdf = [75],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion11",
                OctetosPdf = [39],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion12",
                OctetosPdf = [77],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion13",
                OctetosPdf = [137],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion14",
                OctetosPdf = [148],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion15",
                OctetosPdf = [46],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion16",
                OctetosPdf = [80],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion17",
                OctetosPdf = [81],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion18",
                OctetosPdf = [67],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion19",
                OctetosPdf = [214],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion20",
                OctetosPdf = [83],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion21",
                OctetosPdf = [227],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion24",
                OctetosPdf = [87],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion25",
                OctetosPdf = [205],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion26",
                OctetosPdf = [89],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion27",
                OctetosPdf = [217],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion29",
                OctetosPdf = [91],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion30",
                OctetosPdf = [92],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion31",
                OctetosPdf = [93],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion32",
                OctetosPdf = [49],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion37",
                OctetosPdf = [198],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion41",
                OctetosPdf = [88],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion43",
                OctetosPdf = [60],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion50",
                OctetosPdf = [103],
                Paginas = [10],
                Area = "Valuación",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion51",
                OctetosPdf = [158],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            },
            new()
            {
                Nombre = "Valuacion52",
                OctetosPdf = [86],
                Paginas = [11],
                Area = "Valuación Principal",
                Piso = "1.er piso"
            }
            new()
            {
                Nombre = "PLOTTER EPSON T7200",
                OctetosPdf = [189],
                Paginas = [5],
                Area = "Agrimensura",
                Piso = "1.er piso",
                EtiquetaPdf = "T7200",
                Fabricante = "Epson",
                Modelo = "T7200",
                TipoDispositivo = "Impresora",
                RolEnArea = "Periférico de impresión"
            },
            new()
            {
                Nombre = "TRIMBLE TUC1",
                OctetosPdf = [17],
                Paginas = [13],
                Area = "Dirección y áreas administrativas",
                Piso = "3.er piso",
                EtiquetaPdf = "TrimbleNET-R9",
                Fabricante = "Trimble",
                Modelo = "NET-R9",
                TipoDispositivo = "Otro",
                RolEnArea = "Equipo topográfico"
            }
        };

        return filasUbicacion;
    }
}

/// <summary>
/// Describe una estación documentada en el mapa de ubicaciones.
/// </summary>
internal sealed class FilaUbicacionEquipo
{
    /// <summary>Etiqueta normalizada del equipo; cambiarla selecciona otra identidad en la importación.</summary>
    public string Nombre { get; set; } = string.Empty;
    /// <summary>Octetos indicados entre paréntesis; cambiar la lista modifica las IP candidatas del PDF.</summary>
    public List<int> OctetosPdf { get; set; } = [];
    /// <summary>Páginas del documento; cambiar los números altera la trazabilidad de la fuente.</summary>
    public List<int> Paginas { get; set; } = [];
    /// <summary>Área del plano; cambiarla mueve la estación a otra agrupación.</summary>
    public string Area { get; set; } = string.Empty;
    /// <summary>Piso de referencia; cambiarlo modifica la ubicación mostrada.</summary>
    public string Piso { get; set; } = string.Empty;
    /// <summary>Etiqueta textual original; cambiarla altera la nota de normalización.</summary>
    public string? EtiquetaPdf { get; set; }
    /// <summary>Tipo documentado para periféricos excepcionales; cambiarlo modifica su icono y filtro.</summary>
    public string? TipoDispositivo { get; set; }
    /// <summary>Rol documentado para dispositivos no puestos; cambiarlo altera su clasificación funcional.</summary>
    public string? RolEnArea { get; set; }
    /// <summary>Fabricante confirmado para alias de equipo; cambiarlo modifica la ficha técnica.</summary>
    public string? Fabricante { get; set; }
    /// <summary>Modelo confirmado para alias de equipo; cambiarlo modifica el detalle técnico.</summary>
    public string? Modelo { get; set; }
}
