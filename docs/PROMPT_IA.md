# Contexto permanente para la IA — ProyectoConexionRed

## Objetivo
Crear una aplicación web en español para inventariar, consultar y visualizar la red empresarial documentada por el usuario. Debe enlazar equipos, IP, interfaces, conexiones, ubicación y usuarios/PCs con una topología navegable, auditoría de cambios, simulaciones aisladas y exportación imprimible.

## Estado actual del proyecto
- Repositorio público por decisión explícita del usuario.
- Hay un MVP inicial en C# / ASP.NET Core MVC con EF Core y SQLite.
- Incluye página de inicio, inventario con búsqueda por nombre/IP/área/modelo/fabricante, CSV de resultados filtrados, impresión de la tabla y 25 registros semilla.
- Requiere autenticación por cookie para ver las páginas. El administrador inicial se crea solo cuando la base no tiene usuarios y se configuran INVENTARIO_ADMIN_USUARIO e INVENTARIO_ADMIN_CLAVE fuera del repositorio.
- Las claves se almacenan como hash. La tabla de accesos registra ingresos correctos/fallidos y cierres con hora UTC; no guarda contraseñas.
- La tabla de accesos tiene una página para consultar los últimos cien eventos.
- Las tablas de usuarios e historial se crean con SQL IF NOT EXISTS para compatibilidad con la base prototipo creada en la primera versión. El resto del modelo usa EnsureCreated; emplear migraciones en futuras evoluciones del esquema.
- La gestión básica de cuentas está incorporada: rol Administrador/Tecnico, alta de técnicos (máximo cinco cuentas totales) y activación/desactivación. Las cuentas previas reciben rol Administrador por compatibilidad al agregar la nueva columna SQLite; el alta por la interfaz siempre asigna Tecnico. Se protege la última cuenta administradora activa y no se permite auto desactivación.\n- No están implementados edición de inventario, auditoría de modificaciones, topología gráfica, enlaces, SFP, simulaciones, exportación gráfica ni imágenes. Las cookies de sesiones ya abiertas no se revocan inmediatamente al desactivar una cuenta. No desplegar en producción antes de validar la gestión de sesiones, controles de acceso, HTTPS y pruebas.
- El usuario compartirá posteriormente otros dos Visios con nombres de PC, usuarioPC y nombres de usuario. No inventar esos datos antes de revisar las fuentes.
- Al modificar el código, actualizar siempre README.md y docs/PROMPT_IA.md.
- No se ha confirmado con el usuario que el programa compile ni se han verificado las páginas en ejecución; no afirmar que están verificadas. La administración de cuentas y el manejo de bases anteriores requieren esa verificación pendiente.

## Fuentes y calidad de datos
- Fuente base: DIAGRAMA RED V2.vsdx.
- Confirmación del usuario: todos los dispositivos y las IP asignadas en el Visio son reales y están actualizados.
- Los dispositivos de usuario y sus conexiones de acceso también están actualizados según el usuario.
- Solo algunas conexiones del rack están desactualizadas; señalarlas para revisión. No generalizar ese estado a equipos, IP ni conexiones de usuario.
- Algunos SFP no están confirmados. Registrar como pendientes de actualización/sin confirmar; la fecha de última revisión debe ser real y conocida. Nunca inventar fecha, modelo, tipo o medio.
- Dos switches son inaccesibles administrativamente por falta de credenciales: área Régimen y switch principal de Mesa de Entradas. Esa condición no significa que estén apagados ni que haya una falla de red.
- «Principal» y «No principal» expresan jerarquía de switches dentro de un área. El switch «No principal» de Mesa de Entradas enlaza al principal del área y conecta tres equipos de usuario a través de él.
- Separar estado de acceso administrativo, vigencia de datos y disponibilidad física.
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

## Arquitectura y alcance acordados
- Backend: C# con ASP.NET Core MVC y Entity Framework Core.
- Base inicial: SQLite; se espera un técnico y hasta cinco técnicos con lectura/escritura. Revisar PostgreSQL o SQL Server si aumentan concurrencia o instancias de aplicación.
- El servidor web será la vía de acceso a SQLite; no compartir el archivo directamente por una unidad de red.
- Las vistas física, switching y red serán perspectivas del mismo inventario.
- La impresión disponible en el MVP corresponde solo al inventario tabular; no presentarla como exportación de topología.
- La cookie de autenticación es HTTP Only; en producción se requiere HTTPS. No hay alta pública de usuarios. La pantalla Usuarios solo es visible y accesible para Administrador; las altas nuevas son Tecnico y el total de cuentas queda limitado a cinco.
- Secciones futuras: inventario, topología, puertos/conexiones, switching/VLAN, IP/subredes, PCs/usuarios, auditoría, simulaciones, informes/impresión y administración.
- Simulaciones solo sobre el modelo de datos de la aplicación. No mandar pings, comandos ni cambios a la red real.
- Las imágenes deben corresponder a un modelo identificado y registrar su fuente; no presentarlas como verificadas por similitud de nombre.

## Orden de trabajo
1. Gestionar cuentas y permisos para el máximo previsto de cinco técnicos.
2. Agregar fichas editables y auditoría de modificaciones.
3. Modelar ubicaciones, puertos, enlaces y jerarquía de switches.
4. Crear topología y vistas por capa.
5. Revisar los dos Visios pendientes para enriquecer PCs y usuarios.
6. Agregar simulaciones aisladas, imágenes y exportación/impresión gráfica.
7. Completar despliegue HTTPS, respaldos y migraciones.

## Auditoría requerida
- El MVP registra accesos correctos/fallidos y cierres de sesión con usuario, evento, resultado y fecha UTC.
- Próxima extensión de auditoría: usuario, fecha/hora, entidad, campo, valor anterior y nuevo para modificaciones.
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
