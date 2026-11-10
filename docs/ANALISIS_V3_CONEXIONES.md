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

La topología conserva dos representaciones distintas. Los registros de EnlacesFisicos tienen ambos puertos y producen línea entre nodos. La tabla ConexionesDocumentadas admite un puerto local y datos remotos opcionales; ocupa el frontal y presenta la fuente, pero no traza una línea mientras falte el extremo. Así se pueden importar las filas de Visio sin inventar puertos remotos ni medio. La tabla guarda página, libro incrustado y fila, además de estado, medio y observaciones.

La estructura `ConexionDocumentada`, el formulario de alta y la presentación de relaciones de un extremo están implementados. El inicializador incorpora ahora 416 interfaces de 18 tablas, con carga idempotente y fuente página/libro/fila. Los destinos de usuario se conservan como relaciones de extremo local; se añadieron cuatro tablas de rack de propietario claro y dos enlaces completos a R770 confirmados por el usuario. Los destinos que apuntan a switches quedan a verificar, salvo la relación Mesa secundaria puerto 24 → Aruba principal `192.168.1.1` confirmada por el usuario. Mantener procedencia de página, libro y fila y no tratar el estado de los SFP como confirmado sin verificación. El usuario confirma las IP vigentes del ProCurve `10.0.1.1` y 3Com `192.168.1.24`. Para R770, HP 1920 puerto 16 ↔ P1/iDRAC `.247`; S4148U puerto 21 ↔ P2/LAN `.246`; P4 no está configurado. El medio permanece sin confirmar.


## Resultado de la carga documental de áreas

Las 14 tablas de área aportan 320 puertos: 156 filas `LIBRE`, 8 `DESCONOCIDO` y 156 destinos con nombre. Incluyen 13 switches de área y los ocho puertos declarados para `SWITCH DE 8 PUERTOS` de Inmuebles. El código de inicialización agrega interfaces y relaciones de manera idempotente, conservando página, libro y número de fila. La fila discrepante de Puerta Agrimensura hacia ProCurve conserva la IP canónica confirmada `10.0.1.1` y una nota sobre el `10.0.1.0` que figura en origen. No se asigna tipo de conector ni fecha de inspección. Referencias a switches quedan «A verificar» excepto Mesa secundaria puerto 24 → principal `192.168.1.1`.

Las conexiones de rack se importan en estado pendiente, salvo los enlaces completos a R770 confirmados por el usuario. Siguen excluidas las hojas SFP independientes con propietario ambiguo. La vista solo dibuja enlaces con ambos puertos documentados; ahora muestra los dos enlaces confirmados al R770. El catálogo ordena en el frontal etiquetas numéricas de forma natural y deja al final las etiquetas no numéricas.


## Hoja Excel16 de Inmuebles: switch secundario

La hoja identifica un equipo «SWITCH DE 8 PUERTOS» y registra las posiciones 1–6 para puestos y 7–8 como libres. Se da de alta una ficha genérica de switch en el área Inmuebles, sin fabricante, modelo, IP o tipo de conector. La tabla también incluye filas 17, 18 y 22 con estado libre, incompatibles con el encabezado; no se importan hasta identificar su equipo propietario. El puerto 24 del switch de Inmuebles principal apunta al switch secundario, pero la conexión de rack queda «A verificar» según el alcance de actualización informado por el usuario.


## Tablas de rack con propietario claro

Se incorporan las posiciones 1–24 de Excel3 (HP 5500), Excel5 (HP 1920 E), Excel7 (HP 1920S H) y Excel8 (Dell S4148U). Cada fila guarda origen página/libro/fila. Como algunas conexiones de rack pueden estar desactualizadas, los rótulos LIBRE no se elevan a libres confirmados y las conexiones con destino quedan «A verificar», salvo los dos vínculos a R770 confirmados por el usuario. Las filas SIN SFP y SFP LIBRE se conservan como estados de interface y no se transforman en conexiones. Las filas OCUPADO reservan documentalmente el puerto sin asignar una contraparte.

Dell R770 se incorpora como equipo sin IP de administración asignada: 192.168.1.22 aparece en Excel1 en una disposición de celdas cuya atribución no es segura. Se registran P1/iDRAC 192.168.0.247, P2/LAN 192.168.0.246, P3 LIBRE y P4 NO CONFIGURADO. Los enlaces de HP 1920 puerto 16 a P1 y S4148U puerto 21 a P2 se guardan completos, con estado confirmado por el usuario, medio y transceptores sin confirmar. El mapa puede trazar esos dos enlaces; no se inventó fecha de inspección.

El S4148U tiene un perfil exacto de fabricante con 48 jaulas SFP+. Se tipifican como SFP+ solo las posiciones 1–24 listadas en Excel8; el estado de módulos se conserva aparte. No se importan todavía las hojas SFP independientes de propietario ambiguo ni otras tablas del rack que no tienen una atribución fiable.


## Importación del mapa de ubicaciones de octubre de 2026

Se cotejó `UBICACION-EQUIPOS-OCTUBRE 2026.pdf` con los destinos e IP de las tablas del V3. El octeto entre paréntesis se interpreta como `192.168.0.x`, según confirmó el usuario. Se normalizaron errores evidentes de extracción/OCR (`ensura05` → `Agrimensura05`, `Agimensura20` → `Agrimensura20`, `Cartograf�a05` → `Cartografia05`, `Valucion25` → `Valuacion25`, `ebles02` → `Inmuebles02`, `ia02` → `Secretaria02`, `Secretar�a07` → `Secretaria07`). Cinco filas repetidas se consolidan y se mantienen sus números de página.

