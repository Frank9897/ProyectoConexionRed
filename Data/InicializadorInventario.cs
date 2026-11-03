using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Data;

/// <summary>
/// Prepara SQLite y agrega registros iniciales sin duplicarlos.
/// </summary>
public static class InicializadorInventario
{
    /// <summary>
    /// Crea el esquema necesario, completa tablas de acceso si la base existía y carga equipo y administrador inicial.
    /// </summary>
    /// <param name="serviciosAplicacion">Proveedor de servicios; cambiarlo selecciona otra instancia configurada del inventario.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la inicialización pendiente.</param>
    public static async Task InicializarAsync(IServiceProvider serviciosAplicacion, CancellationToken cancelacion = default)
    {
        // alcanceServicios limita la vida del contexto utilizado durante la preparación inicial.
        using var alcanceServicios = serviciosAplicacion.CreateScope();

        // contextoInventario representa la conexión SQLite del servidor; cambiarla dirige la carga a otra base configurada.
        var contextoInventario = alcanceServicios.ServiceProvider.GetRequiredService<InventarioDbContext>();

        // Crear la estructura funciona para la base nueva; en bases existentes no agrega tablas faltantes.
        await contextoInventario.Database.EnsureCreatedAsync(cancelacion);

        // CrearTablasDeAccesoAsync permite conservar bases antiguas que ya tenían EquiposRed.
        await CrearTablasDeAccesoAsync(contextoInventario, cancelacion);

        // CargarEquiposInicialesAsync agrega el catálogo documental una sola vez.
        await CargarEquiposInicialesAsync(contextoInventario, cancelacion);

        // CargarAdministradorInicialAsync utiliza variables de entorno y nunca escribe la contraseña en los registros.
        await CargarAdministradorInicialAsync(
            contextoInventario,
            alcanceServicios.ServiceProvider.GetRequiredService<IPasswordHasher<UsuarioAplicacion>>(),
            cancelacion);
    }

    /// <summary>
    /// Garantiza que existan las tablas e índices de cuentas e historial incluso si EquiposRed ya estaba creada.
    /// </summary>
    /// <param name="contextoInventario">Contexto de la base; cambiarlo selecciona dónde se crean las tablas.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene las instrucciones SQL.</param>
    private static async Task CrearTablasDeAccesoAsync(
        InventarioDbContext contextoInventario,
        CancellationToken cancelacion)
    {
        // comandosSql crea solo tablas de acceso faltantes; cambiar estas sentencias altera su estructura persistida.
        const string comandosSql = """
            CREATE TABLE IF NOT EXISTS "UsuariosAplicacion" (
                "Identificador" INTEGER NOT NULL CONSTRAINT "PK_UsuariosAplicacion" PRIMARY KEY AUTOINCREMENT,
                "NombreUsuario" TEXT NOT NULL,
                "NombreUsuarioNormalizado" TEXT NOT NULL,
                "HashContrasena" TEXT NOT NULL,
                "Rol" TEXT NOT NULL DEFAULT 'Administrador',
                "Activo" INTEGER NOT NULL,
                "FechaCreacionUtc" TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_UsuariosAplicacion_NombreUsuarioNormalizado"
                ON "UsuariosAplicacion" ("NombreUsuarioNormalizado");
            CREATE TABLE IF NOT EXISTS "EventosAcceso" (
                "Identificador" INTEGER NOT NULL CONSTRAINT "PK_EventosAcceso" PRIMARY KEY AUTOINCREMENT,
                "NombreUsuario" TEXT NOT NULL,
                "FechaUtc" TEXT NOT NULL,
                "TipoEvento" TEXT NOT NULL,
                "Resultado" TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_EventosAcceso_FechaUtc"
                ON "EventosAcceso" ("FechaUtc");
            """;

        await contextoInventario.Database.ExecuteSqlRawAsync(comandosSql, cancelacion);
        await AgregarColumnaRolSiFaltaAsync(contextoInventario, cancelacion);
    }

