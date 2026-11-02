# Contexto permanente para la IA — ProyectoConexionRed

## Objetivo
Crear una aplicación web en español para inventariar, consultar y visualizar la red empresarial documentada por el usuario. La aplicación debe enlazar equipos, IP, interfaces, conexiones, ubicación y usuarios/PCs con una topología navegable, auditoría de cambios, simulaciones aisladas y exportación imprimible.

## Estado actual del proyecto
- Repositorio: público por decisión explícita del usuario.
- Etapa: análisis y documentación inicial; no hay funcionalidades de la aplicación implementadas todavía.
- README y este archivo son la referencia principal para mantener continuidad entre tareas.
- El usuario compartirá posteriormente otros dos archivos Visio con nombres de PC, usuarioPC y nombres de usuario. No inventar ni cargar esos datos antes de recibir y revisar esos archivos.
- Al hacer cambios al proyecto, actualizar siempre README.md y docs/PROMPT_IA.md.

## Fuentes y calidad de los datos
- El archivo base es DIAGRAMA RED V2.vsdx, con fecha de modificación indicada por el sistema de archivos: 23/10/2026.
- Confirmación del usuario: todos los dispositivos y las IP asignadas en el Visio son reales y están actualizados.
- También están actualizados los dispositivos de usuario y sus conexiones de acceso según el usuario.
- Solo algunas conexiones del rack están desactualizadas; marcarlas para revisión. No generalizar esa condición a los equipos, IP ni conexiones de usuarios.
- Algunos SFP no están confirmados. Representar modelo/medio como «pendiente de actualización» o «sin confirmar». Guardar la fecha de última revisión solo cuando haya una revisión real documentada; nunca inventar fecha, modelo o tipo.
- Dos switches son inaccesibles administrativamente por falta de credenciales: área Régimen y switch principal de Mesa de Entradas. Esa condición no significa que estén apagados ni que tengan una falla de red.
- Las etiquetas «principal» y «no principal» expresan jerarquía dentro de un área. Un switch «No principal» enlaza con el switch principal del área y conecta tres equipos de usuario a través de ese switch. Mostrar la relación padre/descendiente de manera explícita.
- Distinguir el estado de acceso administrativo, la actualización de datos y la disponibilidad física: son conceptos independientes.
- El usuario autoriza expresamente la publicación de la documentación de red en este repositorio público. Aun así, no publicar contraseñas, claves, tokens ni otros secretos de autenticación.

## Inventario extraído del Visio base
Las IP de esta lista son datos vigentes según el usuario. Los nombres se transcriben del diagrama y las conexiones de rack asociadas quedan pendientes de revisión cuando corresponda.

### Equipos de rack y núcleo
| Etiqueta en el Visio | IP visible |
|---|---|
| Switch Aruba 1930 (WDGC) | 192.168.1.19 |
| HP ProCurve 29100I-24G | 10.0.1.1 |
| Switch HP 5500 | 192.168.1.218 |
| Switch HP 1920 | 192.168.1.115 |
| Dell S4148U-ON 48G | 192.168.1.44 |
| Switch HP 1920S | 192.168.1.217 |
| Aruba 1930 (reserva) | 192.168.1.200 |

En la vista general también aparecen Cisco Catalyst 3560, módem Claro y patcheras. No asignarles IP ni enlaces salvo que el Visio u otra confirmación lo indique.

### Switches de área / otras fichas
| Etiqueta en el Visio | IP visible |
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
| Switches de áreas sin identificar | 192.168.1.1 y 192.168.1.17 |

Hay direcciones repetidas en etiquetas distintas del Visio, por ejemplo 192.168.1.19. Preservar lo escrito y marcar esa coincidencia para validar; no corregir ni deducir a qué equipo corresponde.

## Preferencias y alcance funcional
- Backend propuesto: C# con ASP.NET Core MVC y Entity Framework Core.
- Base de datos inicial: SQLite. Se espera un técnico actualmente y hasta cinco técnicos con lectura/escritura. Mantener la opción de migrar a PostgreSQL o SQL Server si la concurrencia o el despliegue lo requieren.
- El servidor web debe ser la vía de acceso a SQLite; no compartir el archivo de base directamente por una unidad de red.
- Vistas principales: inventario; topología; conexiones/puertos; switching/VLAN; IP/subredes; PCs/usuarios; auditoría; simulaciones; informes e impresión; administración.
- Las vistas física, switching y red son perspectivas del mismo inventario, no bases duplicadas.
- Las simulaciones usan una copia lógica de los datos dentro de la aplicación. No mandar pings, comandos ni cambios de configuración a la red real.
- Topología gráfica imprimible/exportable con filtros y leyenda.
- Las imágenes de equipos deben corresponder a un modelo identificado y permitir registrar fuente; no elegirlas como verificadas solo por similitud de nombre.

## Orden recomendado de trabajo
1. Revisar y normalizar fuentes Visio, registrando procedencia y estado de cada dato.
2. Definir entidades y relaciones: dispositivo, modelo, puerto, enlace, ubicación/rack, área, VLAN, subred, PC, usuario, revisión, evento de auditoría e imagen.
3. Implementar inventario y fichas de dispositivo.
4. Implementar ubicaciones, puertos y enlaces, con estado de validación y jerarquía de switches por área.
5. Implementar topología y vistas por capa.
6. Implementar auditoría de inicio de sesión y modificaciones.
7. Incorporar los dos Visios pendientes para enriquecer PCs, usuarioPC y nombres de usuario.
8. Implementar simulaciones aisladas y exportación/impresión.
9. Completar administración y despliegue.

## Auditoría
- Historial de acceso: usuario, fecha/hora, resultado de autenticación y evento.
- Historial de cambios: quién, cuándo, entidad, campo, valor anterior y valor nuevo.
- Ejemplo legible: «Usuario modificó IP de Equipo: 192.168.0.120 → 192.168.0.127».
- No guardar contraseñas en claro ni incluirlas en registros, capturas, ejemplos o documentación.

## Reglas obligatorias de desarrollo
1. Nombrar las variables en español.
2. Comentar cada variable en español, explicando qué representa y qué efecto tendría cambiar su valor.
3. Documentar cada función en español: qué hace y qué cambios producen los valores de sus parámetros.
4. Mantener comentarios y documentación en español; conservar nombres propios de APIs y estándares cuando corresponda.
5. Actualizar siempre README.md y este archivo docs/PROMPT_IA.md al modificar el proyecto.
6. Usar mensajes de commit breves y en español.
7. No inventar hechos del inventario ni declarar implementado lo que no exista en el repositorio.
8. Tratar contraseñas, claves y tokens como secretos, aunque el repositorio sea público por decisión del usuario.
9. No probar ni modificar la red de producción. Las pruebas de topología son simulaciones locales.
