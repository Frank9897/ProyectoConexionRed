using System.Text.Json;

namespace ProyectoConexionRed.Web.Data;

/// <summary>
/// Mantiene las posiciones documentadas en trece hojas de switches de área del V3.
/// </summary>
internal static class CatalogoPuertosAreasV3
{
    // datosJson contiene las filas transcritas con su página y hoja; cambiar una fila modifica la importación.
    private const string DatosJson = """
[
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "2",
    "EtiquetaPuerto": "1",
    "Destino": "AGRIMENSURA60",
    "IpRemota": "192.168.0.141 | 192.168.1.250"
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "3",
    "EtiquetaPuerto": "2",
    "Destino": "AGRIMENSURA01",
    "IpRemota": "192.168.0.155"
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "4",
    "EtiquetaPuerto": "3",
    "Destino": "AGRIMENSURA31",
    "IpRemota": "192.168.0.54"
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "5",
    "EtiquetaPuerto": "4",
    "Destino": "AGRIMENSURA07",
    "IpRemota": "192.168.0.160"
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "6",
    "EtiquetaPuerto": "5",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "7",
    "EtiquetaPuerto": "6",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "8",
    "EtiquetaPuerto": "7",
    "Destino": "Desconocido",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "9",
    "EtiquetaPuerto": "8",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "10",
    "EtiquetaPuerto": "9",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "11",
    "EtiquetaPuerto": "10",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "12",
    "EtiquetaPuerto": "11",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "13",
    "EtiquetaPuerto": "12",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "14",
    "EtiquetaPuerto": "13",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "15",
    "EtiquetaPuerto": "14",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "16",
    "EtiquetaPuerto": "15",
    "Destino": "SWITCH AGRIMENSURA 3COM",
    "IpRemota": "192.168.1.24"
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "17",
    "EtiquetaPuerto": "16",
    "Destino": "AGRIMENSURA15",
    "IpRemota": "192.168.0.48"
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "18",
    "EtiquetaPuerto": "17",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "19",
    "EtiquetaPuerto": "18",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "20",
    "EtiquetaPuerto": "19",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "21",
    "EtiquetaPuerto": "20",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "22",
    "EtiquetaPuerto": "21",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "23",
    "EtiquetaPuerto": "22",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "24",
    "EtiquetaPuerto": "23",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 1 HP",
    "Pagina": "SWITCH AGRIMENSURA HP",
    "Libro": "Excel10",
    "FilaFuente": "25",
    "EtiquetaPuerto": "24",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "2",
    "EtiquetaPuerto": "1",
    "Destino": "PLOTTER EPSON T7200",
    "IpRemota": "192.168.0.202"
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "3",
    "EtiquetaPuerto": "2",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "4",
    "EtiquetaPuerto": "3",
    "Destino": "Desconocido",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "5",
    "EtiquetaPuerto": "4",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "6",
    "EtiquetaPuerto": "5",
    "Destino": "AGRIMENSURA17",
    "IpRemota": "192.168.0.50"
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "7",
    "EtiquetaPuerto": "6",
    "Destino": "AGRIMENSURA05",
    "IpRemota": "192.168.0.147"
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "8",
    "EtiquetaPuerto": "7",
    "Destino": "Desconocido",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "9",
    "EtiquetaPuerto": "8",
    "Destino": "Agrimensura600",
    "IpRemota": "192.168.0.151"
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "10",
    "EtiquetaPuerto": "9",
    "Destino": "Agrimensura04",
    "IpRemota": "192.168.0.38"
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "11",
    "EtiquetaPuerto": "10",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "12",
    "EtiquetaPuerto": "11",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "13",
    "EtiquetaPuerto": "12",
    "Destino": "Agrimensura27",
    "IpRemota": "192.168.0.58"
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "14",
    "EtiquetaPuerto": "13",
    "Destino": "Agrimensura20",
    "IpRemota": "192.168.0.74"
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "15",
    "EtiquetaPuerto": "14",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "16",
    "EtiquetaPuerto": "15",
    "Destino": "Agrimensura14",
    "IpRemota": "192.168.0.162"
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "17",
    "EtiquetaPuerto": "16",
    "Destino": "Agrimensura02",
    "IpRemota": "192.168.0.36"
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "18",
    "EtiquetaPuerto": "17",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "19",
    "EtiquetaPuerto": "18",
    "Destino": "Agrimensura13",
    "IpRemota": "192.168.0.47"
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "20",
    "EtiquetaPuerto": "19",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "21",
    "EtiquetaPuerto": "20",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "22",
    "EtiquetaPuerto": "21",
    "Destino": "Agrimensura06",
    "IpRemota": "192.168.0.171"
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "23",
    "EtiquetaPuerto": "22",
    "Destino": "Agrimensura10",
    "IpRemota": "192.168.0.61"
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "24",
    "EtiquetaPuerto": "23",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Switch Agrimensura 3Com",
    "Pagina": "SWITCH AGRIMENSURA 3COM",
    "Libro": "Excel12",
    "FilaFuente": "25",
    "EtiquetaPuerto": "24",
    "Destino": "SWITCH AGRIMENSURA1 HP",
    "IpRemota": "192.168.1.23"
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "2",
    "EtiquetaPuerto": "1",
    "Destino": "Asesoria09",
    "IpRemota": "192.168.0.82"
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "3",
    "EtiquetaPuerto": "2",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "4",
    "EtiquetaPuerto": "3",
    "Destino": "Asesoria07",
    "IpRemota": "192.168.0.90"
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "5",
    "EtiquetaPuerto": "4",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "6",
    "EtiquetaPuerto": "5",
    "Destino": "Sumarios04",
    "IpRemota": "192.168.0.44"
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "7",
    "EtiquetaPuerto": "6",
    "Destino": "Desconocido",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "8",
    "EtiquetaPuerto": "7",
    "Destino": "Desconocido",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "9",
    "EtiquetaPuerto": "8",
    "Destino": "Agrimensura600",
    "IpRemota": "192.168.0.151"
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "10",
    "EtiquetaPuerto": "9",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "11",
    "EtiquetaPuerto": "10",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "12",
    "EtiquetaPuerto": "11",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "13",
    "EtiquetaPuerto": "12",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "14",
    "EtiquetaPuerto": "13",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "15",
    "EtiquetaPuerto": "14",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "16",
    "EtiquetaPuerto": "15",
    "Destino": "Sumarios05",
    "IpRemota": "192.168.0.42"
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "17",
    "EtiquetaPuerto": "16",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "18",
    "EtiquetaPuerto": "17",
    "Destino": "Asesoria05",
    "IpRemota": "192.168.0.119"
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "19",
    "EtiquetaPuerto": "18",
    "Destino": "Asesoria06",
    "IpRemota": "192.168.0.180"
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "20",
    "EtiquetaPuerto": "19",
    "Destino": "Sumarios07",
    "IpRemota": "192.168.0.231"
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "21",
    "EtiquetaPuerto": "20",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "22",
    "EtiquetaPuerto": "21",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "23",
    "EtiquetaPuerto": "22",
    "Destino": "MUNICIPIOS01",
    "IpRemota": "192.168.0.235"
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "24",
    "EtiquetaPuerto": "23",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Asuntos Jurídicos",
    "Pagina": "ASUNTOS JURIDICOS",
    "Libro": "Excel13",
    "FilaFuente": "25",
    "EtiquetaPuerto": "24",
    "Destino": "Switch DELL S4148U-ON 48G",
    "IpRemota": "192.168.1.44(SSH)"
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "2",
    "EtiquetaPuerto": "1",
    "Destino": "Cartografia17",
    "IpRemota": "192.168.0.161"
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "3",
    "EtiquetaPuerto": "2",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "4",
    "EtiquetaPuerto": "3",
    "Destino": "Cartografia12",
    "IpRemota": "192.168.0.100"
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "5",
    "EtiquetaPuerto": "4",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "6",
    "EtiquetaPuerto": "5",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "7",
    "EtiquetaPuerto": "6",
    "Destino": "Cartografia16",
    "IpRemota": "192.168.0.146"
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "8",
    "EtiquetaPuerto": "7",
    "Destino": "Cartografia05",
    "IpRemota": "192.168.0.133"
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "9",
    "EtiquetaPuerto": "8",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "10",
    "EtiquetaPuerto": "9",
    "Destino": "Cartografia06",
    "IpRemota": "192.168.0.157"
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "11",
    "EtiquetaPuerto": "10",
    "Destino": "Cartografia13",
    "IpRemota": "192.168.0.164"
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "12",
    "EtiquetaPuerto": "11",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "13",
    "EtiquetaPuerto": "12",
    "Destino": "Cartografia11",
    "IpRemota": "192.168.0.107"
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "14",
    "EtiquetaPuerto": "13",
    "Destino": "Cartografia04",
    "IpRemota": "192.168.0.62"
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "15",
    "EtiquetaPuerto": "14",
    "Destino": "Cartografia14",
    "IpRemota": "192.168.0.165"
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "16",
    "EtiquetaPuerto": "15",
    "Destino": "Cartografia15",
    "IpRemota": "192.168.0.109"
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "17",
    "EtiquetaPuerto": "16",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "18",
    "EtiquetaPuerto": "17",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "19",
    "EtiquetaPuerto": "18",
    "Destino": "Cartografia20",
    "IpRemota": "192.168.0.184"
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "20",
    "EtiquetaPuerto": "19",
    "Destino": "Cartografia19",
    "IpRemota": "192.168.0.154"
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "21",
    "EtiquetaPuerto": "20",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "22",
    "EtiquetaPuerto": "21",
    "Destino": "Cartografia21",
    "IpRemota": "192.168.0.176"
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "23",
    "EtiquetaPuerto": "22",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "24",
    "EtiquetaPuerto": "23",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Cartografía",
    "Pagina": "CARTOGRAFIA",
    "Libro": "Excel14",
    "FilaFuente": "25",
    "EtiquetaPuerto": "24",
    "Destino": "HPE 1920s (E)",
    "IpRemota": "192.168.1.115"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "2",
    "EtiquetaPuerto": "1",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "3",
    "EtiquetaPuerto": "2",
    "Destino": "Inmuebles02",
    "IpRemota": "192.168.0.220"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "4",
    "EtiquetaPuerto": "3",
    "Destino": "Inmuebles04",
    "IpRemota": "192.168.0.179"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "5",
    "EtiquetaPuerto": "4",
    "Destino": "Inmuebles17",
    "IpRemota": "192.168.0.236"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "6",
    "EtiquetaPuerto": "5",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "7",
    "EtiquetaPuerto": "6",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "8",
    "EtiquetaPuerto": "7",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "9",
    "EtiquetaPuerto": "8",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "10",
    "EtiquetaPuerto": "9",
    "Destino": "Inmuebles26",
    "IpRemota": "192.168.0.98"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "11",
    "EtiquetaPuerto": "10",
    "Destino": "Dominial02",
    "IpRemota": "192.168.0.134"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "12",
    "EtiquetaPuerto": "11",
    "Destino": "Inmuebles24",
    "IpRemota": "192.168.0.153"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "13",
    "EtiquetaPuerto": "12",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "14",
    "EtiquetaPuerto": "13",
    "Destino": "Inmuebles15",
    "IpRemota": "192.168.0.206"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "15",
    "EtiquetaPuerto": "14",
    "Destino": "Dominial03",
    "IpRemota": "192.168.0.230"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "16",
    "EtiquetaPuerto": "15",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "17",
    "EtiquetaPuerto": "16",
    "Destino": "Inmuebles10",
    "IpRemota": "192.168.0.132"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "18",
    "EtiquetaPuerto": "17",
    "Destino": "Inmuebles13",
    "IpRemota": "192.168.0.152"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "19",
    "EtiquetaPuerto": "18",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "20",
    "EtiquetaPuerto": "19",
    "Destino": "Inmuebles16",
    "IpRemota": "192.168.0.139"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "21",
    "EtiquetaPuerto": "20",
    "Destino": "Inmuebles12",
    "IpRemota": "192.168.0.125"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "22",
    "EtiquetaPuerto": "21",
    "Destino": "Inmuebles22",
    "IpRemota": "192.168.0.150"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "23",
    "EtiquetaPuerto": "22",
    "Destino": "Inmuebles18",
    "IpRemota": "192.168.0.43"
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "24",
    "EtiquetaPuerto": "23",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Inmuebles",
    "Pagina": "INMUEBLES",
    "Libro": "Excel15",
    "FilaFuente": "25",
    "EtiquetaPuerto": "24",
    "Destino": "SWITCH DE 8 PUERTOS",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "2",
    "EtiquetaPuerto": "1",
    "Destino": "MENTRADA04",
    "IpRemota": "192.168.0.216"
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "3",
    "EtiquetaPuerto": "2",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "4",
    "EtiquetaPuerto": "3",
    "Destino": "MENTRADA02",
    "IpRemota": "192.168.0.136"
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "5",
    "EtiquetaPuerto": "4",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "6",
    "EtiquetaPuerto": "5",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "7",
    "EtiquetaPuerto": "6",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "8",
    "EtiquetaPuerto": "7",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "9",
    "EtiquetaPuerto": "8",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "10",
    "EtiquetaPuerto": "9",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "11",
    "EtiquetaPuerto": "10",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "12",
    "EtiquetaPuerto": "11",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "13",
    "EtiquetaPuerto": "12",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "14",
    "EtiquetaPuerto": "13",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "15",
    "EtiquetaPuerto": "14",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "16",
    "EtiquetaPuerto": "15",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "17",
    "EtiquetaPuerto": "16",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "18",
    "EtiquetaPuerto": "17",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "19",
    "EtiquetaPuerto": "18",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "20",
    "EtiquetaPuerto": "19",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "21",
    "EtiquetaPuerto": "20",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "22",
    "EtiquetaPuerto": "21",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "23",
    "EtiquetaPuerto": "22",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "24",
    "EtiquetaPuerto": "23",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Mesa de Entrada (No principal)",
    "Pagina": "MESA DE ENTRADA(No principal)",
    "Libro": "Excel18",
    "FilaFuente": "25",
    "EtiquetaPuerto": "24",
    "Destino": "SWTICH ARUBA Mentrada",
    "IpRemota": "192.168.1.1"
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "2",
    "EtiquetaPuerto": "1",
    "Destino": "SWITCH ProCurve 2910al-24G",
    "IpRemota": "10.0.1.0"
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "3",
    "EtiquetaPuerto": "2",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "4",
    "EtiquetaPuerto": "3",
    "Destino": "Agrimensura16",
    "IpRemota": "192.168.0.163"
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "5",
    "EtiquetaPuerto": "4",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "6",
    "EtiquetaPuerto": "5",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "7",
    "EtiquetaPuerto": "6",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "8",
    "EtiquetaPuerto": "7",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "9",
    "EtiquetaPuerto": "8",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "10",
    "EtiquetaPuerto": "9",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "11",
    "EtiquetaPuerto": "10",
    "Destino": "Agrimensura24",
    "IpRemota": "192.168.0.110"
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "12",
    "EtiquetaPuerto": "11",
    "Destino": "Agrimensura22",
    "IpRemota": "192.168.0.55"
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "13",
    "EtiquetaPuerto": "12",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "14",
    "EtiquetaPuerto": "13",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "15",
    "EtiquetaPuerto": "14",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "16",
    "EtiquetaPuerto": "15",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "17",
    "EtiquetaPuerto": "16",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "18",
    "EtiquetaPuerto": "17",
    "Destino": "Agrimensura29",
    "IpRemota": "192.168.0.195"
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "19",
    "EtiquetaPuerto": "18",
    "Destino": "Agrimensura21",
    "IpRemota": "192.168.0.124"
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "20",
    "EtiquetaPuerto": "19",
    "Destino": "Agrimensura23",
    "IpRemota": "192.168.0.37"
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "21",
    "EtiquetaPuerto": "20",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "22",
    "EtiquetaPuerto": "21",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "23",
    "EtiquetaPuerto": "22",
    "Destino": "Agrimensura28",
    "IpRemota": "192.168.0.59"
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "24",
    "EtiquetaPuerto": "23",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Puerta Agrimensura",
    "Pagina": "PUERTA AGRIMENSURA",
    "Libro": "Excel19",
    "FilaFuente": "25",
    "EtiquetaPuerto": "24",
    "Destino": "SWTICH ARUBA Mentrada",
    "IpRemota": "192.168.1.1"
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "2",
    "EtiquetaPuerto": "1",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "3",
    "EtiquetaPuerto": "2",
    "Destino": "Valuacion51",
    "IpRemota": "192.168.0.196"
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "4",
    "EtiquetaPuerto": "3",
    "Destino": "Secretaria11",
    "IpRemota": "192.168.0.228"
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "5",
    "EtiquetaPuerto": "4",
    "Destino": "Admin03",
    "IpRemota": "192.168.0.193"
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "6",
    "EtiquetaPuerto": "5",
    "Destino": "Admin05",
    "IpRemota": "192.168.0.78"
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "7",
    "EtiquetaPuerto": "6",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "8",
    "EtiquetaPuerto": "7",
    "Destino": "Admin02",
    "IpRemota": "192.168.0.71"
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "9",
    "EtiquetaPuerto": "8",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "10",
    "EtiquetaPuerto": "9",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "11",
    "EtiquetaPuerto": "10",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "12",
    "EtiquetaPuerto": "11",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "13",
    "EtiquetaPuerto": "12",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "14",
    "EtiquetaPuerto": "13",
    "Destino": "Secretaria02",
    "IpRemota": "192.168.0.170"
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "15",
    "EtiquetaPuerto": "14",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "16",
    "EtiquetaPuerto": "15",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "17",
    "EtiquetaPuerto": "16",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "18",
    "EtiquetaPuerto": "17",
    "Destino": "Regimen28",
    "IpRemota": "192.168.0.212"
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "19",
    "EtiquetaPuerto": "18",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "20",
    "EtiquetaPuerto": "19",
    "Destino": "Sistemas06",
    "IpRemota": "192.168.0.197"
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "21",
    "EtiquetaPuerto": "20",
    "Destino": "Secretaria09",
    "IpRemota": "192.168.0.226"
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "22",
    "EtiquetaPuerto": "21",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "23",
    "EtiquetaPuerto": "22",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "24",
    "EtiquetaPuerto": "23",
    "Destino": "Gestion02",
    "IpRemota": "192.168.0.199"
  },
  {
    "EquipoLocal": "Regularización Dominial",
    "Pagina": "REGULARIZACION DOMINIAL",
    "Libro": "Excel20",
    "FilaFuente": "25",
    "EtiquetaPuerto": "24",
    "Destino": "SWITCH HP1920 (E)",
    "IpRemota": "192.168.1.115"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "2",
    "EtiquetaPuerto": "1",
    "Destino": "Secretaria08",
    "IpRemota": "192.168.0.114"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "3",
    "EtiquetaPuerto": "2",
    "Destino": "Secretaria03",
    "IpRemota": "192.168.0.123"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "4",
    "EtiquetaPuerto": "3",
    "Destino": "Secretaria17",
    "IpRemota": "192.168.0.172"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "5",
    "EtiquetaPuerto": "4",
    "Destino": "TRIMBLE TUC1",
    "IpRemota": "192.168.0.17"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "6",
    "EtiquetaPuerto": "5",
    "Destino": "Secretaria15",
    "IpRemota": "192.168.0.73"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "7",
    "EtiquetaPuerto": "6",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "8",
    "EtiquetaPuerto": "7",
    "Destino": "Secretaria05",
    "IpRemota": "192.168.0.185"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "9",
    "EtiquetaPuerto": "8",
    "Destino": "Secretaria12",
    "IpRemota": "192.168.0.175"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "10",
    "EtiquetaPuerto": "9",
    "Destino": "Direccion02",
    "IpRemota": "192.168.0.85"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "11",
    "EtiquetaPuerto": "10",
    "Destino": "Direccion01",
    "IpRemota": "192.168.0.232"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "12",
    "EtiquetaPuerto": "11",
    "Destino": "Secretaria16",
    "IpRemota": "192.168.0.115"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "13",
    "EtiquetaPuerto": "12",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "14",
    "EtiquetaPuerto": "13",
    "Destino": "Secretaria14",
    "IpRemota": "192.168.0.237"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "15",
    "EtiquetaPuerto": "14",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "16",
    "EtiquetaPuerto": "15",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "17",
    "EtiquetaPuerto": "16",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "18",
    "EtiquetaPuerto": "17",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "19",
    "EtiquetaPuerto": "18",
    "Destino": "Secretaria13",
    "IpRemota": "192.168.0.229"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "20",
    "EtiquetaPuerto": "19",
    "Destino": "Secretaria01",
    "IpRemota": "192.168.0.167"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "21",
    "EtiquetaPuerto": "20",
    "Destino": "Direccion03",
    "IpRemota": "192.168.0.106"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "22",
    "EtiquetaPuerto": "21",
    "Destino": "Secretaria07",
    "IpRemota": "192.168.0.215"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "23",
    "EtiquetaPuerto": "22",
    "Destino": "Fotocopiadora RICOH",
    "IpRemota": "192.168.0.213"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "24",
    "EtiquetaPuerto": "23",
    "Destino": "Secretaria04",
    "IpRemota": "192.168.0.131"
  },
  {
    "EquipoLocal": "Secretaría",
    "Pagina": "SECRETARIA",
    "Libro": "Excel21",
    "FilaFuente": "25",
    "EtiquetaPuerto": "24",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "2",
    "EtiquetaPuerto": "1",
    "Destino": "Sistemas01",
    "IpRemota": "192.168.0.11"
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "3",
    "EtiquetaPuerto": "2",
    "Destino": "Sistemas05",
    "IpRemota": "192.168.0.15"
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "4",
    "EtiquetaPuerto": "3",
    "Destino": "Sistemas03",
    "IpRemota": "192.168.0.96 - 192.168.1.140"
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "5",
    "EtiquetaPuerto": "4",
    "Destino": "Sistemas07",
    "IpRemota": "192.168.0.53"
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "6",
    "EtiquetaPuerto": "5",
    "Destino": "Sistemas10",
    "IpRemota": "192.168.0.16 - 192.168.1.73"
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "7",
    "EtiquetaPuerto": "6",
    "Destino": "Sistemas23",
    "IpRemota": "192.168.0.127 - 192.168.1.105"
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "8",
    "EtiquetaPuerto": "7",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "9",
    "EtiquetaPuerto": "8",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "10",
    "EtiquetaPuerto": "9",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "11",
    "EtiquetaPuerto": "10",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "12",
    "EtiquetaPuerto": "11",
    "Destino": "Sistemas19",
    "IpRemota": "192.168.0.41 - 192.168.1.100"
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "13",
    "EtiquetaPuerto": "12",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "14",
    "EtiquetaPuerto": "13",
    "Destino": "Sistemas02",
    "IpRemota": "192.168.0.45"
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "15",
    "EtiquetaPuerto": "14",
    "Destino": "Sistemas13",
    "IpRemota": "192.168.0.76"
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "16",
    "EtiquetaPuerto": "15",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "17",
    "EtiquetaPuerto": "16",
    "Destino": "Sistemas44",
    "IpRemota": "192.168.0.187"
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "18",
    "EtiquetaPuerto": "17",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "19",
    "EtiquetaPuerto": "18",
    "Destino": "Sistemas08",
    "IpRemota": "192.168.0.117"
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "20",
    "EtiquetaPuerto": "19",
    "Destino": "Sistemas09",
    "IpRemota": "192.168.0.20"
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "21",
    "EtiquetaPuerto": "20",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "22",
    "EtiquetaPuerto": "21",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "23",
    "EtiquetaPuerto": "22",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "24",
    "EtiquetaPuerto": "23",
    "Destino": "Sistemas06",
    "IpRemota": "192.168.0.189"
  },
  {
    "EquipoLocal": "Sistemas",
    "Pagina": "SISTEMAS",
    "Libro": "Excel23",
    "FilaFuente": "25",
    "EtiquetaPuerto": "24",
    "Destino": "SWITCH HP1920 (E)",
    "IpRemota": "192.168.1.115"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "2",
    "EtiquetaPuerto": "1",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "3",
    "EtiquetaPuerto": "2",
    "Destino": "Valuacion50",
    "IpRemota": "192.168.0.103"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "4",
    "EtiquetaPuerto": "3",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "5",
    "EtiquetaPuerto": "4",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "6",
    "EtiquetaPuerto": "5",
    "Destino": "Valuacion21",
    "IpRemota": "192.168.0.227"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "7",
    "EtiquetaPuerto": "6",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "8",
    "EtiquetaPuerto": "7",
    "Destino": "DESCONOCIDO",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "9",
    "EtiquetaPuerto": "8",
    "Destino": "Valuacion29",
    "IpRemota": "192.168.0.91"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "10",
    "EtiquetaPuerto": "9",
    "Destino": "Valuacion19",
    "IpRemota": "192.168.0.214"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "11",
    "EtiquetaPuerto": "10",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "12",
    "EtiquetaPuerto": "11",
    "Destino": "Valuacion05",
    "IpRemota": "192.168.0.70"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "13",
    "EtiquetaPuerto": "12",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "14",
    "EtiquetaPuerto": "13",
    "Destino": "Valuacion04",
    "IpRemota": "192.168.0.218"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "15",
    "EtiquetaPuerto": "14",
    "Destino": "Valuacion31",
    "IpRemota": "192.168.0.93"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "16",
    "EtiquetaPuerto": "15",
    "Destino": "Valuacion24",
    "IpRemota": "192.168.0.87"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "17",
    "EtiquetaPuerto": "16",
    "Destino": "Valuacion08",
    "IpRemota": "192.168.0.112"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "18",
    "EtiquetaPuerto": "17",
    "Destino": "KONICA MINOLTA",
    "IpRemota": "192.168.0.204"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "19",
    "EtiquetaPuerto": "18",
    "Destino": "Valuacion26",
    "IpRemota": "192.168.0.89"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "20",
    "EtiquetaPuerto": "19",
    "Destino": "Valuacion12",
    "IpRemota": "192.168.0.77"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "21",
    "EtiquetaPuerto": "20",
    "Destino": "Valuacion06",
    "IpRemota": "192.168.0.194"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "22",
    "EtiquetaPuerto": "21",
    "Destino": "Valuacion13",
    "IpRemota": "192.168.0.137"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "23",
    "EtiquetaPuerto": "22",
    "Destino": "Valuacion01",
    "IpRemota": "192.168.0.66"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "24",
    "EtiquetaPuerto": "23",
    "Destino": "Valuacion16",
    "IpRemota": "192.168.0.80"
  },
  {
    "EquipoLocal": "Valuación R0",
    "Pagina": "VALUACION R0",
    "Libro": "Excel24",
    "FilaFuente": "25",
    "EtiquetaPuerto": "24",
    "Destino": "SWITCH AGRIMENSURA",
    "IpRemota": "192.168.1.24"
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "2",
    "EtiquetaPuerto": "1",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "3",
    "EtiquetaPuerto": "2",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "4",
    "EtiquetaPuerto": "3",
    "Destino": "Valuacion43",
    "IpRemota": "192.168.0.60"
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "5",
    "EtiquetaPuerto": "4",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "6",
    "EtiquetaPuerto": "5",
    "Destino": "Valuacion18",
    "IpRemota": "192.168.0.67"
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "7",
    "EtiquetaPuerto": "6",
    "Destino": "Valuacion14",
    "IpRemota": "192.168.0.148"
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "8",
    "EtiquetaPuerto": "7",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "9",
    "EtiquetaPuerto": "8",
    "Destino": "Valuacion20",
    "IpRemota": "192.168.0.83"
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "10",
    "EtiquetaPuerto": "9",
    "Destino": "Valuacion11",
    "IpRemota": "192.168.0.39"
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "11",
    "EtiquetaPuerto": "10",
    "Destino": "Valuacion41",
    "IpRemota": "192.168.0.88"
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "12",
    "EtiquetaPuerto": "11",
    "Destino": "Valuacion51",
    "IpRemota": "192.168.0.158"
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "13",
    "EtiquetaPuerto": "12",
    "Destino": "DESCONOCIDO",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "14",
    "EtiquetaPuerto": "13",
    "Destino": "Valuacion09",
    "IpRemota": "192.168.0.128"
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "15",
    "EtiquetaPuerto": "14",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "16",
    "EtiquetaPuerto": "15",
    "Destino": "Valuacion52",
    "IpRemota": "192.168.0.86"
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "17",
    "EtiquetaPuerto": "16",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "18",
    "EtiquetaPuerto": "17",
    "Destino": "Valuacion30",
    "IpRemota": "192.168.0.92"
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "19",
    "EtiquetaPuerto": "18",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "20",
    "EtiquetaPuerto": "19",
    "Destino": "Valuacion27",
    "IpRemota": "192.168.0.217"
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "21",
    "EtiquetaPuerto": "20",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "22",
    "EtiquetaPuerto": "21",
    "Destino": "DESCONOCIDO",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "23",
    "EtiquetaPuerto": "22",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "24",
    "EtiquetaPuerto": "23",
    "Destino": "SWITCH VALUACIONR2",
    "IpRemota": "192.168.1.20"
  },
  {
    "EquipoLocal": "Valuación R1",
    "Pagina": "VALUACION R1",
    "Libro": "Excel25",
    "FilaFuente": "25",
    "EtiquetaPuerto": "24",
    "Destino": "SWITCH HP1920 (E)",
    "IpRemota": "192.168.1.115"
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "2",
    "EtiquetaPuerto": "1",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "3",
    "EtiquetaPuerto": "2",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "4",
    "EtiquetaPuerto": "3",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "5",
    "EtiquetaPuerto": "4",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "6",
    "EtiquetaPuerto": "5",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "7",
    "EtiquetaPuerto": "6",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "8",
    "EtiquetaPuerto": "7",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "9",
    "EtiquetaPuerto": "8",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "10",
    "EtiquetaPuerto": "9",
    "Destino": "Valuacion17",
    "IpRemota": "192.168.0.81"
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "11",
    "EtiquetaPuerto": "10",
    "Destino": "Valuacion07",
    "IpRemota": "192.168.0.72"
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "12",
    "EtiquetaPuerto": "11",
    "Destino": "Valuacion32",
    "IpRemota": "192.168.0.49"
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "13",
    "EtiquetaPuerto": "12",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "14",
    "EtiquetaPuerto": "13",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "15",
    "EtiquetaPuerto": "14",
    "Destino": "SWITCH VALUACIONR1",
    "IpRemota": "192.168.1.29"
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "16",
    "EtiquetaPuerto": "15",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "17",
    "EtiquetaPuerto": "16",
    "Destino": "Valuacion25",
    "IpRemota": "192.168.0.205"
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "18",
    "EtiquetaPuerto": "17",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "19",
    "EtiquetaPuerto": "18",
    "Destino": "Valuacion03",
    "IpRemota": "192.168.0.68"
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "20",
    "EtiquetaPuerto": "19",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "21",
    "EtiquetaPuerto": "20",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "22",
    "EtiquetaPuerto": "21",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "23",
    "EtiquetaPuerto": "22",
    "Destino": "LIBRE",
    "IpRemota": null
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "24",
    "EtiquetaPuerto": "23",
    "Destino": "SWITCH VALUACIONR0",
    "IpRemota": "192.168.0.32"
  },
  {
    "EquipoLocal": "Valuación R2",
    "Pagina": "VALUACION R2",
    "Libro": "Excel26",
    "FilaFuente": "25",
    "EtiquetaPuerto": "24",
    "Destino": "SWITCH HP1920 (E)",
    "IpRemota": "192.168.1.115"
  }
]
""";

