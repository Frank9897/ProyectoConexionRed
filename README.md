# ProyectoConexionRed

## Evolución a inventarios independientes

La arquitectura se implementará por etapas para conservar el inventario actual `Catastro` y permitir iniciar una sede vacía o cargar un inventario documental. La base prevista es hasta tres inventarios SQLite independientes, seleccionados desde la aplicación; cada uno tendrá equipos, interfaces, conexiones, ubicaciones e historial propios. No se debe duplicar la base copiando archivos mientras la aplicación los tenga abiertos.

Orden acordado: (1) respaldo y preparación de migraciones; (2) migrar `Catastro` sin perder sus datos y retirar la inicialización rígida; (3) crear y seleccionar inventarios, con límite de tres; (4) alta de equipos y switches con interfaces configurables; (5) asignación de equipos a puertos y enlaces switch a switch, reflejados en la topología; (6) carga asistida de inventario y revisión integral. Cada etapa se presenta al usuario y se pausa hasta que responda «continua» antes de empezar la siguiente.

Estado de esta evolución: se creó y verificó una copia local `backup/Catastro.db`. Se agregó `Microsoft.EntityFrameworkCore.Design` 10.0.0 y se generó `Data/Migraciones/EsquemaInicial` a partir del modelo completo, incluidos valores predeterminados y la tabla de mantenimientos. En modo solo lectura se cotejaron sus 8 tablas de aplicación, índices y claves foráneas con el respaldo; `foreign_key_check` no encontró errores. El arranque valida la estructura de una base existente antes de registrar la migración inicial como línea base: coteja tablas, columnas, nulabilidad, valores predeterminados incompatibles, índices, claves foráneas e integridad SQLite. Acepta que una base histórica no defina un valor predeterminado que la aplicación suministra al guardar; rechaza un valor predeterminado diferente cuando la base sí lo declara. Rechaza esquemas incompatibles y migraciones desconocidas; solo después aplica migraciones pendientes. En una base vacía, EF Core crea el esquema mediante la migración inicial. `InicializadorInventario` ya no usa `EnsureCreated` ni crea tablas manualmente. La compilación completa de C# terminó correctamente y la línea base se comprobó sobre una copia temporal: se conservaron 198 equipos, 434 interfaces y 198 conexiones, y se registró `20261113061607_EsquemaInicial` sin ejecutar los `CREATE TABLE` iniciales. No se aplicó la migración a `Catastro` ni se modificaron el respaldo local ni la base activa. El respaldo está excluido por `.gitignore` y no viaja con un clon del repositorio.

Aplicación web en español para documentar y visualizar la infraestructura de red empresarial. El repositorio es público por decisión del usuario.

### Gestión de inventarios independientes (paso 3)

Se agregó la página **Inventarios**. `Catastro` sigue apuntando al archivo SQLite ya configurado y aparece como base principal; las nuevas sedes se crean como archivos separados en la carpeta definida por `RutaInventarios` (por defecto `inventarios`). El límite de tres incluye Catastro. La selección queda guardada en una cookie propia del navegador y la conexión EF Core se resuelve por solicitud, de modo que inventario, puertos, topología e historial operan sobre la base elegida. Crear una sede aplica las migraciones y la deja vacía: no copia ni importa datos de Catastro y no accede a la red. Los archivos nuevos están excluidos de Git. Para hosting, `RutaInventarios` puede apuntar a una carpeta persistente con permisos de escritura.

### Alta de equipos, switches y puertos (paso 4)

El formulario existente permite crear y editar equipos con IP, área, tipo, rol y otros datos documentales. Para un switch, se pueden indicar cantidades de RJ45, SFP, SFP+, QSFP, QSFP+ y QSFP28. Al guardar, se crean registros editables para las posiciones que falten; las etiquetas secuenciales son provisionales, el estado inicial es «A verificar» y el uso «Desconocido». No se inventan enlaces. Las posiciones se pueden corregir desde **Puertos**; al aumentar una capacidad se agregan las posiciones nuevas que falten. Si se reduce una cantidad, los registros previos se conservan para no borrar documentación o enlaces.

