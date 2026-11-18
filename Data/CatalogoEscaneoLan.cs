using System.Text.Json;

namespace ProyectoConexionRed.Web.Data;

/// <summary>
/// Conserva el resultado del escaneo de la LAN (archivo datos_ip.html del escáner de red).
/// Estos datos solo completan fichas del inventario SQLite; no son una base paralela ni consultan la red.
/// </summary>
internal static class CatalogoEscaneoLan
{
    // DatosJson contiene una fila por host detectado; Nombre nulo significa que el escáner no resolvió el nombre
    // y lo mostró igual que la IP; Mac nula significa que el escáner informó 00:00:00:00:00:00 (dato no válido).
    private const string DatosJson = """
[
  {
    "Ip": "192.168.0.1",
    "Nombre": "DGCPRO",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:24:4A",
    "Activo": true
  },
  {
    "Ip": "192.168.0.3",
    "Nombre": null,
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:77:9B",
    "Activo": true
  },
  {
    "Ip": "192.168.0.4",
    "Nombre": "PRONEW",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:B9:82",
    "Activo": true
  },
  {
    "Ip": "192.168.0.6",
    "Nombre": "PRODUCON2016",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:86:ED",
    "Activo": true
  },
  {
    "Ip": "192.168.0.7",
    "Nombre": "VIRTUALXP44",
    "Fabricante": "Microsoft Corporation",
    "Mac": "00:03:FF:45:4B:37",
    "Activo": true
  },
  {
    "Ip": "192.168.0.8",
    "Nombre": null,
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:A6:A2",
    "Activo": true
  },
  {
    "Ip": "192.168.0.9",
    "Nombre": null,
    "Fabricante": "TP-LINK TECHNOLOGIES CO.,LTD.",
    "Mac": "50:3E:AA:14:53:F7",
    "Activo": true
  },
  {
    "Ip": "192.168.0.10",
    "Nombre": "CLON2019",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:4C:B1",
    "Activo": true
  },
  {
    "Ip": "192.168.0.11",
    "Nombre": "sistemas01",
    "Fabricante": "Hon Hai Precision Ind. Co., Ltd.",
    "Mac": "A4:AE:12:7B:C4:71",
    "Activo": true
  },
  {
    "Ip": "192.168.0.12",
    "Nombre": "SOPORTE",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:86:08",
    "Activo": true
  },
  {
    "Ip": "192.168.0.13",
    "Nombre": "WS2019",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:5F:39",
    "Activo": true
  },
  {
    "Ip": "192.168.0.15",
    "Nombre": "SISTEMAS05",
    "Fabricante": "Dell Inc.",
    "Mac": "08:92:04:DF:4C:6B",
    "Activo": true
  },
  {
    "Ip": "192.168.0.16",
    "Nombre": "Sistemas10",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:31:3D",
    "Activo": true
  },
  {
    "Ip": "192.168.0.17",
    "Nombre": null,
    "Fabricante": "Microchip Technology Inc.",
    "Mac": "00:1E:C0:9C:90:17",
    "Activo": true
  },
  {
    "Ip": "192.168.0.20",
    "Nombre": "Sistemas09",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:7D:EB",
    "Activo": true
  },
  {
    "Ip": "192.168.0.22",
    "Nombre": "PANAMA",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:33:01",
    "Activo": true
  },
  {
    "Ip": "192.168.0.23",
    "Nombre": "A1-VEEAM",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:19:41",
    "Activo": true
  },
  {
    "Ip": "192.168.0.24",
    "Nombre": "NEWPRO",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:A2:48",
    "Activo": true
  },
  {
    "Ip": "192.168.0.25",
    "Nombre": "PRODUCON",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:33:F2",
    "Activo": true
  },
  {
    "Ip": "192.168.0.26",
    "Nombre": "Catastro185",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:0B:8F",
    "Activo": true
  },
  {
    "Ip": "192.168.0.27",
    "Nombre": "MULTISERVER",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:82:0F:0F",
    "Activo": true
  },
  {
    "Ip": "192.168.0.28",
    "Nombre": "Wordpress",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:AB:9C",
    "Activo": true
  },
  {
    "Ip": "192.168.0.29",
    "Nombre": "MAPSERVER02",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:82:12:86",
    "Activo": true
  },
  {
    "Ip": "192.168.0.30",
    "Nombre": "PRODUCONBAK",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:82:2A:68",
    "Activo": true
  },
  {
    "Ip": "192.168.0.31",
    "Nombre": "PRODUCON2BAK",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:FD:65",
    "Activo": true
  },
  {
    "Ip": "192.168.0.32",
    "Nombre": "GEOSERVER",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:F1:29",
    "Activo": true
  },
  {
    "Ip": "192.168.0.36",
    "Nombre": "Agrimensura02",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:76:71",
    "Activo": true
  },
  {
    "Ip": "192.168.0.37",
    "Nombre": "Agrimensura23",
    "Fabricante": "Dell Inc.",
    "Mac": "08:92:04:DF:4C:C6",
    "Activo": true
  },
  {
    "Ip": "192.168.0.38",
    "Nombre": "Agrimensura04",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:49:1A",
    "Activo": true
  },
  {
    "Ip": "192.168.0.41",
    "Nombre": "Sistemas19",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:78:40",
    "Activo": true
  },
  {
    "Ip": "192.168.0.42",
    "Nombre": "Sumarios05",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:4A:A5",
    "Activo": true
  },
  {
    "Ip": "192.168.0.43",
    "Nombre": null,
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:4D:F9",
    "Activo": true
  },
  {
    "Ip": "192.168.0.44",
    "Nombre": "Sumarios04",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C7:25:34",
    "Activo": true
  },
  {
    "Ip": "192.168.0.45",
    "Nombre": "Sistemas02",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:80:4B",
    "Activo": true
  },
  {
    "Ip": "192.168.0.47",
    "Nombre": null,
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:48:FD",
    "Activo": true
  },
  {
    "Ip": "192.168.0.48",
    "Nombre": "Agrimensura15",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:78:7F",
    "Activo": true
  },
  {
    "Ip": "192.168.0.49",
    "Nombre": "VALUACION32",
    "Fabricante": "PEGATRON CORPORATION",
    "Mac": "60:02:92:B3:09:73",
    "Activo": true
  },
  {
    "Ip": "192.168.0.51",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:77:FF",
    "Activo": true
  },
  {
    "Ip": "192.168.0.53",
    "Nombre": "Sistemas07",
    "Fabricante": "Dell Inc.",
    "Mac": "54:BF:64:65:8E:5B",
    "Activo": true
  },
  {
    "Ip": "192.168.0.54",
    "Nombre": "AGRIMENSURA31",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:49:2C",
    "Activo": true
  },
  {
    "Ip": "192.168.0.55",
    "Nombre": null,
    "Fabricante": "Dell Inc.",
    "Mac": "08:92:04:DF:4C:4F",
    "Activo": true
  },
  {
    "Ip": "192.168.0.56",
    "Nombre": "Inmuebles19",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:7D:EA",
    "Activo": true
  },
  {
    "Ip": "192.168.0.58",
    "Nombre": "Agrimensura27",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:76:70",
    "Activo": true
  },
  {
    "Ip": "192.168.0.59",
    "Nombre": "Agrimensura28",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:4B:68",
    "Activo": true
  },
  {
    "Ip": "192.168.0.60",
    "Nombre": null,
    "Fabricante": null,
    "Mac": "E8:9C:25:35:4C:0C",
    "Activo": true
  },
  {
    "Ip": "192.168.0.61",
    "Nombre": null,
    "Fabricante": "Dell Inc.",
    "Mac": "54:BF:64:65:9B:32",
    "Activo": true
  },
  {
    "Ip": "192.168.0.62",
    "Nombre": "Cartografia04",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:76:68",
    "Activo": true
  },
  {
    "Ip": "192.168.0.64",
    "Nombre": "INMUEBLES03",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:4B:D5",
    "Activo": true
  },
  {
    "Ip": "192.168.0.66",
    "Nombre": "Valuacion01",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:34:9C",
    "Activo": true
  },
  {
    "Ip": "192.168.0.67",
    "Nombre": "VALUACION18",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "58:11:22:4C:38:C2",
    "Activo": true
  },
  {
    "Ip": "192.168.0.69",
    "Nombre": "MEntrada1",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:4B:B7",
    "Activo": true
  },
  {
    "Ip": "192.168.0.71",
    "Nombre": "Admin02",
    "Fabricante": null,
    "Mac": "E8:9C:25:35:52:D9",
    "Activo": true
  },
  {
    "Ip": "192.168.0.72",
    "Nombre": null,
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:49:14",
    "Activo": true
  },
  {
    "Ip": "192.168.0.73",
    "Nombre": "Secretaria15",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:4E:17",
    "Activo": true
  },
  {
    "Ip": "192.168.0.75",
    "Nombre": "Valuacion10",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:4B:EE",
    "Activo": false
  },
  {
    "Ip": "192.168.0.76",
    "Nombre": "SISTEMAS13",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:7E:31",
    "Activo": true
  },
  {
    "Ip": "192.168.0.77",
    "Nombre": "VALUACION12",
    "Fabricante": null,
    "Mac": "E8:9C:25:35:49:44",
    "Activo": true
  },
  {
    "Ip": "192.168.0.78",
    "Nombre": "ADMIN05",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "50:E5:49:E8:21:12",
    "Activo": true
  },
  {
    "Ip": "192.168.0.79",
    "Nombre": null,
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:48:F3",
    "Activo": true
  },
  {
    "Ip": "192.168.0.81",
    "Nombre": null,
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:82:2B:34",
    "Activo": true
  },
  {
    "Ip": "192.168.0.82",
    "Nombre": "ASESORIA09",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:48:67",
    "Activo": true
  },
  {
    "Ip": "192.168.0.83",
    "Nombre": "Valuacion20",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C7:25:78",
    "Activo": true
  },
  {
    "Ip": "192.168.0.84",
    "Nombre": "CONSOLA-UNIFI",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:4C:48",
    "Activo": true
  },
  {
    "Ip": "192.168.0.85",
    "Nombre": "DIRECCION02",
    "Fabricante": "Dell Inc.",
    "Mac": "54:BF:64:65:8D:DD",
    "Activo": true
  },
  {
    "Ip": "192.168.0.86",
    "Nombre": "Valuacion52",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "50:EB:F6:B7:EA:04",
    "Activo": true
  },
  {
    "Ip": "192.168.0.87",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:31:19",
    "Activo": true
  },
  {
    "Ip": "192.168.0.88",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:76:C2",
    "Activo": true
  },
  {
    "Ip": "192.168.0.89",
    "Nombre": "Valuacion26",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "10:C3:7B:6B:B0:79",
    "Activo": true
  },
  {
    "Ip": "192.168.0.90",
    "Nombre": "Asesoria07",
    "Fabricante": "Dell Inc.",
    "Mac": "54:BF:64:65:99:E1",
    "Activo": true
  },
  {
    "Ip": "192.168.0.91",
    "Nombre": null,
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "2C:56:DC:3B:81:1B",
    "Activo": true
  },
  {
    "Ip": "192.168.0.92",
    "Nombre": "VALUACION30",
    "Fabricante": null,
    "Mac": "E8:9C:25:35:4E:E4",
    "Activo": true
  },
  {
    "Ip": "192.168.0.93",
    "Nombre": "Valuacion31",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:49:17",
    "Activo": true
  },
  {
    "Ip": "192.168.0.95",
    "Nombre": "Personal05",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C7:24:92",
    "Activo": true
  },
  {
    "Ip": "192.168.0.96",
    "Nombre": "Sistemas03",
    "Fabricante": "Dell Inc.",
    "Mac": "54:BF:64:65:8D:A1",
    "Activo": true
  },
  {
    "Ip": "192.168.0.97",
    "Nombre": "REGIMEN16",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:46:82:06",
    "Activo": true
  },
  {
    "Ip": "192.168.0.98",
    "Nombre": "inmuebles26",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:27:68:8F",
    "Activo": true
  },
  {
    "Ip": "192.168.0.99",
    "Nombre": "SERVER99",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:4B:37",
    "Activo": true
  },
  {
    "Ip": "192.168.0.100",
    "Nombre": null,
    "Fabricante": null,
    "Mac": "E8:9C:25:35:52:6D",
    "Activo": true
  },
  {
    "Ip": "192.168.0.102",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:34:C2",
    "Activo": true
  },
  {
    "Ip": "192.168.0.103",
    "Nombre": "Valuacion50",
    "Fabricante": "Dell Inc.",
    "Mac": "08:92:04:DF:4C:56",
    "Activo": true
  },
  {
    "Ip": "192.168.0.104",
    "Nombre": "Regimen17",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:34:CB",
    "Activo": true
  },
  {
    "Ip": "192.168.0.105",
    "Nombre": "Regimen27",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:48:73",
    "Activo": true
  },
  {
    "Ip": "192.168.0.106",
    "Nombre": null,
    "Fabricante": null,
    "Mac": "10:FF:E0:EE:75:B9",
    "Activo": true
  },
  {
    "Ip": "192.168.0.107",
    "Nombre": "Cartografia11",
    "Fabricante": "Dell Inc.",
    "Mac": "54:BF:64:65:9D:98",
    "Activo": true
  },
  {
    "Ip": "192.168.0.109",
    "Nombre": "CARTOGRAFIA10",
    "Fabricante": null,
    "Mac": "10:FF:E0:FA:BF:EA",
    "Activo": true
  },
  {
    "Ip": "192.168.0.110",
    "Nombre": "Agrimensura24",
    "Fabricante": null,
    "Mac": "E8:9C:25:35:52:3A",
    "Activo": true
  },
  {
    "Ip": "192.168.0.111",
    "Nombre": "Regimen07",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:7E:38",
    "Activo": true
  },
  {
    "Ip": "192.168.0.112",
    "Nombre": "valuacion08",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:49:2A",
    "Activo": false
  },
  {
    "Ip": "192.168.0.113",
    "Nombre": null,
    "Fabricante": null,
    "Mac": "E8:9C:25:35:4B:EC",
    "Activo": true
  },
  {
    "Ip": "192.168.0.114",
    "Nombre": "SECRETARIA08",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:4C:4D",
    "Activo": true
  },
  {
    "Ip": "192.168.0.115",
    "Nombre": "Secretaria16",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:80:50",
    "Activo": false
  },
  {
    "Ip": "192.168.0.116",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:34:82",
    "Activo": true
  },
  {
    "Ip": "192.168.0.117",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:78:5D",
    "Activo": true
  },
  {
    "Ip": "192.168.0.118",
    "Nombre": "Regimen14",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:4D:36",
    "Activo": true
  },
  {
    "Ip": "192.168.0.119",
    "Nombre": "Asesoria05",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:48:66",
    "Activo": true
  },
  {
    "Ip": "192.168.0.121",
    "Nombre": "Regimen18",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:7A:A5",
    "Activo": true
  },
  {
    "Ip": "192.168.0.123",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:48:70",
    "Activo": true
  },
  {
    "Ip": "192.168.0.124",
    "Nombre": "AGRIMENSURA21",
    "Fabricante": null,
    "Mac": "10:FF:E0:FA:BA:34",
    "Activo": true
  },
  {
    "Ip": "192.168.0.125",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:76:C4",
    "Activo": true
  },
  {
    "Ip": "192.168.0.126",
    "Nombre": "REGIMEN23",
    "Fabricante": null,
    "Mac": "E8:9C:25:35:52:3B",
    "Activo": true
  },
  {
    "Ip": "192.168.0.127",
    "Nombre": "Sistemas23.catastro.lan",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:49:52",
    "Activo": true
  },
  {
    "Ip": "192.168.0.129",
    "Nombre": "INMUEBLES21",
    "Fabricante": null,
    "Mac": "E8:9C:25:35:52:97",
    "Activo": true
  },
  {
    "Ip": "192.168.0.131",
    "Nombre": "Secretaria04",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "A8:5E:45:2F:3C:84",
    "Activo": true
  },
  {
    "Ip": "192.168.0.132",
    "Nombre": "INMUEBLES10",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "50:E5:49:E7:D1:4A",
    "Activo": true
  },
  {
    "Ip": "192.168.0.133",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C7:25:30",
    "Activo": true
  },
  {
    "Ip": "192.168.0.134",
    "Nombre": "Dominial02",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:48:71",
    "Activo": true
  },
  {
    "Ip": "192.168.0.135",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:33:CE",
    "Activo": true
  },
  {
    "Ip": "192.168.0.136",
    "Nombre": "MENTRADA02",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:4D:4A",
    "Activo": true
  },
  {
    "Ip": "192.168.0.137",
    "Nombre": "Valuacion13",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:4D:33",
    "Activo": true
  },
  {
    "Ip": "192.168.0.138",
    "Nombre": "INMUEBLES08",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:34:8D",
    "Activo": true
  },
  {
    "Ip": "192.168.0.139",
    "Nombre": "INMUEBLES16",
    "Fabricante": null,
    "Mac": "E8:9C:25:35:52:7C",
    "Activo": true
  },
  {
    "Ip": "192.168.0.140",
    "Nombre": "mentrada09",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:34:83",
    "Activo": true
  },
  {
    "Ip": "192.168.0.141",
    "Nombre": "Agrimensura60",
    "Fabricante": "Dell Inc.",
    "Mac": "48:4D:7E:F1:C0:A1",
    "Activo": true
  },
  {
    "Ip": "192.168.0.142",
    "Nombre": "Inmuebles14",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:27:68:E8",
    "Activo": true
  },
  {
    "Ip": "192.168.0.144",
    "Nombre": "INMUEBLES09",
    "Fabricante": null,
    "Mac": "10:FF:E0:FA:B9:94",
    "Activo": true
  },
  {
    "Ip": "192.168.0.145",
    "Nombre": "Inmuebles20",
    "Fabricante": null,
    "Mac": "E8:9C:25:35:4B:6D",
    "Activo": true
  },
  {
    "Ip": "192.168.0.146",
    "Nombre": null,
    "Fabricante": null,
    "Mac": "10:FF:E0:FA:B9:81",
    "Activo": true
  },
  {
    "Ip": "192.168.0.147",
    "Nombre": "Agrimensura05",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:77:EB",
    "Activo": true
  },
  {
    "Ip": "192.168.0.148",
    "Nombre": null,
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:48:FF",
    "Activo": true
  },
  {
    "Ip": "192.168.0.149",
    "Nombre": "Mentrada06",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:34:BB",
    "Activo": true
  },
  {
    "Ip": "192.168.0.150",
    "Nombre": "Inmuebles22",
    "Fabricante": null,
    "Mac": "10:FF:E0:FA:BE:7D",
    "Activo": true
  },
  {
    "Ip": "192.168.0.151",
    "Nombre": "AGRIMENSURA600",
    "Fabricante": "Hewlett Packard",
    "Mac": "2C:27:D7:14:5D:4A",
    "Activo": true
  },
  {
    "Ip": "192.168.0.152",
    "Nombre": "Inmuebles13",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:7F:B9",
    "Activo": true
  },
  {
    "Ip": "192.168.0.153",
    "Nombre": null,
    "Fabricante": null,
    "Mac": "E8:9C:25:35:4D:61",
    "Activo": true
  },
  {
    "Ip": "192.168.0.154",
    "Nombre": "CARTOGRAFIA19",
    "Fabricante": null,
    "Mac": "E8:9C:25:35:52:20",
    "Activo": true
  },
  {
    "Ip": "192.168.0.155",
    "Nombre": null,
    "Fabricante": "Dell Inc.",
    "Mac": "08:92:04:DF:49:E5",
    "Activo": true
  },
  {
    "Ip": "192.168.0.157",
    "Nombre": "CARTOGRAFIA06",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:7A:CD",
    "Activo": true
  },
  {
    "Ip": "192.168.0.158",
    "Nombre": null,
    "Fabricante": null,
    "Mac": "E8:9C:25:35:52:C5",
    "Activo": true
  },
  {
    "Ip": "192.168.0.159",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:7D:D4",
    "Activo": true
  },
  {
    "Ip": "192.168.0.160",
    "Nombre": "Agrimensura07",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:48:FB",
    "Activo": true
  },
  {
    "Ip": "192.168.0.161",
    "Nombre": "CARTOGRAFIA17",
    "Fabricante": "Dell Inc.",
    "Mac": "08:92:04:DF:44:07",
    "Activo": true
  },
  {
    "Ip": "192.168.0.162",
    "Nombre": null,
    "Fabricante": null,
    "Mac": "E8:9C:25:35:51:B9",
    "Activo": true
  },
  {
    "Ip": "192.168.0.163",
    "Nombre": "agrimensura16",
    "Fabricante": "Dell Inc.",
    "Mac": "54:BF:64:65:8E:C5",
    "Activo": true
  },
  {
    "Ip": "192.168.0.164",
    "Nombre": "Cartografia13",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:48:FA",
    "Activo": true
  },
  {
    "Ip": "192.168.0.165",
    "Nombre": "CARTOGRAFIA14",
    "Fabricante": "Dell Inc.",
    "Mac": "08:92:04:DF:4C:67",
    "Activo": true
  },
  {
    "Ip": "192.168.0.167",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C7:25:2C",
    "Activo": true
  },
  {
    "Ip": "192.168.0.169",
    "Nombre": "Personal04",
    "Fabricante": "TP-Link Corporation Limited",
    "Mac": "B4:B0:24:80:A4:0C",
    "Activo": true
  },
  {
    "Ip": "192.168.0.170",
    "Nombre": "Secretaria02",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:4B:DB",
    "Activo": true
  },
  {
    "Ip": "192.168.0.172",
    "Nombre": "Secretaria17",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:77:EE",
    "Activo": true
  },
  {
    "Ip": "192.168.0.173",
    "Nombre": "Inmuebles11",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:4A:BB",
    "Activo": true
  },
  {
    "Ip": "192.168.0.174",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:78:7A",
    "Activo": true
  },
  {
    "Ip": "192.168.0.175",
    "Nombre": "Secretaria12",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:78:43",
    "Activo": true
  },
  {
    "Ip": "192.168.0.177",
    "Nombre": "Inmuebles25",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:78:63",
    "Activo": true
  },
  {
    "Ip": "192.168.0.179",
    "Nombre": "Inmuebles04",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:31:38",
    "Activo": true
  },
  {
    "Ip": "192.168.0.180",
    "Nombre": "Asesoria06",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:48:69",
    "Activo": true
  },
  {
    "Ip": "192.168.0.181",
    "Nombre": null,
    "Fabricante": "TP-LINK TECHNOLOGIES CO.,LTD.",
    "Mac": "F4:F2:6D:05:06:F8",
    "Activo": true
  },
  {
    "Ip": "192.168.0.182",
    "Nombre": null,
    "Fabricante": "TP-LINK TECHNOLOGIES CO.,LTD.",
    "Mac": "F4:F2:6D:05:06:F8",
    "Activo": true
  },
  {
    "Ip": "192.168.0.183",
    "Nombre": "Catastro",
    "Fabricante": "Cisco Systems, Inc",
    "Mac": "BC:67:1C:4D:61:C1",
    "Activo": true
  },
  {
    "Ip": "192.168.0.184",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:7E:32",
    "Activo": true
  },
  {
    "Ip": "192.168.0.185",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:77:EF",
    "Activo": true
  },
  {
    "Ip": "192.168.0.187",
    "Nombre": "Sistemas44",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:49:10",
    "Activo": true
  },
  {
    "Ip": "192.168.0.188",
    "Nombre": null,
    "Fabricante": "TP-LINK TECHNOLOGIES CO.,LTD.",
    "Mac": "F4:F2:6D:05:06:F8",
    "Activo": true
  },
  {
    "Ip": "192.168.0.189",
    "Nombre": "Sistemas06",
    "Fabricante": "Dell Inc.",
    "Mac": "54:BF:64:65:97:AE",
    "Activo": true
  },
  {
    "Ip": "192.168.0.190",
    "Nombre": "Entrada01",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:4B:F9",
    "Activo": true
  },
  {
    "Ip": "192.168.0.193",
    "Nombre": "admin03",
    "Fabricante": "ASRock Incorporation",
    "Mac": "D0:50:99:2C:0B:AA",
    "Activo": true
  },
  {
    "Ip": "192.168.0.194",
    "Nombre": null,
    "Fabricante": "Hon Hai Precision Ind. Co., Ltd.",
    "Mac": "F4:6B:8C:5E:20:8B",
    "Activo": true
  },
  {
    "Ip": "192.168.0.195",
    "Nombre": "AGRIMENSURA29",
    "Fabricante": "Dell Inc.",
    "Mac": "54:BF:64:65:9D:FD",
    "Activo": true
  },
  {
    "Ip": "192.168.0.196",
    "Nombre": "Valuacion51",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "AC:22:0B:2C:01:BE",
    "Activo": true
  },
  {
    "Ip": "192.168.0.197",
    "Nombre": "REGIMEN10",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:4B:FF",
    "Activo": true
  },
  {
    "Ip": "192.168.0.199",
    "Nombre": "GESTION02",
    "Fabricante": null,
    "Mac": "E8:9C:25:35:52:53",
    "Activo": true
  },
  {
    "Ip": "192.168.0.201",
    "Nombre": "mentrada05",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:48:5E",
    "Activo": true
  },
  {
    "Ip": "192.168.0.202",
    "Nombre": "EP414100",
    "Fabricante": "Seiko Epson Corporation",
    "Mac": "B0:E8:92:80:DD:0F",
    "Activo": true
  },
  {
    "Ip": "192.168.0.203",
    "Nombre": "Regimen20",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:4B:F5",
    "Activo": true
  },
  {
    "Ip": "192.168.0.204",
    "Nombre": null,
    "Fabricante": "KONICA MINOLTA HOLDINGS, INC.",
    "Mac": "00:20:6B:DE:F8:EB",
    "Activo": true
  },
  {
    "Ip": "192.168.0.205",
    "Nombre": "Valuacion25",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:47:C0",
    "Activo": true
  },
  {
    "Ip": "192.168.0.206",
    "Nombre": "INMUEBLES15",
    "Fabricante": "Dell Inc.",
    "Mac": "54:BF:64:65:9D:E7",
    "Activo": true
  },
  {
    "Ip": "192.168.0.208",
    "Nombre": "MENTRADA08",
    "Fabricante": null,
    "Mac": "E8:9C:25:35:4B:EB",
    "Activo": true
  },
  {
    "Ip": "192.168.0.209",
    "Nombre": "HPSCANNER",
    "Fabricante": "Advantech Technology (CHINA) Co., Ltd.",
    "Mac": "C4:00:AD:19:E4:AA",
    "Activo": true
  },
  {
    "Ip": "192.168.0.210",
    "Nombre": "STORAGE-BACKUP",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:48:19",
    "Activo": true
  },
  {
    "Ip": "192.168.0.212",
    "Nombre": "Regimen28",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C7:25:2F",
    "Activo": true
  },
  {
    "Ip": "192.168.0.213",
    "Nombre": "RNP5838799DDBDC",
    "Fabricante": "RICOH COMPANY, LTD.",
    "Mac": "58:38:79:9D:DB:DC",
    "Activo": true
  },
  {
    "Ip": "192.168.0.214",
    "Nombre": "VALUACION19",
    "Fabricante": null,
    "Mac": "E8:9C:25:35:52:3D",
    "Activo": true
  },
  {
    "Ip": "192.168.0.215",
    "Nombre": "SECRETARIA07",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:76:BF",
    "Activo": true
  },
  {
    "Ip": "192.168.0.216",
    "Nombre": "Mentrada04",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C7:25:2B",
    "Activo": true
  },
  {
    "Ip": "192.168.0.217",
    "Nombre": "VALUACION27",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:48:E4",
    "Activo": true
  },
  {
    "Ip": "192.168.0.218",
    "Nombre": "Valuacion04",
    "Fabricante": "Dell Inc.",
    "Mac": "08:92:04:DF:49:F5",
    "Activo": true
  },
  {
    "Ip": "192.168.0.219",
    "Nombre": "Mentrada03",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "50:E5:49:E7:CE:85",
    "Activo": true
  },
  {
    "Ip": "192.168.0.220",
    "Nombre": "Inmuebles02",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:34:BD",
    "Activo": true
  },
  {
    "Ip": "192.168.0.221",
    "Nombre": "BACKUP-SERVER",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:34:B0",
    "Activo": true
  },
  {
    "Ip": "192.168.0.222",
    "Nombre": "HPF88D87",
    "Fabricante": "HP Inc.",
    "Mac": "04:0E:3C:F8:8D:87",
    "Activo": true
  },
  {
    "Ip": "192.168.0.224",
    "Nombre": "PERSONAL03",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:4D:F8",
    "Activo": true
  },
  {
    "Ip": "192.168.0.226",
    "Nombre": "Secretaria09",
    "Fabricante": "ASRock Incorporation",
    "Mac": "D0:50:99:23:67:11",
    "Activo": true
  },
  {
    "Ip": "192.168.0.227",
    "Nombre": "Valuacion21",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:47:EB",
    "Activo": true
  },
  {
    "Ip": "192.168.0.228",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:7F:B6",
    "Activo": true
  },
  {
    "Ip": "192.168.0.229",
    "Nombre": "Secretaria13",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:4B:E5",
    "Activo": true
  },
  {
    "Ip": "192.168.0.230",
    "Nombre": "Dominial03",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "D4:5D:64:38:9F:42",
    "Activo": true
  },
  {
    "Ip": "192.168.0.231",
    "Nombre": "SUMARIOS07",
    "Fabricante": null,
    "Mac": "E8:9C:25:35:51:DC",
    "Activo": false
  },
  {
    "Ip": "192.168.0.232",
    "Nombre": "DIRECCION01",
    "Fabricante": null,
    "Mac": "10:FF:E0:EE:74:6C",
    "Activo": true
  },
  {
    "Ip": "192.168.0.233",
    "Nombre": "ebeeplus",
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:74:14",
    "Activo": true
  },
  {
    "Ip": "192.168.0.234",
    "Nombre": null,
    "Fabricante": "Private",
    "Mac": "00:17:61:12:BA:FB",
    "Activo": true
  },
  {
    "Ip": "192.168.0.235",
    "Nombre": "Municipios01",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:7A:C7",
    "Activo": true
  },
  {
    "Ip": "192.168.0.236",
    "Nombre": "Inmuebles17",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "FC:AA:14:C6:4B:CF",
    "Activo": true
  },
  {
    "Ip": "192.168.0.237",
    "Nombre": null,
    "Fabricante": null,
    "Mac": "10:FF:E0:FA:C0:19",
    "Activo": true
  },
  {
    "Ip": "192.168.0.239",
    "Nombre": null,
    "Fabricante": "Dell Inc.",
    "Mac": "08:92:04:DF:4A:EA",
    "Activo": true
  },
  {
    "Ip": "192.168.0.240",
    "Nombre": null,
    "Fabricante": "Private",
    "Mac": "00:17:61:12:BB:02",
    "Activo": true
  },
  {
    "Ip": "192.168.0.241",
    "Nombre": null,
    "Fabricante": "Private",
    "Mac": "00:17:61:12:8F:D9",
    "Activo": true
  },
  {
    "Ip": "192.168.0.242",
    "Nombre": null,
    "Fabricante": "Private",
    "Mac": "00:17:61:12:BA:FA",
    "Activo": true
  },
  {
    "Ip": "192.168.0.243",
    "Nombre": null,
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:A3:B6:F8",
    "Activo": true
  },
  {
    "Ip": "192.168.0.246",
    "Nombre": null,
    "Fabricante": null,
    "Mac": "40:5B:7F:39:A9:10",
    "Activo": true
  },
  {
    "Ip": "192.168.0.247",
    "Nombre": null,
    "Fabricante": null,
    "Mac": "6C:3C:8C:96:E0:C4",
    "Activo": true
  },
  {
    "Ip": "192.168.0.248",
    "Nombre": null,
    "Fabricante": "Dell Inc.",
    "Mac": "80:18:44:EB:D9:5C",
    "Activo": true
  },
  {
    "Ip": "192.168.0.249",
    "Nombre": "iDRAC-7ZD29N2",
    "Fabricante": "Dell Inc.",
    "Mac": "D0:94:66:3F:87:69",
    "Activo": true
  },
  {
    "Ip": "192.168.0.250",
    "Nombre": null,
    "Fabricante": "Dell Inc.",
    "Mac": "80:18:44:EB:86:A8",
    "Activo": true
  },
  {
    "Ip": "192.168.0.251",
    "Nombre": "iDRAC-7ZD19N2",
    "Fabricante": "Dell Inc.",
    "Mac": "D0:94:66:3F:6A:20",
    "Activo": true
  },
  {
    "Ip": "192.168.0.252",
    "Nombre": "idrac",
    "Fabricante": "Dell Inc.",
    "Mac": "20:47:47:7B:0F:AE",
    "Activo": true
  },
  {
    "Ip": "192.168.1.1",
    "Nombre": null,
    "Fabricante": null,
    "Mac": null,
    "Activo": true
  },
  {
    "Ip": "192.168.1.17",
    "Nombre": null,
    "Fabricante": null,
    "Mac": null,
    "Activo": true
  },
  {
    "Ip": "192.168.1.18",
    "Nombre": null,
    "Fabricante": "Hewlett Packard",
    "Mac": "5C:8A:38:9F:1A:32",
    "Activo": true
  },
  {
    "Ip": "192.168.1.19",
    "Nombre": null,
    "Fabricante": "Hewlett Packard",
    "Mac": "5C:8A:38:9E:80:52",
    "Activo": true
  },
  {
    "Ip": "192.168.1.20",
    "Nombre": null,
    "Fabricante": "Hewlett Packard",
    "Mac": "44:31:92:9E:54:20",
    "Activo": true
  },
  {
    "Ip": "192.168.1.23",
    "Nombre": null,
    "Fabricante": "Hewlett Packard",
    "Mac": "5C:8A:38:9F:19:92",
    "Activo": true
  },
  {
    "Ip": "192.168.1.24",
    "Nombre": "Switch Agrimensura",
    "Fabricante": null,
    "Mac": null,
    "Activo": true
  },
  {
    "Ip": "192.168.1.26",
    "Nombre": "Switch1 - Inmuebles",
    "Fabricante": "Hewlett Packard",
    "Mac": "B8:AF:67:9B:8E:DD",
    "Activo": true
  },
  {
    "Ip": "192.168.1.28",
    "Nombre": null,
    "Fabricante": null,
    "Mac": null,
    "Activo": true
  },
  {
    "Ip": "192.168.1.29",
    "Nombre": null,
    "Fabricante": "Hewlett Packard",
    "Mac": "78:48:59:7C:E6:33",
    "Activo": false
  },
  {
    "Ip": "192.168.1.30",
    "Nombre": null,
    "Fabricante": null,
    "Mac": null,
    "Activo": true
  },
  {
    "Ip": "192.168.1.32",
    "Nombre": null,
    "Fabricante": null,
    "Mac": null,
    "Activo": true
  },
  {
    "Ip": "192.168.1.33",
    "Nombre": null,
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:6A:EF:22",
    "Activo": true
  },
  {
    "Ip": "192.168.1.34",
    "Nombre": null,
    "Fabricante": "VMware, Inc.",
    "Mac": "00:50:56:6E:BE:28",
    "Activo": true
  },
  {
    "Ip": "192.168.1.35",
    "Nombre": null,
    "Fabricante": "Dell Inc.",
    "Mac": "D0:94:66:5F:47:14",
    "Activo": true
  },
  {
    "Ip": "192.168.1.36",
    "Nombre": null,
    "Fabricante": "Dell Inc.",
    "Mac": "D0:94:66:5F:47:14",
    "Activo": true
  },
  {
    "Ip": "192.168.1.37",
    "Nombre": null,
    "Fabricante": "Dell Inc.",
    "Mac": "D0:94:66:5F:48:A4",
    "Activo": true
  },
  {
    "Ip": "192.168.1.38",
    "Nombre": "DS_300B",
    "Fabricante": "Brocade Communications Systems LLC",
    "Mac": "88:94:71:28:1B:70",
    "Activo": true
  },
  {
    "Ip": "192.168.1.40",
    "Nombre": "SWITCH RD",
    "Fabricante": null,
    "Mac": null,
    "Activo": true
  },
  {
    "Ip": "192.168.1.41",
    "Nombre": "apc2DC91D",
    "Fabricante": "APC by Schneider Electric",
    "Mac": "28:29:86:2D:C9:1D",
    "Activo": true
  },
  {
    "Ip": "192.168.1.42",
    "Nombre": null,
    "Fabricante": "Eaton Corporation",
    "Mac": "00:20:85:C1:DA:69",
    "Activo": true
  },
  {
    "Ip": "192.168.1.44",
    "Nombre": null,
    "Fabricante": "Dell Inc.",
    "Mac": "E4:F0:04:A4:83:BD",
    "Activo": true
  },
  {
    "Ip": "192.168.1.45",
    "Nombre": "DS_6610B",
    "Fabricante": "Brocade Communications Systems LLC",
    "Mac": "88:94:71:FC:FC:CE",
    "Activo": true
  },
  {
    "Ip": "192.168.1.51",
    "Nombre": null,
    "Fabricante": "Aruba, a Hewlett Packard Enterprise Company",
    "Mac": "44:5B:ED:12:3E:25",
    "Activo": true
  },
  {
    "Ip": "192.168.1.60",
    "Nombre": "MENTRADA02",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:4D:4A",
    "Activo": true
  },
  {
    "Ip": "192.168.1.73",
    "Nombre": "Sistemas10",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:31:3D",
    "Activo": true
  },
  {
    "Ip": "192.168.1.100",
    "Nombre": "Sistemas19",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "D8:5E:D3:D8:78:40",
    "Activo": true
  },
  {
    "Ip": "192.168.1.102",
    "Nombre": "PANAMA",
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "94:DE:80:B4:33:01",
    "Activo": true
  },
  {
    "Ip": "192.168.1.111",
    "Nombre": "STORAGE-BACKUP",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:48:19",
    "Activo": true
  },
  {
    "Ip": "192.168.1.112",
    "Nombre": null,
    "Fabricante": "GIGA-BYTE TECHNOLOGY CO.,LTD.",
    "Mac": "50:E5:49:E8:B5:31",
    "Activo": true
  },
  {
    "Ip": "192.168.1.115",
    "Nombre": null,
    "Fabricante": null,
    "Mac": null,
    "Activo": true
  },
  {
    "Ip": "192.168.1.119",
    "Nombre": "Sistemas23.catastro.lan",
    "Fabricante": "ASUSTek COMPUTER INC.",
    "Mac": "1C:87:2C:44:49:52",
    "Activo": true
  },
  {
    "Ip": "192.168.1.120",
    "Nombre": "Sistemas07",
    "Fabricante": "Dell Inc.",
    "Mac": "54:BF:64:65:8E:5B",
    "Activo": true
  },
  {
    "Ip": "192.168.1.140",
    "Nombre": "Sistemas03",
    "Fabricante": "Dell Inc.",
    "Mac": "54:BF:64:65:8D:A1",
    "Activo": true
  },
  {
    "Ip": "192.168.1.151",
    "Nombre": "AGRIMENSURA600",
    "Fabricante": "Hewlett Packard",
    "Mac": "2C:27:D7:14:5D:4A",
    "Activo": true
  },
  {
    "Ip": "192.168.1.180",
    "Nombre": "Catastro",
    "Fabricante": "Cisco Systems, Inc",
    "Mac": "BC:67:1C:4D:61:C1",
    "Activo": true
  },
  {
    "Ip": "192.168.1.182",
    "Nombre": null,
    "Fabricante": "TP-LINK TECHNOLOGIES CO.,LTD.",
    "Mac": "F4:F2:6D:05:06:F8",
    "Activo": true
  },
  {
    "Ip": "192.168.1.212",
    "Nombre": "T1700",
    "Fabricante": "HP Inc.",
    "Mac": "48:9E:BD:FD:09:1C",
    "Activo": true
  },
  {
    "Ip": "192.168.1.216",
    "Nombre": "Switch Sistemas",
    "Fabricante": "Hewlett Packard Enterprise",
    "Mac": "20:67:7C:4C:44:A8",
    "Activo": true
  },
  {
    "Ip": "192.168.1.217",
    "Nombre": "Switch HP1920s (H)",
    "Fabricante": "Hewlett Packard Enterprise",
    "Mac": "20:67:7C:4E:9E:54",
    "Activo": true
  },
  {
    "Ip": "192.168.1.218",
    "Nombre": null,
    "Fabricante": null,
    "Mac": null,
    "Activo": true
  },
  {
    "Ip": "192.168.1.219",
    "Nombre": "Guillo-Switch3com",
    "Fabricante": "3COM EUROPE LTD",
    "Mac": "00:24:73:CA:67:AF",
    "Activo": true
  },
  {
    "Ip": "192.168.1.250",
    "Nombre": "Agrimensura60",
    "Fabricante": "Dell Inc.",
    "Mac": "48:4D:7E:F1:C0:A1",
    "Activo": true
  }
]
""";

