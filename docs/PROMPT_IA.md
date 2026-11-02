# Contexto permanente para la IA — ProyectoConexionRed

## Objetivo
Crear una aplicación web en español para inventariar, consultar y visualizar la red empresarial documentada por el usuario. Debe enlazar equipos, IP, interfaces, conexiones, ubicación y usuarios/PCs con una topología navegable, auditoría de cambios, simulaciones aisladas y exportación imprimible.

## Estado actual del proyecto
- Repositorio público por decisión explícita del usuario.
- Hay un primer MVP de solo lectura en C# / ASP.NET Core MVC con EF Core y SQLite.
- El MVP tiene página de inicio, vista de inventario con búsqueda por nombre/IP/área/modelo/fabricante, descarga CSV de resultados filtrados y formato de impresión de la tabla; carga 25 registros iniciales.
- La base de datos se crea al iniciar con EnsureCreated; esto es un prototipo. Usar migraciones antes de cambios de esquema posteriores.
- No están implementados todavía el acceso con usuarios, permisos, edición de inventario, auditoría, topología gráfica, enlaces, SFP, simulaciones, exportación gráfica ni imágenes. No desplegar el MVP como servicio de producción hasta incorporar autenticación y autorización.
- El usuario compartirá posteriormente otros dos Visios con nombres de PC, usuarioPC y nombres de usuario. No inventar esos datos antes de revisar las fuentes.
- Al modificar el código, actualizar siempre README.md y docs/PROMPT_IA.md.
- No se ha confirmado con el usuario que el programa compile ni se han verificado las páginas en ejecución; no afirmar que están verificadas.

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
- Secciones futuras: inventario, topología, puertos/conexiones, switching/VLAN, IP/subredes, PCs/usuarios, auditoría, simulaciones, informes/impresión y administración.
- Simulaciones solo sobre el modelo de datos de la aplicación. No mandar pings, comandos ni cambios a la red real.
- Las imágenes deben corresponder a un modelo identificado y registrar su fuente; no presentarlas como verificadas por similitud de nombre.

## Orden de trabajo
1. Consolidar fuentes y esquema de inventario.
2. Agregar fichas editables y validaciones.
3. Modelar ubicaciones, puertos, enlaces y jerarquía de switches.
4. Crear topología y vistas por capa.
5. Incorporar usuarios, permisos e historial de logueos/cambios.
6. Revisar los dos Visios pendientes para enriquecer PCs y usuarios.
7. Agregar simulaciones aisladas, imágenes y exportación/impresión.
8. Completar despliegue, respaldos y migraciones.

## Auditoría requerida
- Accesos: usuario, fecha/hora, resultado y evento de autenticación.
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
