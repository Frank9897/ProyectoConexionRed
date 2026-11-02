# ProyectoConexionRed

Aplicación web en español para documentar y visualizar la infraestructura de red empresarial. El repositorio es público por decisión del usuario.

## Propósito

Reunir en un inventario navegable los dispositivos, IP, puertos, enlaces, áreas, PCs y usuarios de la red; mostrar su topología por vistas física, switching y red; registrar cambios; y permitir simulaciones locales y exportación imprimible.

## Estado del desarrollo

**Primera versión funcional: inventario de solo lectura.** La aplicación usa ASP.NET Core MVC, Entity Framework Core y SQLite. Incluye una página de inicio, una tabla de inventario con búsqueda por nombre, IP, área, modelo o fabricante y 25 registros documentales iniciales cargados cuando la base está vacía.

La base SQLite se crea al iniciar la aplicación. Esta versión usa creación inicial de esquema para el prototipo; las evoluciones del modelo deberán incorporar migraciones.

El archivo **DIAGRAMA RED V2.vsdx** es la fuente base. El usuario confirmó que los dispositivos, IP y datos de los equipos de usuario están vigentes. Algunas conexiones del rack necesitan revisión y algunos SFP están pendientes de actualizar. Se esperan otros dos Visios para incorporar nombres de PC, usuarioPC y nombres de usuario.

Los switches de las áreas Régimen y Mesa de Entradas Principal son inaccesibles administrativamente por falta de credenciales. La aplicación muestra ese estado separado de la vigencia de datos. El switch etiquetado «No principal» de Mesa de Entradas queda identificado como secundario del principal del área y conectado a tres equipos de usuario según la confirmación del usuario.

El inventario detallado y las reglas de continuidad están en [docs/PROMPT_IA.md](docs/PROMPT_IA.md).

## Ejecutar localmente

Requiere el SDK de .NET 10.

```powershell
dotnet run --project ProyectoConexionRed.Web.csproj
```

Al iniciar, se crea `proyecto-conexion-red.db` en el directorio de ejecución y se agregan los registros iniciales si todavía no existen datos.

## Arquitectura

- C# y ASP.NET Core MVC.
- Entity Framework Core con proveedor SQLite.
- El servidor web es la vía de acceso a SQLite; no compartir directamente el archivo de base por una unidad de red.
- Secciones previstas: inventario, topología, puertos y conexiones, switching/VLAN, redes/IP, PCs/usuarios, auditoría, simulaciones, informes e impresión y administración.
- Se espera un técnico actualmente y hasta cinco técnicos con lectura/escritura. Se revisará una base cliente-servidor si aumenta la concurrencia o cambia el despliegue.

## Próximas etapas

1. Revisar y modelar ubicaciones, puertos, conexiones del rack y SFP, con estado de validación.
2. Incorporar edición controlada del inventario.
3. Añadir usuarios, permisos e historial de logueos y modificaciones.
4. Construir topología y vistas por capa.
5. Incorporar los dos Visios pendientes para PCs y usuarios.
6. Agregar simulaciones aisladas, imágenes de modelos y exportación/impresión.
7. Completar despliegue y respaldos.

## Principios de datos y seguridad

- Diferenciar vigencia de datos, estado de acceso administrativo y conectividad física.
- No inventar conexiones, puertos, modelos de SFP ni fechas de revisión.
- Las simulaciones no deben ejecutar acciones contra la red de producción.
- No guardar contraseñas, claves ni tokens en el inventario o en el historial.

## Reglas para contribuir

- Variables en español; comentar cada variable explicando qué representa y el efecto de cambiar su valor.
- Funciones documentadas en español, incluyendo qué hacen sus parámetros y cómo afectan el resultado.
- Actualizar este README y [docs/PROMPT_IA.md](docs/PROMPT_IA.md) en cada modificación.
- Usar mensajes de commit breves y en español.
- No incluir secretos en código, documentación, ejemplos, capturas o commits.
