# ProyectoConexionRed

Aplicación web en español para documentar y visualizar la infraestructura de red empresarial. El repositorio es público por decisión del usuario.

## Propósito

Reunir en un inventario navegable los dispositivos, IP, puertos, enlaces, áreas, PCs y usuarios de la red; mostrar su topología por vistas física, switching y red; registrar cambios; y permitir simulaciones locales y exportación imprimible.

## Estado del desarrollo

**MVP de inventario, acceso e historial en desarrollo.** La aplicación usa ASP.NET Core MVC, Entity Framework Core y SQLite. Incluye inicio, inventario con búsqueda por nombre, IP, área, piso, tipo de dispositivo, modelo o fabricante, alta y edición de fichas, exportación CSV e impresión de la tabla. Carga 25 registros iniciales cuando la base está vacía. El formulario de inventario prioriza datos técnicos y ubicación; los campos de usuario asignado, fuente documental y acceso administrativo no se capturan ni editan por ahora. Los valores ya registrados se conservan al editar. Tipo de dispositivo, rol y vigencia se eligen mediante listas. La tabla muestra el tipo de dispositivo y permite ver 10, 30 o todos los registros, con navegación por páginas. La impresión incluye todos los resultados de la búsqueda aunque en pantalla haya una sola página visible. Los cambios de campos editables quedan auditados. La inicialización incorpora de forma idempotente puestos y ubicaciones del PDF a SQLite; el rol inicial de los puestos computacionales es Usuario y puede ajustarse manualmente, por ejemplo a Jefe.

El login está deshabilitado temporalmente para las pruebas: los módulos principales abren sin sesión. El filtro global de autorización está comentado en Program.cs; las acciones de ingreso y cierre llevan [NonAction], y la navegación pública no muestra controles de cuenta. La administración de usuarios sigue protegida por rol y no está disponible en este modo. Al final del proyecto se restaurará el login descomentando el filtro y quitando [NonAction]. La cuenta administradora inicial solo se crea cuando la base todavía no contiene usuarios y se configuran las variables de entorno `INVENTARIO_ADMIN_USUARIO` y `INVENTARIO_ADMIN_CLAVE`. La clave se guarda como hash. La cuenta inicial recibe rol Administrador. El administrador puede crear cuentas Técnicas, hasta cinco cuentas en total, y activar o desactivar cuentas. Las cuentas existentes reciben rol Administrador al actualizar una base anterior, para no quitar el acceso que ya tenían. El administrador no puede desactivar su propia cuenta activa ni la última cuenta administradora activa. La pantalla «Accesos» muestra los últimos cien inicios de sesión y cierres de sesión, con hora UTC y resultado; no registra contraseñas.

La aplicación permite crear y editar fichas de equipos y puestos; registra cambios y datos iniciales en un historial por usuario, fecha UTC, campo y valores anterior/nuevo. La página Topología resume equipos, switches clasificados, puestos e interfaces y presenta las capas física, switching y acceso en tarjetas adaptables. Cuando no hay enlaces, muestra un estado vacío informativo; cuando los hay, presenta extremos, puertos, medio, estado, fuente y fecha de revisión. La tabla de enlaces se alimenta solo de relaciones guardadas. Se incorporó la sección **Puertos** para registrar interfaces y enlaces físicos entre puertos existentes, con medio, módulos opcionales, fuente, estado de revisión y fecha real. Las listas usan fichas compactas con paginación independiente, 10 elementos iniciales y opciones de 10, 20, 30 o todos, más una transición sutil de 450 ms. Se evita reutilizar un puerto ya enlazado y se registra el alta en el historial de cambios. Esta pantalla solo modifica la documentación SQLite; no consulta ni cambia la red real. La corrección de interfaces y enlaces queda auditada campo por campo; la importación validada de relaciones desde V3 sigue pendiente. Topología ahora dibuja equipos como nodos, enlaces físicos guardados como líneas y posiciones de puertos con estados ocupado, libre confirmado y desconocido. El selector de capas cambia entre vista física (todos los equipos), switching (switches y enlaces documentados entre ellos) y acceso (puestos más switches con enlace explícito a esos puestos); búsqueda y área se aplican en cada vista. El selector es una perspectiva de inventario: no modela todavía VLAN, administración ni rutas de capa 3. Usa iconos SVG locales y perfiles exactos de modelo; la ilustración genérica se aplica cuando el modelo no coincide o la capacidad no está especificada. Se agregaron capacidades de RJ45/SFP/SFP+/QSFP/QSFP+/QSFP28 al inventario de switches y el uso explícito de puertos. Los enlaces siguen siendo documentales, no pruebas de disponibilidad. La ficha SFP y la exportación gráfica siguen pendientes. Falta aplicar límites a intentos de acceso, verificar la aplicación y completar HTTPS antes de considerar un despliegue de producción.