### Asignación contextual desde el frontal (paso 5)

En el detalle de un switch se puede seleccionar un puerto sin relación y con uso desconocido y abrir **Documentar conexión del puerto seleccionado**. El formulario preselecciona esa interfaz si continúa disponible; los puertos ocupados no permiten iniciar otra relación desde esa acción. El técnico confirma el destino y guarda explícitamente el registro. Esto documenta un extremo conocido, permite conexiones de acceso a PCs e impresoras sin inventar un puerto remoto y no ejecuta acciones sobre la red real. Los enlaces completos entre switches continúan registrándose con sus dos puertos mediante **Puertos**; solo aparecerán en topología cuando se guarden ambos extremos.

### Carga asistida a una sede nueva (paso 6)

Desde **Inventarios** se puede descargar una plantilla CSV, analizar un archivo y revisar la vista previa antes de confirmar. El importador admite UTF-8, separador punto y coma, hasta 1 MB, 500 fichas y 10.000 puertos derivados de capacidades. Los encabezados obligatorios son Equipo, Rol, Tipo y Vigencia; el resto de columnas técnicas puede quedar vacío. Las filas con campos inválidos o nombres repetidos bloquean la creación; las IP repetidas se señalan y requieren confirmación explícita. La carga crea otra base SQLite vacía y la completa en una transacción, conservando intacto Catastro y cualquier otra sede. Cada equipo importado deja un evento de historial. Las capacidades de switches generan interfaces provisionales en estado «A verificar» y «Desconocido». El CSV no importa conexiones, ubicación de usuario, asignaciones de usuario ni credenciales; los enlaces se documentan después desde la aplicación. La vista previa muestra nombre, tipo, modelo, ubicación, IP/MAC, rol, capacidades y vigencia para hasta 25 fichas. Las capacidades solo se aceptan en equipos de tipo Switch. Si Rol queda vacío en una fila «Puesto de usuario», se asigna «Usuario» como en el alta manual.

## Propósito

Reunir en un inventario navegable los dispositivos, IP, puertos, enlaces, áreas, PCs y usuarios de la red; mostrar su topología por vistas física, switching y red; registrar cambios; y permitir simulaciones locales y exportación imprimible.

## Estado del desarrollo

**MVP de inventario, acceso e historial en desarrollo.** La aplicación usa ASP.NET Core MVC, Entity Framework Core y SQLite. Incluye inicio, inventario con búsqueda por nombre, IP, área, piso, tipo de dispositivo, modelo o fabricante, alta y edición de fichas, exportación CSV e impresión de la tabla. Carga 25 registros iniciales cuando la base está vacía. El formulario de inventario prioriza datos técnicos y ubicación; los campos de usuario asignado, fuente documental y acceso administrativo no se capturan ni editan por ahora. Los valores ya registrados se conservan al editar. Tipo de dispositivo, rol y vigencia se eligen mediante listas. La tabla muestra el tipo de dispositivo y permite ver 10, 30, 100 o todos los registros, con navegación por páginas. La impresión incluye todos los resultados de la búsqueda aunque en pantalla haya una sola página visible. Los cambios de campos editables quedan auditados. La inicialización incorpora de forma idempotente puestos y ubicaciones del PDF a SQLite; el rol inicial de los puestos computacionales es Usuario y puede ajustarse manualmente, por ejemplo a Jefe.

El login está deshabilitado temporalmente para las pruebas: los módulos principales abren sin sesión. El filtro global de autorización está comentado en Program.cs; las acciones de ingreso y cierre llevan [NonAction], y la navegación pública no muestra controles de cuenta. La administración de usuarios sigue protegida por rol y no está disponible en este modo. Al final del proyecto se restaurará el login descomentando el filtro y quitando [NonAction]. La cuenta administradora inicial solo se crea cuando la base todavía no contiene usuarios y se configuran las variables de entorno `INVENTARIO_ADMIN_USUARIO` y `INVENTARIO_ADMIN_CLAVE`. La clave se guarda como hash. La cuenta inicial recibe rol Administrador. El administrador puede crear cuentas Técnicas, hasta cinco cuentas en total, y activar o desactivar cuentas. Las cuentas existentes reciben rol Administrador al actualizar una base anterior, para no quitar el acceso que ya tenían. El administrador no puede desactivar su propia cuenta activa ni la última cuenta administradora activa. La pantalla «Accesos» muestra los últimos cien inicios de sesión y cierres de sesión, con hora UTC y resultado; no registra contraseñas.

