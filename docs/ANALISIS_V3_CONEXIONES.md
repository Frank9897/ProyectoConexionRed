# Análisis de conexiones de DIAGRAMA RED V3.0

## Alcance revisado

El archivo V3.0 fue leído como paquete Visio. Contiene 22 páginas y 31 libros Excel incrustados. Los nombres de página se resolvieron mediante las relaciones internas del paquete, no solo por el número de archivo XML. Las hojas que aparecen dentro de una página pueden tener su propio encabezado de equipo distinto al nombre de esa página; para atribuir una tabla hay que respetar ambos datos.

## Página y libros incrustados

| Página del Visio | Libro(s) Excel relacionado(s) | Lectura |
|---|---|---|
| RACK 1 | — | Dibujo físico de rack; no contiene hoja de posición enlazada |
| ESQUEMA CONEXIÓN INTERNET | Excel, Excel1, Excel2 | ProCurve, Dell R770 y patchera; revisar cada encabezado antes de atribuir el extremo |
| Switch HP 5500 (D) RACK1 | Excel3, Excel4 | Tabla de puertos de equipos de servidores/almacenamiento y tabla SFP |
| Switch HP 1920 (E) RACK1 | Excel5, Excel6 | Puertos de equipos y tabla rotulada PUERTO SFP |
| DELL S4148U (I) | Excel8 | Tabla de puertos/estado SFP del S4148U |
| Switch HP 1920S (H) RACK1 | Excel7 | Tabla de dispositivos conectados |
| SWITCHES AREAS | Excel9 | Resumen de switches, etiquetas e IP |
| SWITCH AGRIMENSURA HP | Excel10, Excel11 | Puertos de Agrimensura HP; Excel11 tiene encabezado propio Switch HP 1920, revisar propietario antes de importar |
| SWITCH AGRIMENSURA 3COM | Excel12 | Puertos de Agrimensura 3Com |
| ASUNTOS JURIDICOS | Excel13 | Puertos del switch del área |
| CARTOGRAFIA | Excel14 | Puertos del switch del área |
| INMUEBLES | Excel15, Excel16, Excel17 | Switch del área, switch de 8 puertos y hoja rotulada SFP |
| MESA DE ENTRADA (No principal) | Excel18 | Puestos y uplink al principal |
| PUERTA AGRIMENSURA | Excel19 | Puertos del switch del área; contiene la discrepancia de IP ProCurve |
| REGULARIZACION DOMINIAL | Excel20 | Puertos del switch del área |
| SECRETARIA | Excel21, Excel22 | Puertos y tabla adicional rotulada SFP |
| SISTEMAS | Excel23 | Puertos del switch del área |
| VALUACION R0 | Excel24 | Puertos del switch del área |
| VALUACION R1 | Excel25 | Puertos del switch del área |
| VALUACION R2 | Excel26 | Puertos del switch del área |
| RESERVA | Excel27, Excel28 | Tablas de posiciones; Excel28 se presenta como tabla SFP |
| RESERVA2 | Excel29, Excel30 | Tablas de posiciones; Excel30 se presenta como tabla SFP |

## Funciones de switches confirmadas por el usuario

- **HP 5500:** switch de servidores.
- **HP 1920 (E):** distribución que recibe switches de áreas.
- **Dell S4148U-ON:** puente hacia el Dell R770 previsto como servidor principal.
- **Switches con nombre de Agrimensura, Valuación, Sistemas u otra oficina:** switches de área cuando la propia ficha o página los identifica así.
- **Mesa de Entradas:** el switch No principal es secundario y enlaza al Aruba 1930 principal. La hoja muestra MENTRADA04 en el puerto 1, MENTRADA02 en el 3 y el Aruba principal en el 24; son dos puestos más el enlace al principal.

En el inventario, rol operativo, tipo, fabricante y modelo son campos distintos. No usar Switch de área como modelo.

## Relaciones identificadas en tablas

- El libro del HP 5500 lista servidores, almacenamiento y gestión. Incluye ProCurve en el puerto 22 y HP 1920S (H) en el 24.
- El libro de puertos del HP 1920 incluye dispositivos de rack y switches/servicios de área. Su tabla SFP enumera puertos 26, 27 y 28 hacia Secretaría, Agrimensura HP e Inmuebles; los demás renglones de esa tabla deben conservar su estado literal.
- El S4148U indica SUMARIOS en el puerto 2 y HP 1920 (E) 192.168.1.115 en el 24. La fila del puerto 21 que lo rotula como iDRAC `.247` queda corregida por el usuario: corresponde al Dell R770 LAN P2, IP `192.168.0.246`.
- El HP 1920 (E) relaciona R770 P1/iDRAC (`192.168.0.247`) en el puerto 16. El HP 1920S (H) relaciona, entre otros, el HP 1920 (E) en el 22, iDRAC del R920 en el 23 y HP 5500 en el 24.
- Mesa de Entradas no principal documenta MENTRADA04 (192.168.0.216), MENTRADA02 (192.168.0.136) y el Aruba principal (192.168.1.1).
- Varias tablas de switches de área enumeran puestos con IP y posición local; esas relaciones de usuarios están actualizadas según el usuario. Deben cargarse con el número del puerto del switch de origen tal como aparece.

