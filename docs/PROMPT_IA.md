# Contexto permanente para la IA — ProyectoConexionRed

## Objetivo
Crear una aplicación web en español para inventariar, consultar y visualizar la red empresarial documentada por el usuario. Debe enlazar equipos, IP, interfaces, conexiones, ubicación y usuarios/PCs con una topología navegable, auditoría de cambios, simulaciones aisladas y exportación imprimible.

## Estado actual del proyecto
- Repositorio público por decisión explícita del usuario.
- Hay un MVP inicial en C# / ASP.NET Core MVC con EF Core y SQLite.
- Incluye página de inicio, inventario con búsqueda por nombre/IP/área/piso/tipo/modelo/fabricante, CSV de resultados filtrados, impresión de la tabla y 25 registros semilla.
- El inventario permite alta/edición de datos técnicos y ubicación. Por ahora no debe capturar ni editar usuario asignado, fuente documental ni acceso administrativo; los valores previos de esos campos se preservan al editar. Tipo de equipo, rol y vigencia se seleccionan de listas. La tabla muestra el tipo de dispositivo y ofrece 10, 30 o todos los registros, con botones de página anterior/siguiente; al imprimir se muestran todos los resultados filtrados. Las fichas nuevas y sus valores iniciales quedan auditados. Las propiedades nuevas se agregan con una actualización de esquema compatible con bases SQLite anteriores. Se cargaron 170 fichas de puestos y ubicaciones del PDF de forma idempotente; el V3 prevalece para IP y las diferencias quedan como observaciones.
- MODO TEMPORAL SIN LOGIN: las páginas principales se abren sin autenticación para pruebas. En Program.cs se comenta el filtro MVC global; las acciones GET/POST de ingreso y salida quedan marcadas [NonAction]; _Layout muestra navegación pública sin cuenta ni enlace a Usuarios. UsuariosController conserva el requisito de rol Administrador, por lo que la gestión de cuentas no está disponible durante este modo abierto. Para restaurar el login al final del proyecto, descomentar el filtro global, quitar [NonAction] de CuentaController y reponer la navegación autenticada.
- Las claves se almacenan como hash. La tabla de accesos registra ingresos correctos/fallidos y cierres con hora UTC; no guarda contraseñas.
- La tabla de accesos tiene una página para consultar los últimos cien eventos.
- Las tablas de usuarios e historial se crean con SQL IF NOT EXISTS para compatibilidad con la base prototipo creada en la primera versión. El resto del modelo usa EnsureCreated; emplear migraciones en futuras evoluciones del esquema.
- La gestión básica de cuentas está incorporada: rol Administrador/Tecnico, alta de técnicos (máximo cinco cuentas totales) y activación/desactivación. Las cuentas previas reciben rol Administrador por compatibilidad al agregar la nueva columna SQLite; el alta por la interfaz siempre asigna Tecnico. Se protege la última cuenta administradora activa y no se permite auto desactivación.
- La edición controlada de equipos y la auditoría por campo están implementadas en el código: se guardan cuenta, fecha UTC, equipo, campo, valor anterior y valor nuevo. La pantalla Cambios muestra los últimos doscientos eventos. La edición solo modifica la base documental y no actúa sobre la red real.
- Hay un esquema SQLite para interfaces físicas y enlaces entre puertos. La sección «Puertos» permite registrar interfaces y enlaces entre dos puertos existentes, elegir tipo/medio, estado documental, fuente, módulos SFP opcionales, fecha real y observaciones. Las dos colecciones se muestran en fichas compactas en lugar de filas tabulares densas; comienzan con 10 elementos y permiten mostrar 10, 20, 30 o todos, con flechas y una transición sutil de 450 ms. Permite corregir interfaces y enlaces con validación de equipo/puerto, evita duplicar puertos o reutilizar extremos enlazados, separa la falta de datos de la conectividad y audita cada campo cambiado en EventosModificacion. Los formularios solo cambian el inventario documental y no consultan ni modifican la red real. Topología presenta indicadores, nodos SVG locales, capacidad y estados de puertos; sus aristas solo provienen de enlaces guardados. El selector funcional muestra vista física completa, switching (switches y enlaces entre ellos) y acceso (puestos y switches con enlace explícito a esos puestos), junto con búsqueda, filtro de área, resumen dinámico y detalle de enlaces. Estas perspectivas filtran el inventario existente; no afirman VLAN, rutas de capa 3 ni estado en vivo. No inferir conexiones por área, nombre o proximidad. Las jerarquías administrativas/VLAN, simulaciones, ficha SFP y exportación gráfica siguen pendientes. La cookie se valida contra SQLite en cada solicitud: una cuenta desactivada pierde acceso en la siguiente solicitud y un cambio de rol se renueva en la sesión. La marca aleatoria del proceso invalida las cookies anteriores tras un reinicio. No desplegar en producción antes de verificar la aplicación, controles de acceso, HTTPS y pruebas.
- El 4 de noviembre de 2026 se recibieron y revisaron `DIAGRAMA RED V3.0.vsdx` y `Diagrama UPS.vsdx`. El inicializador incorpora de forma idempotente las MAC explícitas del V3 cuando coinciden la etiqueta/IP y el campo MAC está vacío; reconcilia el registro Aruba 1930 principal de Mesa de Entradas en `192.168.1.1`, completa Aruba como fabricante del switch de `192.168.1.17` y da de alta una sola vez el Aruba 1930 Reserva 2. Registra estos cambios en `EventosModificacion` bajo «Carga documental DIAGRAMA RED V3.0». No reemplaza IP, MAC ni fabricante preexistentes. El usuario confirmó que V3.0 fue verificado switch por switch e interfaz por interfaz: usarlo como fuente principal para enlaces, interfaces e IP que documente. No inventar conexiones de rack ausentes o ambiguas. Los datos de UPS, almacenamiento y servidores todavía no se importaron. Las tablas Excel incrustadas de V3 se analizaron para preparar el registro manual y la futura carga controlada. No cargar enlaces ambiguos como confirmados; la IP ProCurve vigente indicada por el usuario es `10.0.1.1`, pese a una celda discrepante en una hoja incrustada. Mesa de Entrada principal es Aruba 1930 `192.168.1.1` y su acceso administrativo es inaccesible. La tabla de Mesa de Entrada secundaria registra MENTRADA04, MENTRADA02 y el puerto 24 hacia el Aruba principal.
- Se recibió `UBICACION-EQUIPOS-OCTUBRE 2026.pdf` y se revisaron sus 14 páginas. Contiene puestos con etiquetas de equipo, asignaciones de usuario y ubicación por área/piso. El usuario aclaró que el número entre paréntesis es el último octeto de la IP de usuario dentro de `192.168.0.0/24`; por ejemplo, `(69)` representa `192.168.0.69`, no un número de inventario. El PDF es fuente de ubicación/usuario y puede estar menos actualizado que V3.0; si ambos contienen datos de red distintos, prevalece V3.0. Los puestos se importan desde el PDF con IP V3 prioritaria y usuario real sin asignar. Cuando lleguen los Visios de PC/usuarioPC, completar la identidad con su fuente; no inferir asociaciones por ubicación gráfica.
- El usuario anticipó otros dos Visios para completar o validar nombres de PC, identificadores usuarioPC y nombres reales de usuario; incorporarlos cuando los entregue y conservar procedencia por dato.
- Una página de `DIAGRAMA RED V3.0.vsdx` contiene credenciales escritas. Nunca transcribirlas a código, inventario, historial, README, prompt ni commits. El usuario fue advertido y debe cambiar las claves que correspondan antes de volver a distribuir el diagrama.
- Se retiró de `Program.cs` el bloque temporal que imprimía un hash y detenía el arranque cuando faltaba la variable de contraseña. La generación de contraseñas continúa únicamente en el alta inicial y en la creación de cuentas mediante los servicios de la aplicación.
- Al modificar el código, actualizar siempre README.md y docs/PROMPT_IA.md.
- No se ha confirmado con el usuario que el programa compile ni se han verificado las páginas en ejecución; no afirmar que están verificadas. La administración de cuentas y el manejo de bases anteriores requieren esa verificación pendiente.