    /// <summary>
    /// Convierte el catálogo fijo en filas tipadas para el inicializador.
    /// </summary>
    /// <returns>Puertos locales, destinos literales y referencias de fila; cambiar el JSON altera las filas cargadas.</returns>
    public static IReadOnlyList<FilaPuertoAreaV3> ObtenerFilas()
    {
        // filasImportadas deserializa las celdas sin completar campos ausentes.
        var filasImportadas = JsonSerializer.Deserialize<List<FilaPuertoAreaV3>>(DatosJson);
        return filasImportadas ?? [];
    }
}

/// <summary>Representa una fila literal de una tabla de puertos del V3.</summary>
internal sealed class FilaPuertoAreaV3
{
    /// <summary>Nombre canónico del switch local; cambiarlo asigna la fila a otra ficha.</summary>
    public string EquipoLocal { get; set; } = string.Empty;
    /// <summary>Página fuente del Visio; cambiarla altera la referencia documental.</summary>
    public string Pagina { get; set; } = string.Empty;
    /// <summary>Libro incrustado; cambiarlo apunta a otra hoja Excel.</summary>
    public string Libro { get; set; } = string.Empty;
    /// <summary>Fila original; cambiarla modifica el rastro dentro del libro.</summary>
    public string FilaFuente { get; set; } = string.Empty;
    /// <summary>Etiqueta local del puerto; cambiarla dirige la relación a otra boca.</summary>
    public string EtiquetaPuerto { get; set; } = string.Empty;
    /// <summary>Destino o estado literal; cambiarlo altera lo que se importa.</summary>
    public string Destino { get; set; } = string.Empty;
    /// <summary>IP remota literal; cambiarla reemplaza el dato escrito en la hoja.</summary>
    public string? IpRemota { get; set; }
}