Las fuentes de red más recientes recibidas el 4 de noviembre de 2026 son **DIAGRAMA RED V3.0.vsdx** y **Diagrama UPS.vsdx**. La inicialización incorpora MAC explícitas del V3 si coinciden la etiqueta y la IP y el campo está vacío; corrige Mesa de Entradas a Aruba 1930 principal con IP 192.168.1.1, completa Aruba como fabricante del switch de IP 192.168.1.17 e incorpora una vez Aruba 1930 Reserva 2. La importación deja historial y no reemplaza datos existentes. El usuario confirmó que V3.0 fue verificado switch por switch e interfaz por interfaz; sus relaciones verificadas tendrán prioridad para IP de red y puertos. La lectura estructural del V3 encontró 31 hojas de cálculo incrustadas. Los puertos se describen como posición local → equipo/IP remoto; muchas tablas no informan puerto remoto ni medio, por lo que esos extremos no deben inventarse. El análisis página/tabla, los roles confirmados y las discrepancias pendientes están en [docs/ANALISIS_V3_CONEXIONES.md](docs/ANALISIS_V3_CONEXIONES.md). La aplicación importa de forma idempotente las posiciones documentadas de puerto y separa ocupación, vigencia y SFP. Las conexiones de rack que no estén confirmadas y algunas relaciones UPS siguen pendientes.

También se recibió **UBICACION-EQUIPOS-OCTUBRE 2026.pdf**, con planos de ubicación, puestos, usuarios y áreas. El usuario aclaró que el número entre paréntesis de cada puesto representa el último octeto de una IP de usuario `192.168.0.xx`; se debe construir la IP completa sin tratarlo como número de inventario. Este PDF será fuente de ubicación y asignación de usuario, mientras que V3.0 prevalece para IP y puertos cuando contenga el dato. El inicializador importa 170 fichas de puestos, relojes y periféricos con área, piso y página de origen. V3 prevalece para las IP; las discrepancias quedan anotadas, y las direcciones que solo aparecen en el PDF quedan «A verificar». No se inventan enlaces a partir de la ubicación.

Se esperan otros dos Visios para completar o validar nombres de PC, identificadores usuarioPC y nombres reales de usuario.

Los switches de las áreas Régimen y Mesa de Entradas Principal son inaccesibles administrativamente por falta de credenciales. El dato de acceso administrativo permanece en las fichas existentes pero no se muestra ni edita desde el inventario por ahora. El switch etiquetado «No principal» de Mesa de Entradas queda identificado como secundario del principal del área y conectado a tres equipos de usuario según la confirmación del usuario.

El inventario detallado y las reglas de continuidad están en [docs/PROMPT_IA.md](docs/PROMPT_IA.md).

## Ejecutar localmente

Requiere el SDK de .NET 10. En el modo temporal sin login no hace falta configurar una cuenta para abrir el inventario. Ejecuta la aplicación:

```powershell
dotnet run --project ProyectoConexionRed.Web.csproj
```

Al iniciar, se crea `proyecto-conexion-red.db` y se agregan los registros iniciales si todavía no existen. Si ya existe una cuenta, las variables de alta inicial no modifican ni restablecen usuarios.

## Arquitectura

- C# y ASP.NET Core MVC.
- Entity Framework Core con proveedor SQLite.
- La autenticación por cookie queda configurada para reactivarse al final. En el modo actual no se exige para las páginas principales; no desplegar el modo abierto.
- El servidor web es la vía de acceso a SQLite; no compartir directamente el archivo de base por una unidad de red.
- Se espera un técnico actualmente y hasta cinco cuentas de técnicos. La administración de usuarios está limitada al rol Administrador; las altas nuevas reciben rol Técnico. La cookie se valida contra SQLite en cada solicitud; una cuenta desactivada pierde acceso en la siguiente solicitud.

## Revisión y refactorización de código

Se centralizaron la normalización de textos opcionales, el registro de cambios por campo y la carga de opciones de equipos e interfaces en `Services/UtilidadesConexiones.cs`. Los formularios de alta y edición de puertos y enlaces comparten modelos base con sus validaciones y fragmentos Razor; las reglas CSS de esa sección quedaron consolidadas en un solo bloque. Esto reduce reglas duplicadas y mantiene un único formato para esos datos. La revisión también corrigió una incompatibilidad en los argumentos usados para auditar cambios de enlaces.

