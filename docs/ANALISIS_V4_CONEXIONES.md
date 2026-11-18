# Análisis de conexiones — DIAGRAMA RED V4.0 y escaneo de la LAN

**Fuentes:** `DIAGRAMA RED V4.0.vsdx` y `datos_ip.html` (escaneo de la LAN con 248 hosts), recibidos el 17 de noviembre de 2026. Los archivos fuente no se modificaron.

## Lectura del V4

El paquete contiene 22 páginas y 30 libros Excel incrustados (uno menos que el V3.5). Los libros no estaban cifrados: se abren directamente con cualquier lector de `.xlsx`. Se cotejaron por página, libro, fila y puerto contra el catálogo del V3.5 que ya usa la aplicación.

- Las tablas de puertos del V4 **no cambian** respecto del V3.5: mismos destinos e IP en las 13 tablas de área, el switch de 8 puertos y los 5 switches de rack. Se mantienen las normalizaciones previas (R0 puerto 24 ↔ R2 puerto 23, `192.168.1.24` para el 3Com, `10.0.1.1` para el ProCurve, S4148U puerto 21 ↔ R770 LAN P2).
- Lo nuevo del V4 son los **equipos dibujados** en las páginas RACK 1 y ESQUEMA CONEXIÓN INTERNET: switch Intellinet de 8 puertos, módem Claro y módem Personal, los dos equipos pfSense (PF y F), dos Dell R740 (O y P) y sus enlaces rotulados.
- Las tablas SFP de los switches traen filas fantasma (puertos 11, 12, 14, 15, 17, 18 y 22) que se colaban como si fueran más de cuatro SFP.

## Regla de cuatro SFP

Por indicación del usuario, todos los switches del Visio, salvo el Dell S4148U-ON (perfil propio: 48 SFP+, 2 QSFP+ y 4 QSFP28), tienen **4 SFP**. La actualización guarda `CantidadPuertosSfp = 4` en esos switches, retira las filas SFP sobrantes que no tengan enlace ni relación, completa las jaulas que falten y numera las etiquetas 25–28 del Visio como SFP 1–4 en todos los switches (antes solo ocurría en el HP 1920). El HP 5500 no importaba sus SFP porque su hoja Excel4 usaba el nombre «Switch HP 5500 (D)»; se corrigió en el catálogo. Quedan fuera de la regla, a confirmar: el Cisco Catalyst 3560 (el V4 lo dibuja con 8 bocas), el switch de 8 puertos de Inmuebles y el Intellinet.

## Equipos y enlaces del esquema de internet

| Equipo | Dato del V4 | Enlaces incorporados |
|---|---|---|
| Intellinet 8 puertos | Switch de 8 puertos, sin modelo ni IP | P-8 ↔ Módem Claro (confirmado); P-1 ↔ PF WAN 3 y P-5 ↔ F WAN 3 (a verificar) |
| PF-SENSE Subdominio | PF, `192.168.0.9` (Excel5 puerto 7) | HP 1920 puerto 7 ↔ LAN (confirmado); WAN ↔ Módem Personal (a verificar) |
| PF-SENSE INTERNET | F, `192.168.0.182` (Excel5 puerto 10) | HP 1920 puerto 10 ↔ LAN (confirmado) |
| Módem Claro, Módem Personal | Rótulos del esquema | Ver arriba |
| Dell R740 (O) y (P) | Dos servidores dibujados | Sin enlaces: los iDRAC `192.168.0.249` y `.251` están en el HP 5500 (puertos 4 y 3), pero el Visio no dice cuál iDRAC es de cuál R740 |

Los enlaces HP 1920 puerto 7 y 10 reemplazan las relaciones parciales que ya existían para esos puertos. No se cargaron pacheras, UPS ni los PC LINUX/PC CÁMARAS como fichas independientes: el rack rotula la PC de F como «PC CAMARAS» y el esquema como «PC LINUX 2.6»; hay que confirmar cuál es.

## Escaneo de la LAN

Las filas se emparejan primero por IP y, si no hay ficha con esa IP, por nombre de host (se ignora el sufijo `.catastro.lan`). Reglas:

1. Solo se completan campos vacíos: MAC, fabricante y, si la ficha no tiene IP, la IP `192.168.0.x`. Un dato ya cargado no se sobrescribe; si la MAC difiere queda una nota `[Escaneo LAN <IP>]` en observaciones.
2. El escáner nombra por IP a los equipos que no resuelve. Una ficha nunca se renombra con una IP; solo se cambia un nombre que sea una IP por el nombre real del escaneo.
3. Un host con dos IP y la misma MAC (por ejemplo `Sistemas23`: `192.168.0.127` y `192.168.1.119`) completa una sola ficha y deja una nota con la segunda IP.
4. Una IP con dos fichas (`192.168.1.19`) solo se asigna si la MAC o el nombre desempatan; si no, se omite.
5. MAC `00:00:00:00:00:00` se trata como dato ausente.
6. Los hosts que no están en el inventario se dan de alta como «No especificado» y «A verificar» (`CrearEquiposDetectadosSinDocumentar` en `Data/ActualizacionV4.cs` lo desactiva).

## Discrepancias que quedan visibles

- `192.168.0.24` del resumen «SWITCHES AREAS» pertenece a una VM (NEWPRO) en el escaneo; confirma que el 3Com es `192.168.1.24`. Además hay otro 3Com en `192.168.1.219` (`Guillo-Switch3com`, MAC `00:24:73:CA:67:AF`) distinto del de Agrimensura (MAC `00:24:73:CA:75:04`).
- `192.168.1.19` está en el rack como Aruba 1930 (WDGC) y en Puerta Agrimensura; el escaneo muestra la MAC de Puerta Agrimensura (HP). La IP del Aruba del rack probablemente es otra.
- `192.168.0.109`: el Visio dice Cartografia15 y el escaneo CARTOGRAFIA10.
- `192.168.0.183` y `192.168.1.180` (misma MAC Cisco, host `Catastro`) coinciden con «CISCO SYSTEM» del Excel7; podría ser el Catalyst 3560, sin confirmar.
- `Sistemas23` tiene `192.168.1.105` en el Visio y `192.168.1.119` en el escaneo.
- Doce IP de puestos del Visio no responden en el escaneo (equipos apagados o IP cambiada) y 75 hosts del escaneo no figuran en ninguna tabla del Visio.
- 38 filas del escaneo no traen fabricante (el escáner no conoce su OUI); quedan vacías.