La aplicación permite crear y editar fichas de equipos y puestos; registra cambios y datos iniciales en un historial por usuario, fecha UTC, campo y valores anterior/nuevo. La página Topología resume equipos, switches clasificados, puestos e interfaces y presenta las capas física, switching y acceso en tarjetas adaptables. Cuando no hay enlaces, muestra un estado vacío informativo; cuando los hay, presenta extremos, puertos, medio, estado, fuente y fecha de revisión. La tabla de enlaces se alimenta solo de relaciones guardadas. Se incorporó la sección **Puertos** para registrar interfaces y enlaces físicos entre puertos existentes, con medio, módulos opcionales, fuente, estado de revisión y fecha real. Las listas usan fichas compactas con paginación independiente, 10 elementos iniciales y opciones de 10, 20, 30 o todos, más una transición sutil de 450 ms. Se evita reutilizar un puerto ya enlazado y se registra el alta en el historial de cambios. Esta pantalla solo modifica la documentación SQLite; no consulta ni cambia la red real. La corrección de interfaces y enlaces queda auditada campo por campo; la inicialización incorpora las filas documentadas del Visio V3.5 y actualiza con auditoría las relaciones cuyo origen documental coincide. Topología ahora dibuja equipos como nodos, enlaces físicos guardados como líneas y posiciones de puertos con estados ocupado, libre confirmado y desconocido. El selector de capas cambia entre vista física (todos los equipos), switching (switches y enlaces documentados entre ellos) y acceso (puestos más switches con enlace explícito a esos puestos); búsqueda y área se aplican en cada vista. El selector es una perspectiva de inventario: no modela todavía VLAN, administración ni rutas de capa 3. Usa iconos SVG locales y perfiles exactos de modelo; la ilustración genérica se aplica cuando el modelo no coincide o la capacidad no está especificada. Se agregaron capacidades de RJ45/SFP/SFP+/QSFP/QSFP+/QSFP28 al inventario de switches y el uso explícito de puertos. Los enlaces siguen siendo documentales, no pruebas de disponibilidad. La ficha SFP y la exportación gráfica siguen pendientes. Falta aplicar límites a intentos de acceso, verificar la aplicación y completar HTTPS antes de considerar un despliegue de producción.

Las fuentes base recibidas el 4 de noviembre de 2026 son **DIAGRAMA RED V3.0.vsdx** y **Diagrama UPS.vsdx**. La actualización más reciente es **DIAGRAMA RED V3.5.vsdx**, recibida el 12 de noviembre de 2026. El V3.5 contiene 22 páginas y 31 libros Excel incrustados; sus tablas se cotejaron por página, equipo, IP/MAC y puerto, porque el número de libro puede cambiar entre versiones. La importación idempotente actualiza las conexiones por fila de origen y deja los cambios auditados cuando se inicia la aplicación.

Se incorporaron los cambios del puerto 19 de Regularización Dominial (Regimen10, 192.168.0.197), puerto 24 de Valuación R0 (Switch Valuación R2, 192.168.1.20), puerto 12 de Valuación R1 (Valuacion37, 192.168.0.198) y la etiqueta del iDRAC conectado al puerto 16 del HP 1920. En el uplink Valuación R0 puerto 24 ↔ Valuación R2 puerto 23, V3.5 tenía IP remota incorrecta en ambas tablas; se normalizó con las IP del listado general (R0 .1.32, R2 .1.20), para que la topología pueda resolver el par. Se conserva la IP confirmada de Agrimensura 3Com (192.168.1.24), aunque el resumen de switches V3.5 escribe 192.168.0.24. También se conserva el enlace previo del S4148U puerto 21 a R770 P2 LAN (192.168.0.246), ya que Excel8 fila 22 indica R770/iDRAC (192.168.0.247), en conflicto con la aclaración previa de que el iDRAC está en HP 1920 puerto 16. La página «NOTIFICADORES» contiene la misma IP/MAC que el equipo RD (192.168.1.40); se mantiene esa identidad, y queda por confirmar el nombre del área. Las discrepancias visibles y el cotejo tabla por tabla están en [docs/ANALISIS_V35_CONEXIONES.md](docs/ANALISIS_V35_CONEXIONES.md).