    /// <summary>
    /// Convierte el escaneo fijo en filas tipadas para el inicializador.
    /// </summary>
    /// <returns>Hosts detectados; cambiar el JSON altera qué datos se completan en el inventario.</returns>
    public static IReadOnlyList<FilaEscaneoLan> ObtenerFilas()
    {
        // filasEscaneo deserializa el JSON sin completar campos ausentes.
        var filasEscaneo = JsonSerializer.Deserialize<List<FilaEscaneoLan>>(DatosJson);
        return filasEscaneo ?? [];
    }
}

/// <summary>Representa un host detectado por el escáner de red.</summary>
internal sealed class FilaEscaneoLan
{
    /// <summary>Dirección IPv4 del host; cambiarla cambia con qué ficha se empareja.</summary>
    public string Ip { get; set; } = string.Empty;
    /// <summary>Nombre de host resuelto; nulo si el escáner solo conoce la IP.</summary>
    public string? Nombre { get; set; }
    /// <summary>Fabricante según el OUI de la MAC; nulo si el escáner no lo identificó.</summary>
    public string? Fabricante { get; set; }
    /// <summary>MAC en formato AA:BB:CC:DD:EE:FF; nula si el escáner no la obtuvo.</summary>
    public string? Mac { get; set; }
    /// <summary>Verdadero si el host respondía durante el escaneo; falso si figuraba como inactivo.</summary>
    public bool Activo { get; set; }
}