### Refactor de código aplicado
- `Services/UtilidadesConexiones.cs` concentra normalización de texto, auditoría por campo y consultas para selectores de equipos/interfaces.
- Los formularios de puertos y enlaces comparten modelos base con las validaciones y parciales Razor de alta y corrección; sus reglas CSS están consolidadas en un solo bloque.
- Se corrigieron las llamadas de auditoría para pasar identificador y nombre del equipo de forma uniforme.
- La revisión fue estática sobre la rama GitHub; no se confirmó compilación .NET en esta sesión.

### Topología gráfica y estados de puerto implementados
- El mapa genera nodos para equipos inventariados y líneas solo para enlaces documentados con ambos extremos válidos; no inferir relaciones por área, nombre o proximidad.
- Los switches pueden tener capacidad manual por grupo RJ45, SFP, SFP+, QSFP, QSFP+ y QSFP28. Si fabricante/modelo coincide con un perfil local exacto, la capacidad verificada del perfil se usa si no fue sobrescrita; modelo impreciso o no conocido usa icono genérico.
- Perfil registrado: Dell / S4148U-ON, con 48 SFP+, 2 QSFP+ y 4 QSFP28, basado en el manual oficial de [Dell](https://www.dell.com/support/manuals/en-uk/networking-s4148f-on/s4100f_t_u_on_install_pub/features?guid=guid-0de00e36-0afd-4bc1-848f-10243d7075ec&lang=en-us). La figura es una ilustración esquemática; las posiciones reales se dibujan según el perfil.
- `InterfazEquipo.EstadoUso` acepta `Desconocido` o `Libre confirmado`. La existencia de un enlace prevalece visualmente como ocupado en la documentación. `EstadoDatos` conserva por separado la vigencia de la información. Una interfaz se asigna a una posición de perfil solo cuando etiqueta y tipo coinciden exactamente al normalizar espacios y mayúsculas; las no coincidentes se muestran por su etiqueta original.
- Colores: verde para puerto usado por un enlace guardado, gris oscuro para libre confirmado y verde oscuro para desconocido. La página explica que ocupado documental no significa disponibilidad en vivo.
- La actualización SQLite agrega columnas de capacidad y uso de forma idempotente para bases anteriores. SVG y scripts se alojan localmente; no se descargan imágenes cuando se carga Topología.

## Fuentes y calidad de datos
- Fuente base: DIAGRAMA RED V2.vsdx.
- Confirmación del usuario: todos los dispositivos y las IP asignadas en el Visio son reales y están actualizados.
- Los dispositivos de usuario y sus conexiones de acceso también están actualizados según el usuario.
- Solo algunas conexiones del rack están desactualizadas; señalarlas para revisión. No generalizar ese estado a equipos, IP ni conexiones de usuario.
- Algunos SFP no están confirmados. Registrar como pendientes de actualización/sin confirmar; la fecha de última revisión debe ser real y conocida. Nunca inventar fecha, modelo, tipo o medio.
- Dos switches son inaccesibles administrativamente por falta de credenciales: área Régimen y switch principal de Mesa de Entradas. Esa condición no significa que estén apagados ni que haya una falla de red.
- «Principal» y «No principal» expresan jerarquía de switches dentro de un área. El switch «No principal» de Mesa de Entradas enlaza al principal del área y conecta tres equipos de usuario a través de él.
- Mantener separados en el modelo los datos de acceso administrativo, vigencia y disponibilidad física; el acceso administrativo no se edita ni muestra en el inventario durante esta etapa.
- El usuario autoriza publicar documentación e IPs del inventario en el repositorio público. No publicar contraseñas, claves, tokens ni otros secretos de autenticación.

## Datos iniciales cargados
Los datos de la lista se transcriben del Visio y las IP son vigentes según el usuario. No inferir enlaces de rack a partir de su presencia en el inventario.

### Equipos de rack y núcleo
| Etiqueta | IP |
|---|---|
| Switch Aruba 1930 (WDGC) | 192.168.1.19 |
| HP ProCurve 29100I-24G | 10.0.1.1 |
| Switch HP 5500 | 192.168.1.218 |
| Switch HP 1920 | 192.168.1.115 |
| Dell S4148U-ON 48G | 192.168.1.44 |
| Switch HP 1920S | 192.168.1.217 |
| Aruba 1930 (reserva) | 192.168.1.200 |
| Cisco Catalyst 3560 | No indicado |

### Fichas de áreas y otros equipos
| Etiqueta | IP |
|---|---|
| Switch Agrimensura 1 HP | 192.168.1.23 |
| Switch Agrimensura 3Com | 192.168.1.24 |
| Asuntos Jurídicos | 192.168.1.30 |
| Cartografía | 192.168.1.51 |
| Inmuebles | 192.168.1.26 |
| Mesa de Entrada (No principal) | 192.168.1.18 |
| Puerta Agrimensura | 192.168.1.19 |
| Regularización Dominial | 192.168.1.40 |
| Secretaría | 192.168.1.28 |
| Sistemas | 192.168.1.216 |
| Valuación R0 | 192.168.1.32 |
| Valuación R1 | 192.168.1.29 |
| Valuación R2 | 192.168.1.20 |
| Switch de área sin identificar | 192.168.1.1 |
| Switch de área sin identificar | 192.168.1.17 |
| Switch de Régimen | No indicado |
| Switch principal de Mesa de Entradas | No indicado |

- El Visio repite 192.168.1.19 en dos etiquetas. Preservar ambas etiquetas y sus datos como aparecen. No deducir que es un error ni asumir si se trata de un equipo duplicado.
- El Visio también muestra un módem Claro y patcheras; no están cargados como registros en el MVP actual. No asignarles IP ni enlace sin fuente.
- No incluir credenciales del Visio en registros semilla, código, comentarios o documentación.

## Fuentes actualizadas de noviembre de 2026

`DIAGRAMA RED V3.0.vsdx` fue indicado por el usuario como la fuente vigente al 4 de noviembre de 2026. Los dispositivos e IP son reales. Algunas conexiones de rack siguen pendientes de revisión. La lectura de texto del archivo permite recuperar etiquetas y conectores explícitos, pero no confirma todas las líneas dibujadas; no completar enlaces a partir de posiciones o de la apariencia sin validación.

### MAC confirmadas en DIAGRAMA RED V3.0

| Etiqueta del equipo | IP | Dirección MAC |
|---|---|---|
| Switch HP 5500 | 192.168.1.218 | 2C:23:3A:28:1C:25 |
| Switch HP 1920 | 192.168.1.115 | 40:B9:3C:2A:C0:7D |
| Switch HP 1920S | 192.168.1.217 | 20:67:7C:4E:9E:54 |
| Dell S4148U-ON 48G | 192.168.1.44 | E4:F0:04:A4:83:BD |
| Switch Agrimensura 1 HP | 192.168.1.23 | 5C:8A:38:9F:19:92 |
| Switch Agrimensura 3Com | 192.168.1.24 | 00:24:73:CA:75:04 |
| Asuntos Jurídicos | 192.168.1.30 | 78:48:59:72:6C:C5 |
| Cartografía | 192.168.1.51 | 44:5B:ED:12:3E:25 |
| Inmuebles | 192.168.1.26 | B8:AF:67:9B:8E:DD |
| Mesa de Entrada (No principal) | 192.168.1.18 | 5C:8A:38:9F:1A:32 |
| Puerta Agrimensura | 192.168.1.19 | 5C:8A:38:9E:80:52 |
| Regularización Dominial | 192.168.1.40 | B8:AF:67:9A:C5:9E |
| Secretaría | 192.168.1.28 | 40:B9:3C:2A:BF:7D |
| Sistemas | 192.168.1.216 | 20:67:7C:4C:44:A8 |
| Valuación R0 | 192.168.1.32 | 78:48:59:73:47:83 |
| Valuación R1 | 192.168.1.29 | 78:48:59:7C:E6:33 |
| Valuación R2 | 192.168.1.20 | 44:31:92:9E:54:20 |
| Switch Aruba 1930 (Reserva 2) | 192.168.1.201 | 94:60:D5:36:DF:4C |

La dirección `192.168.1.19` continúa apareciendo para dos etiquetas distintas, Switch Aruba 1930 (WDGC) y Puerta Agrimensura. Preservar ambas filas; no resolver ni corregir el duplicado por inferencia. El Aruba 1930 de reserva 2 es un registro adicional al equipo de reserva ya listado con IP `192.168.1.200`.

En la página de switches de área, el usuario confirma que `192.168.1.1` es el Aruba 1930 principal de Mesa de Entradas; `192.168.1.17` se identifica como Aruba, pero su modelo/área queda sin confirmar.

El V3 etiqueta el HP ProCurve 29100I-24G con IP `10.0.1.1`, acceso SSH y credenciales desconocidas. Mantener su acceso administrativo como pendiente/no confirmado hasta que el usuario aclare si actualmente es inaccesible. No confundirlo con los dos switches previamente confirmados como inaccesibles (Régimen y Mesa de Entradas Principal).

### Equipos de rack y energía observados

`DIAGRAMA RED V3.0.vsdx` y `Diagrama UPS.vsdx` incluyen etiquetas para módem Claro, módem Personal, equipos INTELLINET, patcheras y equipos Dell identificados como servidores, almacenamiento y switches de rack. Los modelos o etiquetas visibles incluyen Dell R740 (variantes rotuladas O y P), R770, R920, SCV320, SCV300, SCV3000, Dell DS 6610B y Dell DS 300B. Registrar solo campos expresamente legibles y conservar la fuente; IP, MAC, cantidades y enlaces no indicados quedan vacíos o pendientes. No interpretar identificadores de forma como cantidad de unidades hasta resolver si corresponden a vistas frontal/trasera o a elementos duplicados.

El diagrama UPS identifica UPS 1 a UPS 12 y marca UPS 10 como «NO OPERATIVA». También aparecen etiquetas de modelos o familias `SRV3KRI-AR`, `APC RC2000`, `APC RT 10000`, `SUA3000RMI2U`, `SRV1KRLAR`, `SMT3000RMRU`, `UPS EATON`, `UPS APC EASY` y `TRV`. El texto extraído no permite asociar con certeza todos esos modelos a un número de UPS; no inventar esa correspondencia. Hay varias zapatillas de rack con posiciones/etiquetas, pero sus tomas y cargas requieren revisión visual antes de cargar circuitos eléctricos.

## Lectura de DIAGRAMA RED V3.0 y correcciones de clasificación

La inspección del paquete V3.0 localizó 22 páginas y 31 hojas Excel incrustadas. La relación página/hoja se registró en [docs/ANALISIS_V3_CONEXIONES.md](ANALISIS_V3_CONEXIONES.md). La hoja de posiciones suele describir el puerto del switch y el equipo/IP conectado; no suele especificar el puerto físico del extremo remoto ni el medio del cable.

El usuario confirmó que el campo RolEnArea significa función operativa, no modelo: Switch de área, Switch de servidores, Distribución de switches de área, Puente hacia Dell R770 principal previsto, Principal o No principal. Mantener aparte TipoDispositivo, fabricante y modelo. El HP 5500 es switch de servidores; el HP 1920 concentra switches de áreas; el Dell S4148U sirve de puente hacia el Dell R770 previsto como servidor principal. Los switches nombrados por Agrimensura, Valuación, Sistemas u otra oficina deben clasificarse como switches de área cuando la propia ficha o página los identifica como switch.

La inicialización corrige los registros semilla de las áreas, reconcilia el Aruba 1930 principal de Mesa de Entradas con IP 192.168.1.1 y conserva el acceso administrativo inaccesible. La corrección queda auditada y protege fichas duplicadas que ya tengan interfaces.

Discrepancias extraídas que deben seguir visibles hasta resolverse:
- Una tabla incrustada en la página Puerta Agrimensura escribe 10.0.1.0 para el ProCurve; el usuario corrigió expresamente la IP vigente a 10.0.1.1. Usar 10.0.1.1 como dato vigente y conservar la discrepancia como nota de fuente, no como IP alternativa.
- El resumen de switches de áreas escribe 192.168.0.24 para el 3Com, mientras otras fichas/cargas lo registran como 192.168.1.24. No reemplazar el dato canónico sin una comprobación.
- El S4148U registra el R770/iDRAC 192.168.0.247 en el puerto 21; la tabla del HP 1920 también asocia esa IP al puerto 16. Puede ser doble conexión o un registro duplicado; no inventar redundancia.
- Los rótulos LIBRE, DESCONOCIDO, OCUPADO, SIN SFP y SFP LIBRE describen señales distintas. Separar uso físico, vigencia y presencia del transceptor. No concluir que un módulo SFP está instalado por el solo hecho de que exista una bahía.
- En Mesa de Entradas no principal, la hoja marca MENTRADA04 en puerto 1, MENTRADA02 en puerto 3, y el Aruba principal en puerto 24. Son dos puestos más el enlace al principal.

Las posiciones de puertos de las tablas V3 ya se importan a SQLite con página/libro/fila. Mantener vacíos los puertos remotos y el medio si la fuente no los confirma. La gráfica final debe enseñar número/etiqueta y estado en cada puerto, más el equipo/IP conectado en los puertos ocupados; el trazo debe anclarse al puerto local conocido. Si falta el puerto remoto, expresarlo como no documentado. Mantener un dibujo esquemático separado de fotos de fabricante.

## Arquitectura y alcance acordados
- Backend: C# con ASP.NET Core MVC y Entity Framework Core.
- Base inicial: SQLite; se espera un técnico y hasta cinco técnicos con lectura/escritura. Revisar PostgreSQL o SQL Server si aumentan concurrencia o instancias de aplicación.
- El servidor web será la vía de acceso a SQLite; no compartir el archivo directamente por una unidad de red.
- Las vistas física, switching y red serán perspectivas del mismo inventario.
- La impresión disponible corresponde al inventario tabular; no presentarla como exportación de topología.
- La cookie de autenticación es HTTP Only; en producción se requiere HTTPS. No hay alta pública de usuarios. La pantalla Usuarios solo es visible y accesible para Administrador; las altas nuevas son Tecnico y el total de cuentas queda limitado a cinco.
- Secciones futuras: inventario, topología, puertos/conexiones, switching/VLAN, IP/subredes, PCs/usuarios, auditoría, simulaciones, informes/impresión y administración.
- Simulaciones solo sobre el modelo de datos de la aplicación. No mandar pings, comandos ni cambios a la red real.
- Las imágenes deben corresponder a un modelo identificado y registrar su fuente; no presentarlas como verificadas por similitud de nombre.

## Orden de trabajo actualizado

1. Importar de forma controlada las relaciones del V3: puerto local, destino conocido si existe y fuente de página/libro/fila. Los puertos remotos y medios faltantes quedan vacíos o «No confirmado».
2. Revisar los pares recíprocos del rack y las discrepancias restantes con el usuario antes de convertirlos en enlaces completos.
3. Separar de forma estructurada el estado de ocupación del puerto y la presencia/modelo/fecha del transceptor SFP.
4. Enriquecer PCs/usuarios con los dos Visios pendientes y luego completar jerarquía de switching/VLAN con relaciones confirmadas.
5. Agregar simulaciones exclusivamente sobre datos locales y exportación/impresión gráfica.
6. Verificar bases SQLite previas, migraciones, respaldos, HTTPS y permisos; restaurar el login al finalizar el proyecto.

La entidad SQLite `ConexionDocumentada` ya admite puerto local obligatorio, destino remoto opcional, medio/estado, fecha y procedencia (página, libro y fila). No sustituye `EnlaceFisico`, conserva la compatibilidad con los enlaces de dos puertos y crea la tabla faltante al iniciar bases SQLite existentes. El puerto se muestra ocupado; sin extremo remoto validado, no se dibuja una arista.

## Auditoría requerida
- El MVP registra accesos correctos/fallidos y cierres de sesión con usuario, evento, resultado y fecha UTC.
- Las fichas de equipos registran usuario, fecha UTC, equipo, campo, valor anterior y nuevo; la pantalla Cambios presenta hasta doscientos eventos recientes.
- Cambios: usuario, fecha/hora, entidad, campo, valor anterior y nuevo.
- Ejemplo visible: «Usuario modificó IP de Equipo: 192.168.0.120 → 192.168.0.127».
- Nunca registrar contraseñas en texto claro ni incluirlas en ejemplos o historial.

## Reglas obligatorias de desarrollo
1. Variables en español.
2. Comentar cada variable en español, explicando qué representa y qué efecto tendría cambiar su valor.
3. Documentar cada función en español: qué hace y cómo cambian los resultados si cambian los parámetros.
4. Mantener comentarios y documentación en español; conservar los nombres propios de APIs y estándares.
5. Actualizar siempre README.md y docs/PROMPT_IA.md junto con las modificaciones.
6. Mensajes de commit breves y en español.
7. No inventar datos ni presentar como implementado o verificado lo que el repositorio no demuestra.
8. No incluir secretos aunque el repositorio sea público.
9. No probar ni modificar la red de producción.


## Ajustes de topología confirmados por el usuario

- Referencia visual: frontal metálico esquemático con puertos numerados en dos hileras, inspirado en la imagen compartida; no presentar una ilustración genérica como fotografía real del modelo.
- Cada switch debe abrir una página de detalle con su frontal y sus enlaces, además de aparecer en el mapa general.
- Colores del frontal: ocupado en verde claro; libre confirmado en gris oscuro; estado desconocido en verde oscuro translúcido. El color siempre lleva texto accesible/ayuda emergente.
- La ocupación del puerto y la presencia/modelo/fecha del SFP son datos independientes.
- Correcciones del usuario: ProCurve `10.0.1.1`; 3Com `192.168.1.24`; HP 1920 puerto 16 al R770 P1/iDRAC `192.168.0.247`; S4148U puerto 21 al R770 P2/LAN `192.168.0.246`; R770 P4 sin configurar. El medio se conserva como no confirmado.
- No inferir puerto remoto ni medio de cable por el tipo de bahía SFP o por el modelo de switch. Los enlaces de rack dudosos siguen «A verificar».
- Mantener variables, comentarios y documentación en español; cada función nueva explica su objetivo y el efecto de cambiar parámetros. Actualizar README y este prompt con cada avance.


## Estado de importación documental V3 (9 de noviembre de 2026)

- El arranque incorpora de forma idempotente 416 interfaces procedentes de 18 tablas del archivo `DIAGRAMA RED V3.0.vsdx`: 320 posiciones de área y 96 de cuatro switches de rack con propietario claro.
- Para cada posición se conserva página, libro Excel incrustado y fila fuente. Las conexiones de rack quedan «A verificar» salvo HP 1920 puerto 16 ↔ Dell R770 P1/iDRAC .247 y S4148U puerto 21 ↔ Dell R770 P2/LAN .246, confirmadas por el usuario. Esos dos pares se guardan como enlaces completos y se dibujan; el medio sigue sin confirmar. Las otras referencias parciales no generan líneas.
- Las filas de puestos se consideran vigentes según la confirmación del usuario. ProCurve se normaliza a `10.0.1.1` solo en la fila discrepante de Puerta Agrimensura y se anota la corrección de la fuente.
- No inferir fecha de revisión ni módulos SFP instalados. Las interfaces guardan tipo «No confirmado» salvo las posiciones SFP+ del perfil exacto S4148U. Mantener las filas de rack LIBRE, SIN SFP, SFP LIBRE y OCUPADO como datos de distinta naturaleza; no presentar las filas libres del rack como libres confirmadas. No sobrescribir una interfaz ya enlazada o marcada «Libre confirmado».
- El switch secundario de Inmuebles se incorpora como `SWITCH DE 8 PUERTOS`, sin fabricante, modelo, IP ni tipo de conector inventados. Se incluyen únicamente las posiciones 1–8 de Excel16. Las filas 17, 18 y 22 contradicen la capacidad declarada y quedan excluidas. La relación desde el puerto 24 del switch principal de Inmuebles permanece «A verificar» por ser conexión de rack.
- Las cuatro tablas claras de rack (HP 5500, HP 1920, HP 1920S y S4148U) se incorporan. Las 33 filas LIBRE de rack quedan a verificar; 15 SIN SFP y 3 SFP LIBRE se guardan como observaciones, y 3 OCUPADO se registran sin inventar su destino. Siguen pendientes las hojas SFP independientes de propietario ambiguo y otras tablas sin propietario resuelto.
- Se agrega Dell R770 como nodo sin IP administrativa atribuida. Se documentan P1/iDRAC .247, P2/LAN .246, P3 LIBRE y P4 NO CONFIGURADO; solo los enlaces P1 y P2 confirmados por el usuario se trazan.
- La ordenación del frontal muestra primero las etiquetas numéricas en orden natural (1, 2, …, 10) y luego etiquetas sin número.


## Importación de ubicaciones del PDF (9 de noviembre de 2026)

- Incorporar las 170 fichas únicas extraídas de `UBICACION-EQUIPOS-OCTUBRE 2026.pdf`, con nombre, área, piso, páginas fuente y el octeto interpretado como `192.168.0.x`.
- `DIAGRAMA RED V3.0` prevalece para la IP. Anotar en observaciones las discrepancias del PDF; no reemplazar por el valor del PDF. En la importación actual: 125 IP V3 resueltas, 16 discrepancias, 44 IP provenientes solo del PDF con estado `A verificar` y `Sistemas23` sin IP principal por dos candidatas V3 no resueltas.
- No asignar personas a puestos; esperar los dos Visios que contienen PC, usuarioPC y usuario real.
- El usuario confirma que `T7200` corresponde a `PLOTTER EPSON T7200` y `TrimbleNET-R9` a `TRIMBLE TUC1`; usar los nombres canónicos V3 y guardar las etiquetas del PDF como observación. T7200 queda con IP V3 `.202` y discrepancia PDF `.189` anotada.
- El origen tipado C# es una carga de respaldo idempotente hacia SQLite, no una segunda base operativa. No subir el archivo `.db`. Si ya existe una ficha, conservarla y no sobrescribir sus cambios.
- Registrar el alta de cada puesto en el historial de modificaciones. No ejecutar pings, consultas ni cambios en la red real.


## Roles de puestos y experiencia de conexiones (10 de noviembre de 2026)

- El rol predeterminado de los equipos de tipo `Puesto de usuario` es `Usuario`. El técnico puede cambiarlo manualmente, por ejemplo a `Jefe`. Si la ficha existente tenía un rol personalizado, la importación no lo reemplaza. Los relojes y periféricos mantienen funciones propias.
- En switching, mostrar dos tarjetas por fila en escritorio y una en móvil. Diferenciar ocupado, libre confirmado y desconocido con color y borde; incluir conteos por switch y mantener accesible el estado por texto.
- En topología, conexiones y detalle de switch, mostrar nombre del equipo y su IP debajo en gris, junto con puerto y estado. Ocultar el medio hasta estar documentado y confirmado. No mostrar página/libro/fila en una columna por registro; ofrecer el respaldo bajo demanda.
- Las listas deben buscar por equipo/IP/puerto, filtrar estado, paginar y enlazar directamente al frontal cuando el equipo sea un switch. Ofrecer impresión desde las vistas de topología.
- Mantener las relaciones incompletas como pendientes. No crear aristas, medios o puertos remotos por inferencia.


## Interacción del frontal y fichas de conexión (10 de noviembre de 2026)
- En detalle del switch, los puertos del frontal se seleccionan con ratón, Enter o Espacio. Se anuncia el estado y destino documental, se destaca la ficha correspondiente y se desplaza hasta ella cuando la selección parte del puerto.
- Al seleccionar el cuerpo de una ficha, se resalta el puerto asociado por su etiqueta local. Los enlaces y controles internos conservan su acción normal.
- La asociación usa solo etiquetas del puerto local documentadas; no deducir destino remoto, medio ni conectividad actual. Si la conexión no tiene puerto remoto confirmado, mantenerlo explícitamente sin documentar.
- El foco debe ser visible, accesible y compatible con movimiento reducido. Documentar el comportamiento en README.md y en este archivo con cada cambio posterior.


## Ajustes visuales y de confianza documental (10 de noviembre de 2026)
- Topología debe mostrar dos tarjetas por fila en escritorio y una en pantallas estrechas, con espacio suficiente para apreciar el frontal de puertos.
- El mapa general y el detalle del switch cuentan igual conexiones completas y conexiones documentadas parcialmente para el uso del puerto. No mostrar ocupado como desconocido por omitir relaciones parciales en el detalle.
- El usuario confirma como documentalmente válidos los registros extraídos de los Visios. Mostrar esa confianza de fuente en estado “Confirmado”; esto no afirma conectividad en vivo ni autoriza completar extremos, IP, puerto remoto o medio ausentes.
- Si el tipo de puerto no aparece especificado, rotular “Tipo de puerto no indicado” en lugar de mostrar “NO CONFIRMADO” como si describiera el switch o sus conexiones.
- El detalle de switch se prepara para imprimir en una hoja A4 horizontal: conexiones en dos columnas, frontal y datos técnicos compactos, sin panel vacío de enlaces completos. Mantener visibles nombres, IP, puertos y estados; no ocultar ambigüedades del extremo remoto que sí tengan efecto.


## Revisión tabla por tabla del Visio V3 y capas de conexión (10 de noviembre de 2026)

La clasificación vigente que prevalece sobre reglas anteriores de “relación parcial pendiente” es:
- Capa 1 = patcheras y recorrido físico.
- Capa 2 = una relación switch a switch. Su existencia está confirmada si aparece en una tabla. Emparejar puertos locales y remotos solo si la tabla opuesta conduce a un único par recíproco; si no, conservar la relación confirmada y dejar el número del puerto remoto vacío.
- Capa 3 = PC, impresora, servidor, iDRAC u otro equipo final conectado al puerto de un switch. Mostrar el puerto local del switch. No mostrar ni solicitar un puerto remoto de NIC del equipo final.
- Los datos del V3 están confirmados por el usuario. Diferenciar falta de datos sobre un extremo o un número de puerto de incertidumbre sobre la conexión en sí.
- Respetar las correcciones del usuario: ProCurve 10.0.1.1; 3Com 192.168.1.24; Aruba 1930 principal de Mesa de Entradas 192.168.1.1 e inaccesible por falta de credenciales; R770 P1/iDRAC .247 y P2/LAN .246. “Principal/no principal” expresa la función de interconexión del switch y no su modelo.
- AGRIMENSURA15 es una PC/equipo de usuario conectado al switch de Agrimensura: representarlo como endpoint de Capa 3, sin puerto remoto inventado.
- Comparar los libros Excel incrustados por nombre de equipo, IP y etiqueta del puerto antes de emparejar. Normalizar diferencias menores del nombre, pero no resolver conflictos de IP o candidatos múltiples por intuición. Documentar libro/fila fuente y explicar cada emparejamiento recíproco en `docs/ANALISIS_V3_CONEXIONES.md`.
- Las tablas SFP mantienen sus etiquetas separadas de RJ45 (por ejemplo `SFP 28`) cuando comparten números. No deducir instalación de módulo, velocidad o independencia de jaulas combo si el documento no lo dice.
- Actualizar README y este prompt con todo cambio de modelo/capa. Mantener variables, comentarios y commits en español conforme a las reglas del proyecto.

- Si una tabla de switch documenta una PC/equipo final sin ficha de inventario, la topología puede crear un nodo visual temporal con nombre/IP documentados para dibujar la conexión de Capa 3. No persiste ni inventa una ficha de SQLite. Si existe equipo inventariado que coincide, reutilizarlo.

- Al añadir filas a un JSON incrustado en C#, validar el bloque JSON completo antes de actualizar el repositorio; una fila mal separada impide el arranque del inicializador.

## Impresión y UX de topología
- El detalle de un switch debe imprimirse completo en una página A4 horizontal: frontal, información del equipo y relaciones. Usar el ancho útil de la hoja y el CSS de impresión para compactar/refluir los bloques. No calcular una escala de toda la página a partir de la altura de pantalla; el detalle debe ocupar el ancho A4, no quedar como miniatura centrada. Restaurar tras imprimir cualquier estilo temporal.
- El mapa general imprime un resumen gráfico compacto en una hoja. No incluir fichas largas de enlaces en esa hoja panorámica; conservarlas en la vista interactiva y en el detalle del switch.
- No marcar conexiones documentadas como pendientes cuando solo falte el puerto remoto. Mantener textos, métricas y leyendas coherentes con la capa y la fuente.
- Actualizar README y este prompt al cambiar impresión o interacción.

- (Reemplazado el 10 de noviembre de 2026) El mapa de topología es un lienzo libre dentro de una ventana recortada; se desplaza con el fondo y se acerca con zoom en lugar de usar barras de desplazamiento. Permitir maximizar el panel completo para navegarlo en cualquiera de las capas y conservar sus controles.
- Las líneas se dibujan en coordenadas del lienzo (mismo sistema que las tarjetas) y el lienzo se traslada y escala como un bloque, por lo que maximizar o hacer zoom no obliga a recalcular las coordenadas de los extremos.
- Para la impresión A4 horizontal, distribuir tarjetas de conexión en más columnas según su cantidad y mantener el contenido al ancho imprimible. No encoger la página entera con el alto de la vista de pantalla; conservar el margen A4 y evitar miniaturas centradas.
- Actualizar README y este prompt junto con las mejoras de mapa o impresión.

- (Reemplazado el 10 de noviembre de 2026) Las tarjetas ya no se limitan a dos columnas: cada tarjeta es independiente, se puede arrastrar libremente y sus líneas la siguen. La disposición automática inicial agrupa en clústeres (equipo central a la izquierda; cada switch con sus equipos a su derecha).
- Dejar espacio visible entre tarjetas para que las líneas no se confundan con el contenido. Las conexiones deben llevar color de alto contraste y una clave visual por capa.
- Permitir mover el fondo y acercar con Ctrl + rueda o con una barra deslizante, restablecer todas las posiciones a la disposición genérica, encajar solo la vista actual y maximizar el panel sin perder el contexto de capa.
- El SVG es de 1 px con overflow visible dentro del lienzo transformado; no dimensionarlo al contenido ni sumar scroll a las coordenadas.
- Actualizar README y este prompt junto con cambios del mapa.

- El mapa debe estar contenido en una caja propia con sus controles de centrar y maximizar dentro del marco.
- Maximizar significa expandir el marco dentro de la página con un fondo atenuado, botón para restaurar y tecla Escape; no usar la API de pantalla completa del navegador.
- Evitar que la topología parezca una tabla: dejar espacio entre tarjetas para que las conexiones documentadas se vean por capa. Las tarjetas se posicionan de forma absoluta en el lienzo.
- Mantener el lienzo SVG, las tarjetas y sus líneas dentro del mismo lienzo transformado para conservar su alineación.

## Mapa de tarjetas libres (10 de noviembre de 2026)

- Cada equipo es una tarjeta con posición absoluta (`transform: translate`) sobre `[data-lienzo-topologia]`; el visor `[data-mapa-topologia]` recorta (`overflow: hidden`) y el script aplica traslación y zoom al lienzo.
- Arrastre por Pointer Events: con ratón desde cualquier punto libre de la tarjeta; en táctil solo desde la cabecera. Los puertos, enlaces y botones internos conservan su acción. Un clic sin movimiento selecciona la tarjeta y resalta sus conexiones; las flechas del teclado mueven la tarjeta enfocada (Mayús: paso grande).
- Las líneas salen del lado que mira a la otra tarjeta, a la altura del puerto documentado, con una pastilla de número de puerto. Si un extremo no tiene puerto documentado, la línea sale del centro del lado. No inferir conexiones nuevas.
- Color por capa: Capa 1 morado, Capa 2 azul, Capa 3 verde; ámbar discontinuo para pendientes y azul discontinuo para uplinks sin contraparte única.
- Las posiciones movidas por la persona se guardan por capa en `localStorage` (`topologia-tarjetas-jerarquica-v2`); el resto usa la disposición automática. Cambiar la versión de la clave descarta coordenadas anteriores que no corresponden al esquema inicial jerárquico. **Reorganizar** descarta las posiciones de la capa actual y reparte de nuevo con el modo elegido.
- La casilla «Solo equipos con conexión» (activa por defecto) oculta los equipos sin ninguna conexión en la capa elegida; desmarcarla muestra todo el inventario.
- La impresión del mapa general oculta primero el resto de la página y encaja las tarjetas y enlaces en un área de 287 × 180 mm dentro de A4 horizontal con margen de 5 mm. No escalar el contenedor completo según secciones que la impresión oculta. El encaje puede ampliar por encima del 100 % (hasta 400 %) para aprovechar el papel y debe restaurar la posición y el zoom al cerrar la vista previa.
- Actualizar README y este prompt junto con cambios del mapa. No usar emojis ni iconos en los textos de la interfaz.

### Distribuir y colapsar (10 de noviembre de 2026)

- Modos de distribución (`data-modo-distribucion`): **filas jerárquicas** por defecto (padres a la izquierda, niveles hijos a la derecha, sin optimizar por tamaño de ventana), **mosaico** y **cuadrícula** por área. Cambiar de modo o pulsar Reorganizar reubica las tarjetas visibles de la capa.
- Cada tarjeta tiene botones de texto: **Contraer/Expandir** (solo cabecera) y **Ocultar N/Mostrar N** (oculta o muestra los N equipos que cuelgan de ella en el árbol de recorrido). Los equipos ocultos por grupo no se cuentan como «sin conexión»: la conectividad se evalúa antes de colapsar.
- Las herramientas globales de contraer/expandir tarjetas y ocultar/mostrar grupos se agrupan bajo «Más opciones»; las acciones individuales de cada tarjeta se guardan bajo su resumen «Acciones». Los estados de contraído/colapsado valen solo durante la visita actual y no se guardan.
- Cualquier función nueva del mapa debe usar botones de texto, sin emojis ni iconos, y mantener las líneas ancladas al puerto o al centro del lado cuando el puerto queda oculto.


## Restablecimiento y disposición genérica del mapa

- En una primera carga, disponer las tarjetas de forma jerárquica y determinista por niveles; la posición no debe depender de la proporción de la ventana. La persona puede mover cada tarjeta; las posiciones manuales se cachean por capa en `localStorage`.
- **Restablecer mapa** borra las posiciones guardadas de todas las capas, limpia búsqueda, filtros y grupos colapsados, y reconstruye la disposición jerárquica inicial. No limitarse a restablecer el zoom. **Encajar vista** solo cambia el encuadre y conserva posiciones.
- Reducir herramientas visibles: controles primarios para zoom, restablecer, encajar y maximizar; filtros y herramientas de distribución/colapso bajo «Más opciones». Usar una barra deslizante de zoom en lugar de botones separados de acercar y alejar.
- Switches con 48 o más interfaces, incluido Dell S4148U, usan una tarjeta de 700 px para mostrar el frontal con legibilidad; el tamaño se mide antes de calcular la distribución y los anclajes de línea. Aumentar la separación de niveles para dejar visibles las líneas.
- Mostrar una sección de pendientes que distinga relaciones confirmadas de detalle faltante: puertos remotos Capa 2 sin cotejo único, medios no especificados, módulos SFP/SFP+ por confirmar, tramos de rack que requieren cotejo actual y switches sin credenciales (Régimen y Mesa de Entrada Principal). Un extremo de Capa 3 no requiere puerto de PC.
- A petición del usuario, ejecutar una sola vez por base SQLite el reinicio inicial de `EventosModificacion`; guardar una marca en `MantenimientosEjecutados` dentro de la misma transacción. No borrar `EventosAcceso` ni volver a borrar el historial en cada arranque. Si el historial queda vacío, mostrar un mensaje indicando que las próximas modificaciones aparecerán allí.
- Actualizar README y este prompt con cambios de topología, auditoría e interfaz.