En las tablas que nombran PCs o dispositivos finales, la posición local corresponde a acceso de capa 3; las conexiones entre switches corresponden a switching de capa 2; las relaciones físicas de rack, patchera y medios corresponden a capa 1. No se inventan puertos remotos ni medios cuando las tablas no los muestran. El V3.5 informa estados de algunas jaulas SFP, pero no identifica siempre el transceptor exacto; algunas relaciones UPS y tramos físicos todavía requieren completar sus extremos. El análisis previo del V3 está en [docs/ANALISIS_V3_CONEXIONES.md](docs/ANALISIS_V3_CONEXIONES.md).

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

El detalle individual de switch se imprime en A4 horizontal al ancho útil de la hoja. El diseño de impresión compacta el frontal, los datos y las conexiones; no reduce la página completa por la altura de la vista web. El mapa general omite los paneles ajenos al mapa y encaja las tarjetas y enlaces en casi toda el área imprimible (287 × 180 mm, con margen de 5 mm). El cálculo panorámico usa solo el mapa visible, puede ampliarlo por encima del 100 % y restablece el zoom de pantalla al cerrar la vista previa.


### Mapa navegable e impresión en una hoja

La topología mantiene el lienzo en un área visible estable, con desplazamiento interno para recorrer las tarjetas. El botón **Maximizar mapa** expande el panel y conserva filtros y controles de capa. La impresión del detalle del switch reparte las conexiones en columnas según su cantidad, conserva el margen A4 y mantiene el contenido al ancho natural de impresión. La impresión panorámica muestra el mapa en formato compacto en una página.


### Navegación visual del mapa

Cada tarjeta del mapa se puede mover libremente y sus conexiones la siguen. La primera carga usa un esquema jerárquico por niveles y muestra también los equipos documentados sin enlace dibujable. El mapa se navega arrastrando el fondo, con Ctrl + rueda o con la barra de zoom. **Restablecer mapa** borra las posiciones locales guardadas de las tres capas y vuelve al esquema inicial; **Encajar vista** encuadra las tarjetas visibles y **Maximizar mapa** amplía el lienzo dentro de la página. Las conexiones conservan sus colores por capa y se anclan al puerto documentado.


### Caja del mapa y expansión dentro de la página

Los controles principales del mapa están agrupados en su marco. Las herramientas de distribución, filtro de conexiones y colapso se guardan bajo **Más opciones** para reducir el ruido visual. Maximizar amplía la caja dentro de la página; no usa la pantalla completa del navegador y se restaura con el botón o Escape.

### Mapa de tarjetas libres

Cada equipo se muestra como una tarjeta sobre un lienzo libre. La disposición genérica coloca los equipos en niveles jerárquicos y conserva las relaciones documentadas como líneas. Las posiciones que se mueven se guardan por capa en el navegador; al restablecer, se elimina esa caché para todas las capas y se calcula nuevamente el esquema base. Al hacer clic en una tarjeta se resaltan sus conexiones y las tarjetas vecinas. Los archivos implicados son `Views/Topologia/Index.cshtml`, `wwwroot/js/topologia-grafica.js` y el bloque final «Mapa de tarjetas libres» de `wwwroot/css/site.css`.

Para despejar el esquema puedes **contraer** una tarjeta o **ocultar el grupo** de un switch desde el menú **Acciones** de esa tarjeta. Las acciones en bloque (**Contraer tarjetas**, **Expandir tarjetas**, **Ocultar grupos**, **Mostrar grupos**) están dentro de **Más opciones** y solo se aplican durante la visita. Los switches con 48 o más interfaces, incluido el Dell S4148U, usan una tarjeta de 700 px para distinguir mejor sus puertos. La disposición jerárquica mantiene más espacio entre niveles para que las líneas no queden apretadas.


