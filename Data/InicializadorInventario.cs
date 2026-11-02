using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Data;

/// <summary>
/// Prepara el esquema prototipo y agrega los registros base confirmados por el usuario.
/// </summary>
public static class InicializadorInventario
{
    /// <summary>
    /// Crea la base si falta y siembra los equipos una sola vez cuando el inventario está vacío.
    /// </summary>
    /// <param name="serviciosAplicacion">Proveedor de servicios; cambiarlo selecciona otra instancia configurada del inventario.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla detiene la inicialización pendiente.</param>
    public static async Task InicializarAsync(IServiceProvider serviciosAplicacion, CancellationToken cancelacion = default)
    {
        // alcanceServicios limita la vida del contexto utilizado durante la preparación inicial.
        using var alcanceServicios = serviciosAplicacion.CreateScope();

        // contextoInventario representa la conexión SQLite del servidor; cambiarla dirige la carga a otra base configurada.
        var contextoInventario = alcanceServicios.ServiceProvider.GetRequiredService<InventarioDbContext>();

        // Crear la estructura es adecuado para este prototipo vacío; las siguientes evoluciones deberán usar migraciones.
        await contextoInventario.Database.EnsureCreatedAsync(cancelacion);

        // inventarioExistente permite evitar duplicar los registros semilla; cambiar la consulta altera el criterio de carga inicial.
        var inventarioExistente = await contextoInventario.Equipos.AnyAsync(cancelacion);
        if (inventarioExistente)
        {
            return;
        }

        // equiposIniciales contiene únicamente los dispositivos/IP extraídos y las dos restricciones administrativas confirmadas.
        var equiposIniciales = CrearEquiposIniciales();

        // Guardar esta lista carga el inventario documental; no cambia ningún dispositivo de la red real.
        await contextoInventario.Equipos.AddRangeAsync(equiposIniciales, cancelacion);
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