Estos renglones no bastan para asumir medio de cable ni numeración del puerto del equipo remoto. Cuando falte el dato, el modelo debe mostrar no documentado y no crear un puerto remoto ficticio.

### Pares de puertos recíprocos que aparecen en las hojas

Las coincidencias siguientes sirven para proponer extremos locales y remotos con mejor sustento documental. Como el usuario indicó que algunas conexiones del rack pueden estar desactualizadas, se mantienen como **candidatos a revisar físicamente** hasta confirmar vigencia. Las tablas no identifican de manera confiable el medio de cada cable.

| Extremo en una hoja | Extremo recíproco | Evidencia y tratamiento |
|---|---|---|
| HP 5500 puerto 24 | HP 1920S (H) puerto 24 | Ambas hojas nombran al switch contraparte; confirmar que el cable siga conectado. Medio no confirmado. |
| HP 1920 (E) puerto 24 | HP 1920S (H) puerto 22 | Coincidencia de contraparte en ambas tablas; revisar en rack. |
| HP 1920 (E) puerto 19 | Valuación R1 puerto 24 | Referencia recíproca en las tablas; revisar vigencia del enlace de rack. |
| HP 1920 (E) puerto 20 | Valuación R2 puerto 24 | Referencia recíproca en las tablas; revisar vigencia del enlace de rack. |
| HP 1920 (E) puerto 21 | Sistemas puerto 24 | Referencia recíproca en las tablas; revisar vigencia del enlace de rack. |
| HP 1920 (E) SFP 26 | Secretaría SFP 26 | Referencia recíproca; no se confirma tipo/modelo de transceptor ni medio. |
| Agrimensura HP puerto 15 | Agrimensura 3Com puerto 24 | Referencia recíproca entre las hojas de ambas áreas; verificar físicamente. |

No consolidar como enlace confirmado la salida S4148U puerto 24 ↔ HP 1920: la tabla HP 1920 menciona el S4148U en los puertos 1 y 23, por lo que el extremo correspondiente es ambiguo. También dejar «a verificar» la relación HP 1920 puerto 13 ↔ Cartografía: la hoja de Cartografía señala al HP 1920S (H) en su puerto 24, y no confirma el extremo remoto del puerto 13.


## Diferencias que no se deben corregir por inferencia

1. La tabla Excel de la página PUERTA AGRIMENSURA registra el ProCurve como 10.0.1.0; el usuario confirmó que la dirección correcta vigente es 10.0.1.1. Para la ficha vigente prevalece la corrección del usuario.
2. El resumen de switches de áreas registra el 3Com como 192.168.0.24; el usuario confirma que la IP vigente es 192.168.1.24. Mantener 192.168.1.24 en la ficha y conservar la discrepancia como nota de fuente.
3. La tabla del S4148U rotula su puerto 21 como iDRAC .247, pero el usuario corrige el extremo: S4148U puerto 21 ↔ Dell R770 LAN P2, IP 192.168.0.246. HP 1920 puerto 16 ↔ Dell R770 P1/iDRAC, IP 192.168.0.247. El R770 P3 figura libre y P4 no configurado. No hay base para declarar medio físico.
4. Las tablas incluyen etiquetas LIBRE, DESCONOCIDO, OCUPADO, SIN SFP y SFP LIBRE. No son equivalentes: ocupación del puerto, vigencia del registro y transceptor instalado deben persistirse por separado.
5. La tabla de Agrimensura HP incluye AGRIMENSURA60 con dos direcciones (192.168.0.141 y 192.168.1.250). Conservar ambas como observación/alias hasta que los Visios de usuario aclaren interfaces, no descartar una.

## Consecuencia para la vista gráfica y el modelo de datos

La gráfica actual muestra posiciones por capacidad documentada y relaciones únicamente desde EnlacesFisicos, cuyo modelo requiere dos puertos existentes. El frontal esquemático ya muestra bocas numeradas en dos hileras, el estado por color y abre una página con detalles de cada switch. El destino emergente aparece cuando el enlace de dos extremos existe. Para importar filas de Visio que solo indican el puerto local, la siguiente ampliación debe permitir un extremo remoto opcional, con nombre/IP y procedencia, sin inventar puerto remoto ni medio.

La importación masiva de estas tablas a SQLite todavía está pendiente. Mantener procedencia de página, libro y fila y no tratar el estado de los SFP como confirmado sin verificación. El usuario confirma las IP vigentes del ProCurve `10.0.1.1` y 3Com `192.168.1.24`. Para R770, HP 1920 puerto 16 ↔ P1/iDRAC `.247`; S4148U puerto 21 ↔ P2/LAN `.246`; P4 no está configurado. El medio permanece sin confirmar.
