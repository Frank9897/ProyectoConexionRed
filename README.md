# ProyectoConexionRed

Aplicación web en español para documentar y visualizar la infraestructura de red empresarial. El repositorio es público por decisión del usuario.

## Propósito

Reunir en un inventario navegable los dispositivos, IP, puertos, enlaces, áreas, PCs y usuarios de la red; mostrar su topología por vistas física, switching y red; registrar cambios; y permitir simulaciones locales y exportación imprimible.

## Estado del desarrollo

**MVP de inventario, acceso e historial en desarrollo.** La aplicación usa ASP.NET Core MVC, Entity Framework Core y SQLite. Incluye inicio, inventario con búsqueda por nombre, IP, área, usuario, piso, modelo o fabricante, alta y edición de fichas, exportación CSV e impresión de la tabla. Carga 25 registros iniciales cuando la base está vacía. Las fichas admiten usuario asignado, piso y fuente documental, y sus cambios quedan auditados. El alta manual de puestos ya está preparada; la carga masiva de puestos del PDF todavía no se ha incorporado.

El login está deshabilitado temporalmente para las pruebas: los módulos principales abren sin sesión. El filtro global de autorización está comentado en Program.cs; las acciones de ingreso y cierre llevan [NonAction], y la navegación pública no muestra controles de cuenta. La administración de usuarios sigue protegida por rol y no está disponible en este modo. Al final del proyecto se restaurará el login descomentando el filtro y quitando [NonAction]. La cuenta administradora inicial solo se crea cuando la base todavía no contiene usuarios y se configuran las variables de entorno `INVENTARIO_ADMIN_USUARIO` y `INVENTARIO_ADMIN_CLAVE`. La clave se guarda como hash. La cuenta inicial recibe rol Administrador. El administrador puede crear cuentas Técnicas, hasta cinco cuentas en total, y activar o desactivar cuentas. Las cuentas existentes reciben rol Administrador al actualizar una base anterior, para no quitar el acceso que ya tenían. El administrador no puede desactivar su propia cuenta activa ni la última cuenta administradora activa. La pantalla «Accesos» muestra los últimos cien inicios de sesión y cierres de sesión, con hora UTC y resultado; no registra contraseñas.

La aplicación permite crear y editar fichas de equipos y puestos; registra cambios y datos iniciales en un historial por usuario, fecha UTC, campo y valores anterior/nuevo. La página Topología resume equipos, switches clasificados, puestos e interfaces y presenta las capas física, switching y acceso en tarjetas adaptables. Cuando no hay enlaces, muestra un estado vacío informativo; cuando los hay, presenta extremos, puertos, medio, estado, fuente y fecha de revisión. La tabla de enlaces se alimenta solo de relaciones guardadas. El esquema SQLite de interfaces/enlaces existe, pero sigue pendiente el formulario para editar conexiones y la carga de enlaces. La topología gráfica interactiva, la ficha SFP y la exportación gráfica siguen pendientes. Falta aplicar límites a intentos de acceso, verificar la aplicación y completar HTTPS antes de considerar un despliegue de producción.

Las fuentes de red más recientes recibidas el 4 de noviembre de 2026 son **DIAGRAMA RED V3.0.vsdx** y **Diagrama UPS.vsdx**. La inicialización incorpora MAC explícitas del V3 si coinciden la etiqueta y la IP y el campo está vacío; completa Aruba como fabricante en dos switches sin modelo e incorpora una vez Aruba 1930 Reserva 2. La importación deja historial y no reemplaza datos existentes. El usuario confirmó que V3.0 fue verificado switch por switch e interfaz por interfaz; sus relaciones verificadas tendrán prioridad para IP de red y puertos. Las conexiones de rack que allí no estén confirmadas, los SFP y algunas relaciones UPS siguen pendientes.

También se recibió **UBICACION-EQUIPOS-OCTUBRE 2026.pdf**, con planos de ubicación, puestos, usuarios y áreas. El usuario aclaró que el número entre paréntesis de cada puesto representa el último octeto de una IP de usuario `192.168.0.xx`; se debe construir la IP completa sin tratarlo como número de inventario. Este PDF será fuente de ubicación y asignación de usuario, mientras que V3.0 prevalece para IP y puertos cuando contenga el dato. El inventario aún no carga los puestos del PDF; esa importación debe conservar fuente y estado de vigencia y evitar completar enlaces por posición gráfica.

Se esperan otros dos Visios para completar o validar nombres de PC, identificadores usuarioPC y nombres reales de usuario.

Los switches de las áreas Régimen y Mesa de Entradas Principal son inaccesibles administrativamente por falta de credenciales. La aplicación muestra ese estado separado de la vigencia de datos. El switch etiquetado «No principal» de Mesa de Entradas queda identificado como secundario del principal del área y conectado a tres equipos de usuario según la confirmación del usuario.

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

## Próximas etapas

1. Verificar edición, auditoría y compatibilidad con bases SQLite anteriores.
2. Agregar pantallas para registrar interfaces y enlaces con fuente, estado de revisión e historial.
3. Incorporar las relaciones de switching y la información de PCs/usuarios de los Visios pendientes.
4. Dibujar la topología gráfica por las vistas física, switching y acceso/IP.
5. Agregar simulaciones aisladas, imágenes identificadas y exportación imprimible de topología.
6. Completar migraciones, respaldos y despliegue seguro.

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
