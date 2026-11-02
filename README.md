# ProyectoConexionRed

Aplicación web en español para documentar y visualizar la infraestructura de red empresarial. El repositorio es público por decisión del usuario.

## Propósito

Reunir en un inventario navegable los dispositivos, IP, puertos, enlaces, áreas, PCs y usuarios de la red; mostrar su topología por vistas física, switching y red; registrar cambios; y permitir simulaciones locales y exportación imprimible.

## Estado del desarrollo

**Etapa actual: análisis y documentación inicial.** Aún no hay funcionalidades web implementadas ni una base de datos del proyecto.

El archivo **DIAGRAMA RED V2.vsdx** sirve como base. El usuario confirmó que los dispositivos, IP y datos de los equipos de usuario están vigentes. Algunas conexiones del rack necesitan revisión y algunos SFP están pendientes de actualizar. Se esperan otros dos Visios para incorporar nombres de PC, usuarioPC y nombres de usuario.

Los switches de las áreas Régimen y Mesa de Entradas Principal son inaccesibles administrativamente por falta de credenciales. La aplicación deberá mostrar esa condición sin interpretarla como una falla de red. En ciertas áreas, el switch etiquetado «No principal» se conecta al principal del área y brinda conexión a tres equipos de usuario.

El inventario detallado extraído del Visio y las reglas de continuidad para desarrollo están en [docs/PROMPT_IA.md](docs/PROMPT_IA.md).

## Arquitectura prevista

- C# y ASP.NET Core MVC.
- Entity Framework Core.
- SQLite como base inicial, adecuada para el equipo actual y hasta cinco técnicos con lectura/escritura prevista.
- El servidor web será el único acceso al archivo SQLite. Se revisará una base cliente-servidor si crece la concurrencia o el despliegue lo requiere.
- Secciones previstas: inventario, topología, puertos y conexiones, switching/VLAN, redes/IP, PCs/usuarios, auditoría, simulaciones, informes e impresión, y administración.

## Principios de datos y seguridad

- Diferenciar datos vigentes, pendientes de revisión y desactualizados.
- No inventar conexiones, puertos, modelos de SFP ni fechas de revisión.
- Mantener separado el estado administrativo (por ejemplo, falta de credenciales) del estado físico o de conectividad.
- Las simulaciones se ejecutarán sobre los datos de la aplicación y no actuarán sobre la red de producción.
- Mantener historial de accesos y cambios con usuario, fecha/hora y valores anterior/nuevo.
- No guardar ni publicar contraseñas, claves o tokens, aunque el repositorio sea público.

## Orden de trabajo previsto

1. Revisar y normalizar las fuentes Visio.
2. Definir el modelo de datos.
3. Construir el inventario y fichas de dispositivos.
4. Incorporar ubicaciones, puertos, conexiones y jerarquía de switches.
5. Crear topología y vistas por capa.
6. Añadir auditoría y control de acceso.
7. Incorporar los dos Visios pendientes para PCs y usuarios.
8. Añadir simulaciones aisladas y exportación/impresión.
9. Completar administración y despliegue.

## Reglas para contribuir

- Variables en español; comentar cada variable explicando qué representa y el efecto de cambiar su valor.
- Funciones documentadas en español, incluyendo qué hacen sus parámetros y cómo afectan el resultado.
- Actualizar este README y [docs/PROMPT_IA.md](docs/PROMPT_IA.md) en cada modificación.
- Mensajes de commit breves y en español.
- No incluir secretos en código, documentación, ejemplos, capturas o commits.