### Revisión de pendientes documentales

La página distingue los campos que faltan de detalle sin presentar las conexiones del Visio como falsas. Se enumeran los extremos de Capa 2 sin puerto remoto cotejable, medios que no están especificados, módulos SFP/SFP+ que faltan confirmar, tramos del rack pendientes de cotejo y switches sin acceso administrativo (Régimen y Mesa de Entrada Principal). Un puesto de Capa 3 necesita el puerto del switch y la identidad del equipo final, no un puerto remoto de PC.

### Reinicio único del historial de cambios

A petición del usuario, al próximo inicio de cada base SQLite existente se eliminan las filas anteriores de `EventosModificacion`. La tabla `MantenimientosEjecutados` conserva una marca para que los siguientes arranques no repitan el borrado. El historial de accesos no se modifica. La página Cambios muestra un estado vacío hasta que se registre una modificación nueva.


### Validación de importación CSV (paso 7, 13 de noviembre de 2026)

Se corrigió un error de compilación de la vista previa: el modelo de fila no exponía el campo Piso que la vista Razor mostraba. El controlador ahora transfiere ese dato. También se eliminó una advertencia de acceso a un valor nullable en la preselección de conexiones. La compilación completa terminó correctamente; quedó una advertencia NU1900 porque este entorno no pudo consultar el servicio de vulnerabilidades de NuGet.

La carga se comprobó con una copia temporal aislada y datos ficticios: la vista previa respondió correctamente, la confirmación creó dos fichas y la página de inventario las mostró. Un puesto de usuario sin rol recibió el rol predeterminado Usuario; un switch de prueba creó las tres interfaces indicadas por sus capacidades. No se usó ni modificó Catastro. El servidor temporal y la copia de validación se retiraron al terminar. No se completó inspección visual del navegador porque la autorización de Computer Use expiró; la comprobación fue por HTTP y compilación.

## Navegación de inventarios (primer ajuste, 13 de noviembre de 2026)

La página **Inicio** ahora contiene la gestión de sedes: permite seleccionar el inventario activo, crear una sede vacía o preparar una carga CSV. Se retiró el contador de bases disponibles del contenido visual. El acceso anterior **/Inventarios** redirige a Inicio para conservar compatibilidad con enlaces guardados. La página **Inventario** y su tabla general siguen disponibles sin cambios. La barra principal ya no muestra dos enlaces llamados «Inventarios» e «Inventario»; muestra «Inicio» para gestionar sedes y conserva «Inventario» para consultar equipos. Próximo ajuste acordado: separar el inventario por Switches, Usuarios y Otros equipos con filtros/categorías derivados de sus datos.

### Inventario por secciones (paso 2, 13 de noviembre de 2026)

La tabla general conserva la búsqueda, edición, exportación e impresión, y ahora se puede filtrar por **Switches**, **Usuarios** y **Otros equipos**. Los switches tienen filtros para **Áreas**, **Datacenter**, **Reserva**, **Sin acceso** y **Servidores**. Los usuarios se separan entre **Documentados** y **Sin documentar**. La lista permite 10, 30, 100 o todos los registros; búsqueda y descarga CSV conservan sección y categoría seleccionadas.

La clasificación usa datos persistidos: tipo de equipo, área, rol operativo y estado administrativo. «Datacenter» usa el área Rack/Datacenter; «Reserva» usa área o rol de reserva; «Sin acceso» usa un estado administrativo que indique inaccesibilidad; «Servidores» usa el rol de switch de servidores o de puente al Dell R770. Los usuarios se reconocen por el tipo explícito de puesto/computadora. Se marca como documentado cuando el nombre o IP del usuario coincide exactamente con una conexión registrada desde un puerto de switch o cuando hay un enlace físico completo entre interfaces del switch y del equipo. No se deducen relaciones por nombres parecidos. Los demás tipos, incluidos impresoras, fotocopiadoras, scanners, plotters y tipo desconocido, quedan en Otros equipos.

