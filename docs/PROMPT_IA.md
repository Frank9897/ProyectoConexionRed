# Contexto permanente para la IA — ProyectoConexionRed

## Objetivo
Crear una aplicación web en español para inventariar, consultar y visualizar la red empresarial documentada por el usuario. Debe enlazar equipos, IP, interfaces, conexiones, ubicación y usuarios/PCs con una topología navegable, auditoría de cambios, simulaciones aisladas y exportación imprimible.

## Estado actual del proyecto
- Repositorio público por decisión explícita del usuario.
- Hay un MVP inicial en C# / ASP.NET Core MVC con EF Core y SQLite.
- Incluye página de inicio, inventario con búsqueda por nombre/IP/área/piso/tipo/modelo/fabricante, CSV de resultados filtrados, impresión de la tabla y 25 registros semilla.
- El inventario permite alta/edición de datos técnicos y ubicación. Por ahora no debe capturar ni editar usuario asignado, fuente documental ni acceso administrativo; los valores previos de esos campos se preservan al editar. Tipo de equipo, rol y vigencia se seleccionan de listas. La tabla muestra el tipo de dispositivo y ofrece 10, 30 o todos los registros, con botones de página anterior/siguiente; al imprimir se muestran todos los resultados filtrados. Las fichas nuevas y sus valores iniciales quedan auditados. Las propiedades nuevas se agregan con una actualización de esquema compatible con bases SQLite anteriores. Todavía no se cargaron masivamente los puestos del PDF.
- MODO TEMPORAL SIN LOGIN: las páginas principales se abren sin autenticación para pruebas. En Program.cs se comenta el filtro MVC global; las acciones GET/POST de ingreso y salida quedan marcadas [NonAction]; _Layout muestra navegación pública sin cuenta ni enlace a Usuarios. UsuariosController conserva el requisito de rol Administrador, por lo que la gestión de cuentas no está disponible durante este modo abierto. Para restaurar el login al final del proyecto, descomentar el filtro global, quitar [NonAction] de CuentaController y reponer la navegación autenticada.
- Las claves se almacenan como hash. La tabla de accesos registra ingresos correctos/fallidos y cierres con hora UTC; no guarda contraseñas.
- La tabla de accesos tiene una página para consultar los últimos cien eventos.
- Las tablas de usuarios e historial se crean con SQL IF NOT EXISTS para compatibilidad con la base prototipo creada en la primera versión. El resto del modelo usa EnsureCreated; emplear migraciones en futuras evoluciones del esquema.
- La gestión básica de cuentas está incorporada: rol Administrador/Tecnico, alta de técnicos (máximo cinco cuentas totales) y activación/desactivación. Las cuentas previas reciben rol Administrador por compatibilidad al agregar la nueva columna SQLite; el alta por la interfaz siempre asigna Tecnico. Se protege la última cuenta administradora activa y no se permite auto desactivación.
- La edición controlada de equipos y la auditoría por campo están implementadas en el código: se guardan cuenta, fecha UTC, equipo, campo, valor anterior y valor nuevo. La pantalla Cambios muestra los últimos doscientos eventos. La edición solo modifica la base documental y no actúa sobre la red real.
- Hay un esquema SQLite para interfaces físicas y enlaces entre puertos. La sección «Puertos» permite registrar interfaces y enlaces entre dos puertos existentes, elegir tipo/medio, estado documental, fuente, módulos SFP opcionales, fecha real y observaciones. Las dos colecciones se muestran en fichas compactas en lugar de filas tabulares densas; comienzan con 10 elementos y permiten mostrar 10, 20, 30 o todos, con flechas y una transición sutil de 450 ms. Permite corregir interfaces y enlaces con validación de equipo/puerto, evita duplicar puertos o reutilizar extremos enlazados, separa la falta de datos de la conectividad y audita cada campo cambiado en EventosModificacion. Los formularios solo cambian el inventario documental y no consultan ni modifican la red real. Topología presenta indicadores y una tabla alimentada únicamente por enlaces guardados. La topología gráfica interactiva, relaciones de switching/VLAN, simulaciones, ficha SFP, exportación gráfica e imágenes siguen pendientes. La cookie se valida contra SQLite en cada solicitud: una cuenta desactivada pierde acceso en la siguiente solicitud y un cambio de rol se renueva en la sesión. La marca aleatoria del proceso invalida las cookies anteriores tras un reinicio. No desplegar en producción antes de verificar la aplicación, controles de acceso, HTTPS y pruebas.
- El 4 de noviembre de 2026 se recibieron y revisaron `DIAGRAMA RED V3.0.vsdx` y `Diagrama UPS.vsdx`. El inicializador incorpora de forma idempotente las MAC explícitas del V3 cuando coinciden la etiqueta/IP y el campo MAC está vacío; completa Aruba como fabricante para los dos switches de área con IP `192.168.1.1` y `192.168.1.17` si el campo está vacío; y da de alta una sola vez el Aruba 1930 Reserva 2. Registra estos cambios en `EventosModificacion` bajo «Carga documental DIAGRAMA RED V3.0». No reemplaza IP, MAC ni fabricante preexistentes. El usuario confirmó que V3.0 fue verificado switch por switch e interfaz por interfaz: usarlo como fuente principal para enlaces, interfaces e IP que documente. No inventar conexiones de rack ausentes o ambiguas. Los datos de UPS, almacenamiento y servidores todavía no se importaron. Las tablas Excel incrustadas de V3 se analizaron para preparar el registro manual y la futura carga controlada. No cargar enlaces ambiguos como confirmados; la IP ProCurve vigente indicada por el usuario es `10.0.1.1`, pese a una celda discrepante en una hoja incrustada. Mesa de Entrada principal es Aruba 1930 `192.168.1.1` y su acceso administrativo es inaccesible. La tabla de Mesa de Entrada secundaria registra MENTRADA04, MENTRADA02 y el puerto 24 hacia el Aruba principal.
- Se recibió `UBICACION-EQUIPOS-OCTUBRE 2026.pdf` y se revisaron sus 14 páginas. Contiene puestos con etiquetas de equipo, asignaciones de usuario y ubicación por área/piso. El usuario aclaró que el número entre paréntesis es el último octeto de la IP de usuario dentro de `192.168.0.0/24`; por ejemplo, `(69)` representa `192.168.0.69`, no un número de inventario. El PDF es fuente de ubicación/usuario y puede estar menos actualizado que V3.0; si ambos contienen datos de red distintos, prevalece V3.0. Los puestos aún no se han importado. Guardar el nombre de usuario tal como aparece y anotar la fuente; no inventar asociaciones de switch derivadas de la posición gráfica.
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