La revisión se hizo sobre la rama publicada. En esta sesión no hay un checkout local de .NET para ejecutar la compilación; el resultado requiere esa comprobación en el entorno del proyecto.

## Próximas etapas

1. Completar la revisión de hojas Excel del V3 cuyo switch propietario o conexión de rack siga ambiguo; no convertir notas parciales en enlaces.
2. Separar en datos estructurados el uso del puerto, la vigencia del registro y la presencia/modelo/fecha del transceptor SFP.
3. Enriquecer PCs y usuarios cuando lleguen los dos Visios pendientes; luego completar jerarquía de switching y VLAN con relaciones confirmadas.
4. Agregar simulaciones únicamente sobre datos locales, sin comandos ni consultas a la red real.
5. Completar exportación gráfica de topología, migraciones, respaldos y controles de acceso antes de restaurar el login.


### Topología gráfica implementada

La vista Topología representa todas las fichas de equipo como nodos y dibuja una línea solamente para cada registro de `EnlacesFisicos` cuyos dos extremos existen. Incluye búsqueda, filtro por área, iconos SVG locales por tipo, frontal de puertos adaptable, lista de relaciones exactas y un estado visual accesible:

- Verde: puerto ocupado por un enlace completo o una relación parcial guardada; no confirma conectividad actual.
- Gris oscuro: puerto con uso marcado explícitamente como `Libre confirmado` con vigencia `Confirmado`.
- Verde oscuro: puerto desconocido o sin correspondencia exacta con una posición.