    /// <summary>
    /// Agrega el rol a bases SQLite anteriores y conserva como administradoras las cuentas existentes.
    /// </summary>
    /// <param name="contextoInventario">Contexto de base; cambiarlo aplica el ajuste en otra base.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la lectura del esquema o la alteración.</param>
    private static async Task AgregarColumnaRolSiFaltaAsync(
        InventarioDbContext contextoInventario,
        CancellationToken cancelacion)
    {
        // conexionBase representa la conexión actual de EF; cambiarla inspecciona otra base.
        var conexionBase = contextoInventario.Database.GetDbConnection();
        // cerrarAlFinal indica si esta función abrió la conexión; cambiarlo altera si la conexión se cierra al terminar.
        var cerrarAlFinal = conexionBase.State != ConnectionState.Open;
        if (cerrarAlFinal)
        {
            await conexionBase.OpenAsync(cancelacion);
        }

        // existeColumnaRol evita repetir ALTER TABLE; cambiar su resultado decide si se amplía el esquema.
        var existeColumnaRol = false;
        await using (var comandoEsquema = conexionBase.CreateCommand())
        {
            comandoEsquema.CommandText = "PRAGMA table_info(\"UsuariosAplicacion\")";
            await using var lectorEsquema = await comandoEsquema.ExecuteReaderAsync(cancelacion);
            while (await lectorEsquema.ReadAsync(cancelacion))
            {
                // nombreColumna contiene el nombre leído del esquema; cambiar el índice cambia qué dato se compara.
                var nombreColumna = lectorEsquema.GetString(1);
                if (nombreColumna == "Rol")
                {
                    existeColumnaRol = true;
                    break;
                }
            }
        }

        if (!existeColumnaRol)
        {
            // ALTER TABLE asigna un rol administrador predeterminado a cuentas previas para conservar el acceso.
            await contextoInventario.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"UsuariosAplicacion\" ADD COLUMN \"Rol\" TEXT NOT NULL DEFAULT 'Administrador'",
                cancelacion);
        }

