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
- El S4148U indica SUMARIOS en el puerto 2, R770/iDRAC 192.168.0.247 en el 21 y HP 1920 (E) 192.168.1.115 en el 24.
- El HP 1920S (H) relaciona, entre otros, el HP 1920 (E) en el 22, iDRAC del R920 en el 23 y HP 5500 en el 24.
- Mesa de Entradas no principal documenta MENTRADA04 (192.168.0.216), MENTRADA02 (192.168.0.136) y el Aruba principal (192.168.1.1).
- Varias tablas de switches de área enumeran puestos con IP y posición local; esas relaciones de usuarios están actualizadas según el usuario. Deben cargarse con el número del puerto del switch de origen tal como aparece.

Estos renglones no bastan para asumir medio de cable ni numeración del puerto del equipo remoto. Cuando falte el dato, el modelo debe mostrar no documentado y no crear un puerto remoto ficticio.

## Diferencias que no se deben corregir por inferencia

1. La tabla Excel de la página PUERTA AGRIMENSURA registra el ProCurve como 10.0.1.0; el usuario confirmó que la dirección correcta vigente es 10.0.1.1. Para la ficha vigente prevalece la corrección del usuario.
2. El resumen de switches de áreas registra el 3Com como 192.168.0.24; otras fichas del V3 y el inventario lo registran como 192.168.1.24. Conservar nota de discrepancia y no reemplazar el valor canónico sin validación.
3. El S4148U y el HP 1920 mencionan ambos el iDRAC del R770 en 192.168.0.247 (puerto 21 y puerto 16, respectivamente). Puede ser un doble enlace o un dato duplicado; verificar antes de consolidarlo como redundancia.
4. Las tablas incluyen etiquetas LIBRE, DESCONOCIDO, OCUPADO, SIN SFP y SFP LIBRE. No son equivalentes: ocupación del puerto, vigencia del registro y transceptor instalado deben persistirse por separado.
5. La tabla de Agrimensura HP incluye AGRIMENSURA60 con dos direcciones (192.168.0.141 y 192.168.1.250). Conservar ambas como observación/alias hasta que los Visios de usuario aclaren interfaces, no descartar una.

## Consecuencia para la vista gráfica y el modelo de datos

La primera gráfica muestra posiciones de puertos y líneas de extremo a extremo basadas en EnlacesFisicos, pero las hojas V3 suelen documentar solo el puerto local del switch. Para representar estas filas sin falsear el dato, la siguiente ampliación debe admitir una conexión desde una interfaz conocida hacia un equipo remoto con puerto remoto opcional. El frontal mostrará etiqueta/número y estado; el puerto ocupado podrá mostrar nombre/IP remoto y resaltarse. El trazo debe terminar en el nodo remoto, saliendo del puerto conocido. El puerto remoto ausente se mostrará como no documentado.

La importación de estas tablas a SQLite todavía está pendiente. Mantener la procedencia de página, libro y fila y no tratar el estado de los SFP como confirmado si el usuario no lo ha verificado por separado.