La tabla `InterfazEquipo` separa `EstadoUso` de `EstadoDatos`. La ausencia de enlace no convierte un puerto en libre. Los equipos con capacidad manual pueden definir cantidades por grupo de conector. Se agregó un perfil local exacto para Dell S4148U-ON (48 SFP+, 2 QSFP+ y 4 QSFP28), respaldado por [documentación oficial de Dell](https://www.dell.com/support/manuals/en-uk/networking-s4148f-on/s4100f_t_u_on_install_pub/features?guid=guid-0de00e36-0afd-4bc1-848f-10243d7075ec&lang=en-us). Los modelos sin perfil exacto usan el switch genérico y las cantidades documentadas manualmente; las etiquetas de puertos solo se mapean si coinciden exactamente tras normalizar espacios y mayúsculas.

La inicialización SQLite agrega de forma idempotente las columnas nuevas de capacidad y uso de puerto para conservar bases existentes. Las ilustraciones se sirven desde `wwwroot/images/dispositivos`; los SVG son esquemáticos y los puertos se dibujan según los datos.

## Principios de datos y seguridad

- Diferenciar vigencia de datos, estado de acceso administrativo y conectividad física.
- No inventar conexiones, puertos, modelos de SFP ni fechas de revisión.
- Las simulaciones no deben ejecutar acciones contra la red de producción.
- No guardar contraseñas, claves ni tokens en el inventario, los logs o el historial.
- Antes de compartir o publicar diagramas, retirar credenciales que puedan estar escritas dentro de sus páginas.

## Reglas para contribuir

- Variables en español; comentar cada variable explicando qué representa y el efecto de cambiar su valor.
- Funciones documentadas en español, incluyendo qué hacen sus parámetros y cómo afectan el resultado.
- Actualizar este README y [docs/PROMPT_IA.md](docs/PROMPT_IA.md) en cada modificación.
- Usar mensajes de commit breves y en español.
- No incluir secretos en código, documentación, ejemplos, capturas o commits.


## Avance de topología visual

La vista de topología incorpora un frontal esquemático para cada switch con capacidad documentada o ingresada. Las bocas se distribuyen en dos filas numeradas; el verde claro marca ocupación registrada, el gris oscuro marca puerto libre confirmado y el verde oscuro translúcido representa estado desconocido. Las bahías SFP se dibujan por separado y la presencia/modelo del transceptor no se deduce de la bahía. Cada puerto ocupado muestra el destino en la ayuda emergente cuando existe un enlace completo o una relación documental de un extremo. El enlace «Ver puertos y enlaces» abre el detalle del switch.

La aplicación ahora admite relaciones documentales con un puerto local obligatorio y equipo/IP/puerto remotos opcionales. Guarda página, libro Excel y fila de origen, estado, medio y observaciones; los valores predeterminados son «A verificar» y «No confirmado». Una relación parcial ocupa el puerto en el frontal y aparece en sus detalles, pero no dibuja una línea entre equipos. El formulario evita reutilizar puertos ya vinculados y registra el alta en auditoría. Las tablas de área seleccionadas del V3 ya se incorporan al iniciar la aplicación, con carga idempotente, página/libro/fila de procedencia y destinos de un extremo. Se incorporan cuatro tablas de rack con propietario claro; siguen pendientes las hojas SFP independientes con propietario ambiguo y otras tablas sin atribución segura.

Correcciones confirmadas para el análisis: HP ProCurve `10.0.1.1`; switch 3Com `192.168.1.24`; el HP 1920 puerto 16 corresponde al iDRAC del Dell R770 (P1, `192.168.0.247`); el S4148U puerto 21 corresponde al LAN P2 del R770 (`192.168.0.246`), y P4 del R770 está sin configurar. El medio de esos enlaces sigue sin confirmación.


## Importación de puertos de área y rack del V3

La inicialización de SQLite incorpora 18 tablas del archivo `DIAGRAMA RED V3.0.vsdx`: 416 interfaces documentadas en total. Las 14 tablas de área suman 320 posiciones; cuatro tablas con propietario de rack claro añaden 96. Las altas son idempotentes: volver a iniciar la aplicación no duplica interfaces ni relaciones.

Cada posición conserva el nombre de la página, libro Excel incrustado y fila original. Las conexiones de usuario de las tablas de área se registran según la confirmación del usuario. Los datos de rack quedan «A verificar», salvo los dos enlaces que el usuario confirmó con el Dell R770. HP 1920 puerto 16 ↔ R770 P1/iDRAC (`192.168.0.247`) y S4148U puerto 21 ↔ R770 P2/LAN (`192.168.0.246`) ya tienen ambos extremos registrados y aparecen como líneas en el mapa. El medio y los transceptores permanecen sin confirmar. Las demás relaciones de un extremo no producen líneas.

El importador no asigna medio, modelo de transceptor ni fecha de revisión que la hoja no acredita. Solo tipifica como SFP+ las posiciones del Dell S4148U respaldadas por el perfil exacto del modelo. Corrige únicamente la IP ProCurve de la fila discrepante a `10.0.1.1`, dejando constancia de que el valor escrito en esa fila era `10.0.1.0`. Protege también los puertos existentes con enlace completo, relación parcial o estado «Libre confirmado». Se incluyen 13 switches de área, el switch secundario de Inmuebles y los puertos 1–24 de cuatro switches de rack identificados por sus páginas: HP 5500, HP 1920, HP 1920S y Dell S4148U. En la tabla secundaria de Inmuebles solo se importan puertos 1–8; las filas 17, 18 y 22 contradicen su encabezado y quedan excluidas. Las tablas SFP independientes con propietario ambiguo y las restantes hojas de rack siguen pendientes.


### Calidad de los puertos de rack importados

Las filas de rack se conservan como aparecen en el Visio, con página, libro y fila. Los 33 rótulos LIBRE del rack no se marcan como libres confirmados; sus conexiones pueden estar desactualizadas. Las filas SIN SFP, SFP LIBRE y OCUPADO se preservan como estados literales: OCUPADO ocupa el puerto documentalmente, pero no crea nombre, IP ni extremo remoto ficticio. Los estados de transceptor no se deducen de la capacidad del switch. Para S4148U, las 24 posiciones importadas se clasifican como jaulas SFP+ según el perfil exacto del modelo; no se afirma qué módulos están instalados.

El nodo Dell R770 no recibe dirección IP de administración porque la celda 192.168.1.22 de Excel1 no tiene una atribución inequívoca. Se registran las interfaces P1–P4: P1 e iDRAC 192.168.0.247, P2/LAN 192.168.0.246, P3 libre según Excel1 y P4 sin configurar según la aclaración del usuario. Solo P1 y P2 participan en los dos enlaces confirmados del mapa; no se asigna fecha de revisión ni medio físico.


## Importación de puestos y ubicaciones

Se incorporan 170 fichas únicas desde `UBICACION-EQUIPOS-OCTUBRE 2026.pdf`, con etiqueta normalizada, área, piso, páginas fuente y octeto de IP. El valor entre paréntesis se interpreta como último octeto de `192.168.0.x`. Las cinco repeticiones del PDF se consolidan y se conservan las páginas de referencia; el usuario asignado queda vacío hasta recibir los Visios de PCs y usuarios.

Para escoger IP prevalece `DIAGRAMA RED V3.0`: 125 fichas tienen IP V3 resuelta, incluyendo seis casos donde el PDF ayuda a escoger entre varias direcciones; 16 diferencias quedan anotadas con la IP V3 como vigente. Hay 44 IP que solo aparecen en el PDF y se importan como `A verificar`. `Sistemas23` conserva las dos candidatas V3 en observaciones y queda sin IP principal. Por confirmación del usuario, `T7200` se integra como `PLOTTER EPSON T7200` y `TrimbleNET-R9` como `TRIMBLE TUC1`.

El catálogo C# tipado es únicamente una carga documental idempotente para poblar SQLite cuando falten esas fichas; SQLite continúa como base operativa. No se añade un segundo catálogo JSON ni se publica el archivo `.db`. Si las fichas ya existen, la carga las conserva para no sobrescribir ediciones de técnicos.

El inicializador deja constancia de cada alta en el historial de cambios, con origen y descripción del dato. Las IP y conexiones se documentan dentro de la aplicación; no se ejecutan pruebas ni cambios en la red real.


## Presentación detallada de conexiones

La vista Switching organiza los switches en dos columnas en escritorio y una columna en pantallas estrechas. El frontal diferencia ocupado (verde brillante), libre confirmado (gris) y desconocido (verde oscuro con borde discontinuo ámbar), además de resumir sus cantidades. La ficha de cada switch abre su detalle con modelo, IP, rol, frontal, conexiones completas y relaciones pendientes.

Las conexiones muestran nombre del equipo, IP en gris, etiqueta de puerto y estado. El medio solo aparece cuando está documentado y confirmado; página/libro/fila quedan bajo un desplegable para mantener la lectura compacta. La lista permite buscar por nombre, IP y puerto, filtrar por estado, paginar y abrir directamente el frontal del switch. También se puede imprimir la vista actual.


### Recorrido sincronizado entre puertos y conexiones

En el detalle de un switch, cada puerto del frontal se puede seleccionar con ratón, Enter o Espacio. La selección anuncia estado y destino documentados, destaca la ficha que comparte la etiqueta del puerto y desplaza la página hacia ella. Al pulsar una ficha se resalta el puerto correspondiente. Si el remoto no está documentado, se muestra esa falta explícitamente y no se crea un enlace ficticio. El foco de teclado y el movimiento respetan la preferencia de movimiento reducido del navegador.


### Ajustes de switching, estado documental e impresión

- Las tarjetas de topología usan dos columnas en escritorio y una en pantallas angostas; cada tarjeta gana ancho para mostrar el frontal y sus datos con menos recorte horizontal.
- El mapa y el detalle del switch cuentan igual los enlaces completos y las relaciones documentadas con extremo remoto parcial. Un puerto ocupado por una relación parcial ya no aparece como desconocido al abrir el switch.
- Por confirmación del usuario, los registros con procedencia de Visio se presentan como confirmados documentalmente. Esto confirma lo que la fuente registra; no implica conectividad en vivo ni completa los nombres, IP, puerto remoto o medio que falten.
- Los tipos de puerto no especificados se rotulan como “Tipo de puerto no indicado”, para evitar confundir esa falta puntual con el estado de confirmación de todo el switch.
- La impresión del detalle se compacta para A4 horizontal: tarjetas de conexión en dos columnas, frontal y datos técnicos en paralelo, controles y paneles vacíos omitidos. Se conservan extremos, IP, puertos y estados visibles.


### Capas de topología cotejadas con el Visio V3

La vista organiza las conexiones según la función documentada: **Capa 1** representa el cableado/patcheras, **Capa 2** los enlaces entre switches y **Capa 3** los equipos finales conectados a puertos de switches. Una PC como AGRIMENSURA15 se presenta en Capa 3 con el puerto local del switch; no se le atribuye un puerto remoto de NIC. En Capa 2, se muestran ambos puertos solo cuando las tablas opuestas del V3 permiten cotejar un par único. Si el puerto opuesto falta, la conexión entre switches sigue marcada como confirmada documentalmente y el número remoto queda sin asignar.

Se cotejaron los 31 libros Excel incrustados en `DIAGRAMA RED V3.0.vsdx`. El análisis por libro, los pares recíprocos, las correcciones confirmadas y las filas ambiguas están documentados en [docs/ANALISIS_V3_CONEXIONES.md](docs/ANALISIS_V3_CONEXIONES.md). Se incorporan al catálogo los SFP/SFP+ descritos en las tablas correspondientes; las interfaces se rotulan `SFP n` para distinguirlas de RJ45 con el mismo número. Esto no supone que las jaulas compartidas sean independientes ni que haya un transceptor instalado.

Las correcciones confirmadas aplicadas incluyen ProCurve `10.0.1.1`, 3Com `192.168.1.24`, Aruba 1930 principal de Mesa de Entradas `192.168.1.1` y R770 P2/LAN `192.168.0.246` (P1/iDRAC `192.168.0.247`). La inaccesibilidad por credenciales de Régimen y Mesa de Entradas principal se conserva como estado administrativo y no como falta de confirmación de su existencia o de sus enlaces documentados.


Cuando una tabla de puerto documenta un endpoint Capa 3 que todavía no tiene una ficha independiente, el mapa dibuja un nodo visual temporal con su nombre/IP y el icono correspondiente. Ese nodo solo existe en la topología generada y no crea un registro nuevo en SQLite; si luego se incorpora al inventario, el mapa usa la ficha persistida.


### Corrección del importador V3

Se corrigió un separador faltante entre filas del JSON incrustado de `CatalogoPuertosAreasV3.cs`, que impedía iniciar la importación. El contenido JSON del catálogo quedó validado antes de guardar el cambio.


### Impresión de switch y mapa en una hoja

El detalle individual de switch se imprime en A4 horizontal, ajustando automáticamente su escala para mantener el frontal, los datos y las conexiones en una sola página. El mapa general omite los paneles ajenos al mapa y encaja las tarjetas y enlaces en casi toda el área imprimible (287 × 180 mm, con margen de 5 mm). El cálculo panorámico usa solo el mapa visible, puede ampliarlo por encima del 100 % y restablece el zoom de pantalla al cerrar la vista previa.


### Mapa navegable e impresión en una hoja

La topología mantiene el lienzo en un área visible estable, con desplazamiento interno para recorrer las tarjetas. El botón **Maximizar mapa** expande el panel y conserva filtros y controles de capa. La impresión del detalle del switch ajusta las conexiones en columnas según su cantidad, conserva un margen A4 y reduce el contenido solo cuando hace falta. La impresión panorámica muestra el mapa en formato compacto en una página.


### Navegación visual del mapa

Desde el 10 de noviembre de 2026 las tarjetas del mapa ya no están limitadas a dos columnas: cada una es independiente y se puede arrastrar libremente, y sus líneas la siguen. El mapa se navega arrastrando el fondo, con Ctrl + rueda o con los botones **Acercar**/**Alejar**; **Vista inicial** vuelve al zoom legible centrado en el equipo principal, **Ver todo** encaja todas las tarjetas y **Distribuir** reparte de nuevo las tarjetas con el modo elegido (mosaico para verlo todo, filas por grupo o cuadrícula por área). **Maximizar mapa** expande el panel completo. Las conexiones se distinguen con colores por capa y salen a la altura del puerto documentado, con su número rotulado.


### Caja del mapa y expansión dentro de la página

Los controles **Centrar vista** y **Maximizar mapa** están dentro del marco del mapa. Maximizar amplía la caja en la misma página con fondo atenuado; no solicita pantalla completa al navegador. Se puede restaurar con el botón o con Escape. Las tarjetas quedan en una composición escalonada con conexiones visibles entre ellas.

### Mapa de tarjetas libres

Cada equipo se muestra como una tarjeta sobre un lienzo libre. La disposición inicial agrupa los equipos en clústeres (el equipo central a la izquierda y, a su derecha, cada switch con los equipos que cuelgan de él). Puedes mover cada tarjeta con el ratón o con las flechas del teclado (con la tarjeta enfocada); las posiciones movidas se recuerdan por capa en el navegador. Al hacer clic en una tarjeta se resaltan sus conexiones y las tarjetas vecinas. La casilla «Solo equipos con conexión» oculta los equipos sin enlaces en la capa elegida. Los archivos implicados son `Views/Topologia/Index.cshtml`, `wwwroot/js/topologia-grafica.js` y el bloque final «Mapa de tarjetas libres» de `wwwroot/css/site.css`.

Para despejar el esquema puedes **contraer** una tarjeta (queda solo su cabecera) o **ocultar el grupo** de un switch con el botón «Ocultar N» de su tarjeta; **Contraer todo** y **Colapsar grupos** lo hacen en bloque y redistribuyen el mapa. Estos estados solo valen durante la visita.
