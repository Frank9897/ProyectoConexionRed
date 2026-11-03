# ProyectoConexionRed

Aplicación web en español para documentar y visualizar la infraestructura de red empresarial. El repositorio es público por decisión del usuario.

## Propósito

Reunir en un inventario navegable los dispositivos, IP, puertos, enlaces, áreas, PCs y usuarios de la red; mostrar su topología por vistas física, switching y red; registrar cambios; y permitir simulaciones locales y exportación imprimible.

## Estado del desarrollo

**MVP de inventario, acceso e historial de logueos en desarrollo.** La aplicación usa ASP.NET Core MVC, Entity Framework Core y SQLite. Incluye una página de inicio, una tabla de inventario con búsqueda por nombre, IP, área, modelo o fabricante, descarga CSV de los resultados y formato de impresión para esa tabla. Carga 25 registros documentales iniciales cuando la base está vacía.

El acceso requiere sesión. La cuenta administradora inicial solo se crea cuando la base todavía no contiene usuarios y se configuran las variables de entorno `INVENTARIO_ADMIN_USUARIO` y `INVENTARIO_ADMIN_CLAVE`. La clave se guarda como hash. La cuenta inicial recibe rol Administrador. El administrador puede crear cuentas Técnicas, hasta cinco cuentas en total, y activar o desactivar cuentas. Las cuentas existentes reciben rol Administrador al actualizar una base anterior, para no quitar el acceso que ya tenían. El administrador no puede desactivar su propia cuenta activa ni la última cuenta administradora activa. La pantalla «Accesos» muestra los últimos cien inicios de sesión y cierres de sesión, con hora UTC y resultado; no registra contraseñas.

La aplicación permite editar los campos documentales de cada equipo y guarda en un historial el usuario, la fecha UTC, el campo y los valores anterior y nuevo. El historial es de solo lectura desde la web y presenta hasta doscientos eventos recientes. Estos cambios actualizan SQLite y no ejecutan acciones sobre la red. La página Topología presenta las vistas física, switching y de acceso/IP, e informa cuando no hay enlaces documentados. Ya existe el esquema SQLite para interfaces y enlaces físicos, todavía sin datos cargados ni pantalla de edición de conexiones. La topología gráfica interactiva, la ficha SFP y la exportación gráfica siguen pendientes. Falta aplicar límites a intentos de acceso, verificar la aplicación y completar HTTPS antes de considerar un despliegue de producción.

El archivo **DIAGRAMA RED V2.vsdx** es la fuente base. El usuario confirmó que los dispositivos, IP y datos de los equipos de usuario están vigentes. Algunas conexiones del rack necesitan revisión y algunos SFP están pendientes de actualizar. Se esperan otros dos Visios para incorporar nombres de PC, usuarioPC y nombres de usuario.

Los switches de las áreas Régimen y Mesa de Entradas Principal son inaccesibles administrativamente por falta de credenciales. La aplicación muestra ese estado separado de la vigencia de datos. El switch etiquetado «No principal» de Mesa de Entradas queda identificado como secundario del principal del área y conectado a tres equipos de usuario según la confirmación del usuario.

El inventario detallado y las reglas de continuidad están en [docs/PROMPT_IA.md](docs/PROMPT_IA.md).

## Ejecutar localmente

Requiere el SDK de .NET 10. Antes del primer inicio, configura la cuenta administradora en el entorno de la sesión, no en archivos del repositorio. Ejemplo para PowerShell:

```powershell
# usuarioInicial define el nombre de la cuenta creada si la base todavía no tiene usuarios.
$usuarioInicial = Read-Host "Usuario inicial"
$env:INVENTARIO_ADMIN_USUARIO = $usuarioInicial

# contrasenaProtegida recibe la clave sin mostrarla mientras se escribe.
$contrasenaProtegida = Read-Host "Contraseña inicial" -AsSecureString

# La variable de entorno se usa solo para el alta inicial; no la guardes en scripts ni en el repositorio.
$env:INVENTARIO_ADMIN_CLAVE = [System.Net.NetworkCredential]::new("", $contrasenaProtegida).Password

dotnet run --project ProyectoConexionRed.Web.csproj

# Quitar los valores de la sesión al detener la aplicación.
Remove-Item Env:INVENTARIO_ADMIN_USUARIO
Remove-Item Env:INVENTARIO_ADMIN_CLAVE
Remove-Variable contrasenaProtegida
Remove-Variable usuarioInicial
```

Al iniciar, se crea `proyecto-conexion-red.db` y se agregan los registros iniciales si todavía no existen. Si ya existe una cuenta, las variables de alta inicial no modifican ni restablecen usuarios.

## Arquitectura

- C# y ASP.NET Core MVC.
- Entity Framework Core con proveedor SQLite.
- Cookie de autenticación HTTP Only; en producción se requiere HTTPS.
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

## Reglas para contribuir

- Variables en español; comentar cada variable explicando qué representa y el efecto de cambiar su valor.
- Funciones documentadas en español, incluyendo qué hacen sus parámetros y cómo afectan el resultado.
- Actualizar este README y [docs/PROMPT_IA.md](docs/PROMPT_IA.md) en cada modificación.
- Usar mensajes de commit breves y en español.
- No incluir secretos en código, documentación, ejemplos, capturas o commits.