El cambio se publicó directamente en `main`. Se revisó el código en GitHub, pero no se ejecutó compilación ni prueba de navegador en esta etapa.

### Ajustes de Topología (paso 3, 13 de noviembre de 2026)

La barra de herramientas dentro del marco del mapa contiene un solo botón **Maximizar mapa** y un solo botón **Imprimir mapa**; se retiraron las copias del encabezado y de los listados inferiores. Al abrir Topología, todas las tarjetas comienzan contraídas para que el mapa muestre primero su estructura y conexiones. **Restablecer mapa** también vuelve a esa presentación contraída, además de recuperar la disposición genérica. Se retiraron de la página general, debajo de Pendientes de detalle, los bloques «CAPA 1 · DETALLE DE CONEXIONES» y «DETALLE DE EXTREMOS POR INTERFAZ». Las relaciones y los datos guardados siguen disponibles en el mapa y en el detalle de cada switch; no se borró información.

El código se revisó en GitHub y quedó publicado en `main`. En esta etapa no se ejecutó compilación ni revisión visual del navegador.


### Selector y edición de puertos (paso 7)

La página **Puertos** presenta switches en tarjetas con IP, modelo, cantidad de interfaces inventariadas y conteos de ocupados, libres confirmados y desconocidos. Los conteos se calculan sobre registros de interfaces reales de la base documental, no sobre la capacidad teórica del modelo. Los destinos aparecen solo cuando están documentados. **Editar puertos** abre el mismo detalle visual del switch usado por Topología; desde allí, al seleccionar una interfaz inventariada se activa su edición. Los puertos sintéticos de un perfil que aún no existen como registros no se pueden editar hasta inventariarlos.

Desde el detalle se pueden editar o eliminar enlaces completos y relaciones parciales. Las eliminaciones solicitan confirmación y guardan un evento de auditoría; no afectan el equipo real. Un puerto no se elimina mientras tenga una relación completa o parcial: primero hay que editar o eliminar esa relación. Después de agregar un puerto o guardar una conexión completa, la aplicación regresa al frontal del switch correspondiente. La aplicación sigue sin consultar ni modificar la red de trabajo.

**Alcance de verificación de este paso:** los cambios se publicaron directamente en la rama principal del repositorio. No se ejecutó compilación, pruebas ni recorrido en navegador en este paso.

### Búsqueda de switches en Puertos

La sección **Puertos** permite filtrar las tarjetas localmente por texto (nombre, área, modelo, IP o destino visible) y por conexiones registradas, puertos desconocidos, confirmación o ausencia de conexiones. El contador indica cuántos switches coinciden. El filtrado se ejecuta en el navegador sobre la información ya presentada y no genera tráfico hacia la red de trabajo.

### Validaciones al editar puertos y enlaces

La identidad de un puerto (equipo propietario y etiqueta) queda protegida mientras tenga un enlace completo o una relación parcial. Para moverlo o renombrarlo, primero se debe resolver la conexión correspondiente. Tampoco se puede marcar como «Libre confirmado» un puerto que tenga una relación. Al crear un enlace, los dos extremos deben ser distintos, existir en la base, no estar enlazados ni tener una relación parcial, y no estar marcados como libres confirmados. Al corregir enlaces, se valida también que un extremo nuevo no esté ocupado por otra conexión ni tenga una relación parcial; los extremos existentes se pueden conservar para editar sus metadatos.


Corrección de compilación: el formulario de edición de relaciones parciales incluye el identificador del equipo propietario para construir el enlace de regreso al detalle del switch.

### Retorno contextual desde el detalle del switch

Al editar o eliminar un enlace desde la ficha de un switch, la acción guarda ese equipo como contexto de navegación. El servidor verifica que el switch indicado sea realmente uno de los extremos del enlace; si no participa, selecciona otro switch del mismo enlace y, si no hay ninguno, regresa a Puertos. Al editar, la opción de regreso se conserva en el formulario y en sus errores de validación.