En la página de switches de área, V3 identifica como Aruba los equipos con IP `192.168.1.1` y `192.168.1.17`, pero no especifica sus modelos. Completar solo el fabricante cuando esté vacío; dejar modelo sin identificar.

El V3 etiqueta el HP ProCurve 29100I-24G con IP `10.0.1.1`, acceso SSH y credenciales desconocidas. Mantener su acceso administrativo como pendiente/no confirmado hasta que el usuario aclare si actualmente es inaccesible. No confundirlo con los dos switches previamente confirmados como inaccesibles (Régimen y Mesa de Entradas Principal).

### Equipos de rack y energía observados

`DIAGRAMA RED V3.0.vsdx` y `Diagrama UPS.vsdx` incluyen etiquetas para módem Claro, módem Personal, equipos INTELLINET, patcheras y equipos Dell identificados como servidores, almacenamiento y switches de rack. Los modelos o etiquetas visibles incluyen Dell R740 (variantes rotuladas O y P), R770, R920, SCV320, SCV300, SCV3000, Dell DS 6610B y Dell DS 300B. Registrar solo campos expresamente legibles y conservar la fuente; IP, MAC, cantidades y enlaces no indicados quedan vacíos o pendientes. No interpretar identificadores de forma como cantidad de unidades hasta resolver si corresponden a vistas frontal/trasera o a elementos duplicados.

El diagrama UPS identifica UPS 1 a UPS 12 y marca UPS 10 como «NO OPERATIVA». También aparecen etiquetas de modelos o familias `SRV3KRI-AR`, `APC RC2000`, `APC RT 10000`, `SUA3000RMI2U`, `SRV1KRLAR`, `SMT3000RMRU`, `UPS EATON`, `UPS APC EASY` y `TRV`. El texto extraído no permite asociar con certeza todos esos modelos a un número de UPS; no inventar esa correspondencia. Hay varias zapatillas de rack con posiciones/etiquetas, pero sus tomas y cargas requieren revisión visual antes de cargar circuitos eléctricos.

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

## Orden de trabajo

1. Verificar edición, auditoría y compatibilidad con bases SQLite previas.
2. Completar edición y corrección de interfaces/enlaces; cargar enlaces de V3 solo si ambos extremos y puertos están identificados.
3. Incorporar relaciones de switching y revisar los dos Visios pendientes para PCs y usuarios.
4. Dibujar topología gráfica por vistas física, switching y acceso/IP.
5. Agregar simulaciones aisladas, imágenes y exportación/impresión gráfica.
6. Completar despliegue HTTPS, respaldos y migraciones.

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