### Resultado de resolución de IP

| Resultado | Cantidad | Tratamiento |
|---|---:|---|
| IP V3 única o resuelta por coincidencia con PDF | 125 | Guardar la IP V3 |
| Discordancia PDF/V3 | 16 | Guardar la IP V3 y anotar ambas |
| Solo IP en PDF | 44 | Guardar la IP PDF como `A verificar` |
| IP V3 múltiple sin resolución | 1 | IP principal vacía; mantener candidatas en observación |
| **Total de fichas importadas** | **170** | Rol Usuario en puestos computacionales; usuario real sin asignar |

### Discrepancias donde prevalece V3

| Equipo | PDF | V3 elegido |
|---|---|---|
| Agrimensura07 | 192.168.0.40 | 192.168.0.160 |
| Agrimensura21 | 192.168.0.207 | 192.168.0.124 |
| Agrimensura27 | 192.168.0.59 | 192.168.0.58 |
| Cartografia04 | 192.168.0.35 | 192.168.0.62 |
| Cartografia16 | 192.168.0.169 | 192.168.0.146 |
| Cartografia19 | 192.168.0.168 | 192.168.0.154 |
| Cartografia21 | 192.168.0.170 | 192.168.0.176 |
| Dominial03 | 192.168.0.57 | 192.168.0.230 |
| Gestion02 | 192.168.0.71 | 192.168.0.199 |
| Inmuebles02 | 192.168.0.133 | 192.168.0.220 |
| Inmuebles04 | 192.168.0.138 | 192.168.0.179 |
| Secretaria02 | 192.168.0.220 | 192.168.0.170 |
| Secretaria03 | 192.168.0.221 | 192.168.0.123 |
| Sumarios04 | 192.168.0.101 | 192.168.0.44 |
| Valuacion08 | 192.168.0.131 | 192.168.0.112 |
| PLOTTER EPSON T7200 (PDF: T7200) | 192.168.0.189 | 192.168.0.202 |

### Casos resueltos o pendientes

- `Sistemas23`: el PDF indica `192.168.0.64`; V3 contiene `192.168.0.127` y `192.168.1.105`. Se guardan ambas direcciones V3 en observaciones y se deja vacía la IP principal.
- El usuario confirma `T7200` = `PLOTTER EPSON T7200`: se importa con la IP V3 `192.168.0.202` y se anota el PDF `192.168.0.189`.
- El usuario confirma `TrimbleNET-R9` = `TRIMBLE TUC1`: se importa con la IP común `192.168.0.17`, usando el nombre canónico del V3 y preservando la etiqueta PDF en observaciones.
- `Sistemas06` (PDF `.127` y `.189`) se resuelve a `192.168.0.189`, que coincide con una de las IP V3 (`.189`, `.197`); ambas diferencias quedan en observaciones.
- `Valuacion51` usa `192.168.0.158`, coincidente con una de sus IP V3 (`.158`, `.196`). `Agrimensura60` usa `192.168.0.141`; se mantiene `192.168.1.250` como dirección adicional consignada en V3.

Las áreas y pisos se toman de la página del PDF. El usuario asignado queda vacío hasta integrar los Visios de PCs y usuarios. Los puestos computacionales reciben el rol predeterminado `Usuario`; el técnico puede cambiarlo manualmente a `Jefe` u otro rol, y el inicializador no reemplaza valores personalizados. Las fichas se crean de forma idempotente en SQLite y cada alta genera historial; si una ficha con ese nombre ya existe, se conserva sin sobrescribir los datos de técnicos. El catálogo tipado C# es una carga fuente reproducible, no una base paralela ni reemplazo de SQLite; la base `.db` no se agrega al repositorio.

## Ajustes UX/UI de switching (10 de noviembre de 2026)

El mapa acomoda switches en dos columnas en escritorio y una en móvil. El frontal diferencia ocupado, libre confirmado y desconocido con color y borde contrastantes e incluye contadores por estado. El detalle del switch reúne frontal, resumen, IP, rol, enlaces completos y conexiones parciales. Cada extremo muestra nombre, IP atenuada y puerto. Las listas permiten búsqueda, filtro de estado, paginación y acceso directo al frontal. El medio solo aparece cuando está confirmado y página/libro/fila no se muestran por defecto; el respaldo queda bajo demanda. La vista admite impresión.


## Interacción puerto–conexión (10 de noviembre de 2026)

En el detalle de switch, los puertos tienen controles accesibles por ratón, Enter y Espacio. La selección anuncia el estado y destino guardados, resalta la ficha local que corresponde a la etiqueta del puerto y desplaza la página a esa ficha. Seleccionar el cuerpo de una ficha resalta el puerto; controles internos como enlaces y resúmenes de detalles mantienen su comportamiento. El vínculo usa la etiqueta de puerto local que ya consta en ambos registros. No se usa para deducir puerto remoto, medio ni conectividad actual. Los puertos sin destino presentan «Sin destino documentado». El foco se distingue visualmente y se respeta la preferencia de movimiento reducido.