        if (cerrarAlFinal)
        {
            await conexionBase.CloseAsync();
        }
    }

    /// <summary>
    /// Carga equipos iniciales si no hay registros en la tabla de inventario.
    /// </summary>
    /// <param name="contextoInventario">Contexto de datos; cambiarlo selecciona qué inventario recibe el catálogo.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la consulta o escritura.</param>
    private static async Task CargarEquiposInicialesAsync(
        InventarioDbContext contextoInventario,
        CancellationToken cancelacion)
    {
        // inventarioExistente evita repetir los registros semilla; cambiar el criterio altera cuándo se carga el catálogo.
        var inventarioExistente = await contextoInventario.Equipos.AnyAsync(cancelacion);
        if (inventarioExistente)
        {
            return;
        }

        // equiposIniciales contiene dispositivos e IP confirmados, sin datos de credenciales.
        var equiposIniciales = CrearEquiposIniciales();

        // Guardar esta lista carga el inventario documental; no cambia ningún dispositivo de la red real.
        await contextoInventario.Equipos.AddRangeAsync(equiposIniciales, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);
    }

    /// <summary>
    /// Crea la cuenta administradora inicial solo cuando aún no hay cuentas y se configuraron ambas variables de entorno.
    /// </summary>
    /// <param name="contextoInventario">Contexto de usuarios; cambiarlo dirige la creación a otra base.</param>
    /// <param name="codificadorContrasenas">Servicio de hash; cambiarlo modifica el mecanismo de almacenamiento de contraseñas.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la consulta o creación.</param>
    private static async Task CargarAdministradorInicialAsync(
        InventarioDbContext contextoInventario,
        IPasswordHasher<UsuarioAplicacion> codificadorContrasenas,
        CancellationToken cancelacion)
    {
        // usuarioInicial contiene el nombre configurado fuera del repositorio; cambiarlo selecciona otra cuenta administradora.
        var usuarioInicial = Environment.GetEnvironmentVariable("INVENTARIO_ADMIN_USUARIO");

        // contrasenaInicial solo existe durante la creación; cambiarla modifica la clave verificada, que se guarda únicamente como hash.
        var contrasenaInicial = Environment.GetEnvironmentVariable("INVENTARIO_ADMIN_CLAVE");

        if (string.IsNullOrWhiteSpace(usuarioInicial) || string.IsNullOrWhiteSpace(contrasenaInicial))
        {
            return;
        }

        // nombreNormalizado permite una comparación estable; cambiarlo altera la clave única de búsqueda de la cuenta.
        var nombreNormalizado = usuarioInicial.Trim().ToUpperInvariant();

        // hayUsuarios evita reemplazar cuentas ya configuradas; cambiar el resultado habilita o detiene el alta inicial.
        var hayUsuarios = await contextoInventario.Usuarios.AnyAsync(cancelacion);
        if (hayUsuarios)
        {
            return;
        }

        // nuevoUsuario guarda el nombre y la clave derivada; cambiar sus campos altera la cuenta inicial creada.
        var nuevoUsuario = new UsuarioAplicacion
        {
            NombreUsuario = usuarioInicial.Trim(),
            NombreUsuarioNormalizado = nombreNormalizado,
            Rol = "Administrador",
            Activo = true,
            FechaCreacionUtc = DateTime.UtcNow
        };
        nuevoUsuario.HashContrasena = codificadorContrasenas.HashPassword(nuevoUsuario, contrasenaInicial);

        await contextoInventario.Usuarios.AddAsync(nuevoUsuario, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);
    }

    /// <summary>
    /// Devuelve los equipos iniciales sin inventar direcciones ni modelos ausentes.
    /// </summary>
    /// <returns>Lista de equipos documentales; cambiar sus valores modifica la carga inicial en bases vacías.</returns>
    private static List<EquipoRed> CrearEquiposIniciales()
    {
        return
        [
            new() { Nombre = "Switch Aruba 1930 (WDGC)", Fabricante = "Aruba", Modelo = "Instant On 1930", IpAdministracion = "192.168.1.19", Area = "Rack", TipoDispositivo = "Switch", RolEnArea = "Principal", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "El equipo y la IP están confirmados por el usuario; algunas conexiones del rack requieren revisión." },
            new() { Nombre = "HP ProCurve 29100I-24G", Fabricante = "HP", Modelo = "ProCurve 29100I-24G", IpAdministracion = "10.0.1.1", Area = "Rack", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "El Visio indica acceso SSH; algunas conexiones del rack requieren revisión." },
            new() { Nombre = "Switch HP 5500", Fabricante = "HP", Modelo = "5500", IpAdministracion = "192.168.1.218", Area = "Rack", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "Algunas conexiones del rack requieren revisión." },
            new() { Nombre = "Switch HP 1920", Fabricante = "HP", Modelo = "1920", IpAdministracion = "192.168.1.115", Area = "Rack", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "Algunas conexiones del rack requieren revisión." },
            new() { Nombre = "Dell S4148U-ON 48G", Fabricante = "Dell", Modelo = "S4148U-ON", IpAdministracion = "192.168.1.44", Area = "Rack", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "El Visio indica acceso SSH; algunas conexiones del rack requieren revisión." },
            new() { Nombre = "Switch HP 1920S", Fabricante = "HP", Modelo = "1920S", IpAdministracion = "192.168.1.217", Area = "Rack", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "Algunas conexiones del rack requieren revisión." },
            new() { Nombre = "Aruba 1930 (reserva)", Fabricante = "Aruba", Modelo = "Instant On 1930", IpAdministracion = "192.168.1.200", Area = "Reserva", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Cisco Catalyst 3560", Fabricante = "Cisco", Modelo = "Catalyst 3560", Area = "Rack", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "El Visio no muestra una IP para este registro." },
            new() { Nombre = "Switch Agrimensura 1 HP", Fabricante = "HP", IpAdministracion = "192.168.1.23", Area = "Agrimensura", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Switch Agrimensura 3Com", Fabricante = "3Com", IpAdministracion = "192.168.1.24", Area = "Agrimensura", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Asuntos Jurídicos", IpAdministracion = "192.168.1.30", Area = "Asuntos Jurídicos", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Cartografía", IpAdministracion = "192.168.1.51", Area = "Cartografía", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Inmuebles", IpAdministracion = "192.168.1.26", Area = "Inmuebles", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Mesa de Entrada (No principal)", IpAdministracion = "192.168.1.18", Area = "Mesa de Entradas", RolEnArea = "Secundario", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información", Observaciones = "El usuario confirma su función de switch secundario; enlaza al principal del área y da conexión a tres equipos." },
            new() { Nombre = "Puerta Agrimensura", IpAdministracion = "192.168.1.19", Area = "Agrimensura", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Regularización Dominial", IpAdministracion = "192.168.1.40", Area = "Regularización Dominial", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Secretaría", IpAdministracion = "192.168.1.28", Area = "Secretaría", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Sistemas", IpAdministracion = "192.168.1.216", Area = "Sistemas", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Valuación R0", IpAdministracion = "192.168.1.32", Area = "Valuación", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Valuación R1", IpAdministracion = "192.168.1.29", Area = "Valuación", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Valuación R2", IpAdministracion = "192.168.1.20", Area = "Valuación", TipoDispositivo = "Equipo de red", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Switch de Régimen", Area = "Régimen", TipoDispositivo = "Switch", RolEnArea = "No indicado", EstadoDatos = "Vigente", EstadoAdministrativo = "Inaccesible: falta de credenciales", Observaciones = "Equipo e inaccesibilidad confirmados por el usuario; IP no indicada en el registro base." },
            new() { Nombre = "Switch principal de Mesa de Entradas", Area = "Mesa de Entradas", TipoDispositivo = "Switch", RolEnArea = "Principal", EstadoDatos = "Vigente", EstadoAdministrativo = "Inaccesible: falta de credenciales", Observaciones = "Equipo e inaccesibilidad confirmados por el usuario; IP no indicada en el registro base." },
            new() { Nombre = "Switch de área sin identificar (1)", IpAdministracion = "192.168.1.1", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" },
            new() { Nombre = "Switch de área sin identificar (2)", IpAdministracion = "192.168.1.17", TipoDispositivo = "Switch", EstadoDatos = "Vigente", EstadoAdministrativo = "Sin información" }
        ];
    }
}
