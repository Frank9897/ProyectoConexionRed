using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Data;

/// <summary>
/// Expone las tablas del inventario y configura sus restricciones básicas.
/// </summary>
public class InventarioDbContext : DbContext
{
    /// <summary>
    /// Crea el contexto de base de datos con las opciones configuradas por ASP.NET Core.
    /// </summary>
    /// <param name="opciones">Proveedor y conexión; cambiarlos modifica la base a la que accede este contexto.</param>
    public InventarioDbContext(DbContextOptions<InventarioDbContext> opciones) : base(opciones)
    {
    }

    /// <summary>
    /// Conjunto de equipos documentados; cambiar los registros modifica el inventario persistido.
    /// </summary>
    public DbSet<EquipoRed> Equipos => Set<EquipoRed>();

    /// <summary>
    /// Conjunto de cuentas locales; cambiar sus registros modifica quién puede iniciar sesión.
    /// </summary>
    public DbSet<UsuarioAplicacion> Usuarios => Set<UsuarioAplicacion>();

    /// <summary>
    /// Conjunto de accesos registrados; cambiar sus filas modifica el historial de autenticación.
    /// </summary>
    public DbSet<EventoAcceso> EventosAcceso => Set<EventoAcceso>();

    /// <summary>
    /// Conjunto de cambios de inventario; cambiar sus filas modifica el historial documental.
    /// </summary>
    public DbSet<EventoModificacion> EventosModificacion => Set<EventoModificacion>();

    /// <summary>
    /// Conjunto de interfaces físicas documentadas; cambiar sus filas modifica los puertos registrados.
    /// </summary>
    public DbSet<InterfazEquipo> InterfacesEquipo => Set<InterfazEquipo>();

    /// <summary>
    /// Conjunto de enlaces físicos registrados; cambiar sus filas modifica las conexiones dibujables.
    /// </summary>
    public DbSet<EnlaceFisico> EnlacesFisicos => Set<EnlaceFisico>();

    /// <summary>
    /// Define nombres y límites de columnas; cambiar estas reglas requiere actualizar el esquema de la base.
    /// </summary>
    /// <param name="constructorModelo">Constructor de metadatos; cambiar su configuración altera el esquema generado.</param>
    protected override void OnModelCreating(ModelBuilder constructorModelo)
    {
        base.OnModelCreating(constructorModelo);

        // configuracionEquipo reúne el esquema de EquipoRed; cambiar sus límites modifica sus columnas e índices.
        var configuracionEquipo = constructorModelo.Entity<EquipoRed>();
        configuracionEquipo.ToTable("EquiposRed");
        configuracionEquipo.HasKey(nameof(EquipoRed.Identificador));
        configuracionEquipo.Property(nameof(EquipoRed.Nombre)).HasMaxLength(160).IsRequired();
        configuracionEquipo.Property(nameof(EquipoRed.Fabricante)).HasMaxLength(100);
        configuracionEquipo.Property(nameof(EquipoRed.Modelo)).HasMaxLength(120);
        configuracionEquipo.Property(nameof(EquipoRed.IpAdministracion)).HasMaxLength(45);
        configuracionEquipo.Property(nameof(EquipoRed.DireccionMac)).HasMaxLength(32);
        configuracionEquipo.Property(nameof(EquipoRed.Area)).HasMaxLength(120);
        configuracionEquipo.Property(nameof(EquipoRed.RolEnArea)).HasMaxLength(60).IsRequired();
        configuracionEquipo.Property(nameof(EquipoRed.TipoDispositivo)).HasMaxLength(100).IsRequired();
        configuracionEquipo.Property(nameof(EquipoRed.EstadoAdministrativo)).HasMaxLength(100).IsRequired();
        configuracionEquipo.Property(nameof(EquipoRed.EstadoDatos)).HasMaxLength(60).IsRequired();
        configuracionEquipo.Property(nameof(EquipoRed.Observaciones)).HasMaxLength(1000);
        configuracionEquipo.HasIndex(nameof(EquipoRed.Nombre));
        configuracionEquipo.HasIndex(nameof(EquipoRed.IpAdministracion));

        // configuracionUsuario reúne las cuentas locales; cambiar límites o índice modifica validaciones y búsquedas.
        var configuracionUsuario = constructorModelo.Entity<UsuarioAplicacion>();
        configuracionUsuario.ToTable("UsuariosAplicacion");
        configuracionUsuario.HasKey(nameof(UsuarioAplicacion.Identificador));
        configuracionUsuario.Property(nameof(UsuarioAplicacion.NombreUsuario)).HasMaxLength(100).IsRequired();
        configuracionUsuario.Property(nameof(UsuarioAplicacion.NombreUsuarioNormalizado)).HasMaxLength(100).IsRequired();
        configuracionUsuario.Property(nameof(UsuarioAplicacion.HashContrasena)).HasMaxLength(500).IsRequired();
        configuracionUsuario.Property(nameof(UsuarioAplicacion.Rol)).HasMaxLength(30).IsRequired();
        configuracionUsuario.HasIndex(nameof(UsuarioAplicacion.NombreUsuarioNormalizado)).IsUnique();

        // configuracionEvento reúne el historial de acceso; cambiar sus límites altera los datos que pueden registrarse.
        var configuracionEvento = constructorModelo.Entity<EventoAcceso>();
        configuracionEvento.ToTable("EventosAcceso");
        configuracionEvento.HasKey(nameof(EventoAcceso.Identificador));
        configuracionEvento.Property(nameof(EventoAcceso.NombreUsuario)).HasMaxLength(100).IsRequired();
        configuracionEvento.Property(nameof(EventoAcceso.TipoEvento)).HasMaxLength(60).IsRequired();
        configuracionEvento.Property(nameof(EventoAcceso.Resultado)).HasMaxLength(40).IsRequired();
        configuracionEvento.HasIndex(nameof(EventoAcceso.FechaUtc));

        // configuracionCambio reúne los cambios de fichas; cambiar límites altera qué valores caben en la auditoría.
        var configuracionCambio = constructorModelo.Entity<EventoModificacion>();
        configuracionCambio.ToTable("EventosModificacion");
        configuracionCambio.HasKey(nameof(EventoModificacion.Identificador));
        configuracionCambio.Property(nameof(EventoModificacion.NombreEquipo)).HasMaxLength(160).IsRequired();
        configuracionCambio.Property(nameof(EventoModificacion.NombreUsuario)).HasMaxLength(100).IsRequired();
        configuracionCambio.Property(nameof(EventoModificacion.Campo)).HasMaxLength(100).IsRequired();
        configuracionCambio.Property(nameof(EventoModificacion.ValorAnterior)).HasMaxLength(2000);
        configuracionCambio.Property(nameof(EventoModificacion.ValorNuevo)).HasMaxLength(2000);
        configuracionCambio.HasIndex(nameof(EventoModificacion.FechaUtc));
        configuracionCambio.HasIndex(nameof(EventoModificacion.IdentificadorEquipo));

        // configuracionInterfaz define puertos y equipo propietario; cambiar límites altera las etiquetas admitidas.
        var configuracionInterfaz = constructorModelo.Entity<InterfazEquipo>();
        configuracionInterfaz.ToTable("InterfacesEquipo");
        configuracionInterfaz.HasKey(nameof(InterfazEquipo.Identificador));
        configuracionInterfaz.Property(nameof(InterfazEquipo.EtiquetaPuerto)).HasMaxLength(100).IsRequired();
        configuracionInterfaz.Property(nameof(InterfazEquipo.TipoInterfaz)).HasMaxLength(100).IsRequired();
        configuracionInterfaz.Property(nameof(InterfazEquipo.EstadoDatos)).HasMaxLength(60).IsRequired();
        configuracionInterfaz.Property(nameof(InterfazEquipo.Observaciones)).HasMaxLength(1000);
        configuracionInterfaz.HasIndex(nameof(InterfazEquipo.IdentificadorEquipo), nameof(InterfazEquipo.EtiquetaPuerto)).IsUnique();
        configuracionInterfaz.HasOne<EquipoRed>()
            .WithMany()
            .HasForeignKey(nameof(InterfazEquipo.IdentificadorEquipo))
            .OnDelete(DeleteBehavior.Restrict);

        // configuracionEnlace une dos interfaces; cambiar sus claves cambia los puertos conectados en el inventario.
        var configuracionEnlace = constructorModelo.Entity<EnlaceFisico>();
        configuracionEnlace.ToTable("EnlacesFisicos");
        configuracionEnlace.HasKey(nameof(EnlaceFisico.Identificador));
        configuracionEnlace.Property(nameof(EnlaceFisico.Medio)).HasMaxLength(60).IsRequired();
        configuracionEnlace.Property(nameof(EnlaceFisico.ModuloOrigen)).HasMaxLength(120);
        configuracionEnlace.Property(nameof(EnlaceFisico.ModuloDestino)).HasMaxLength(120);
        configuracionEnlace.Property(nameof(EnlaceFisico.EstadoDatos)).HasMaxLength(60).IsRequired();
        configuracionEnlace.Property(nameof(EnlaceFisico.Fuente)).HasMaxLength(300);
        configuracionEnlace.Property(nameof(EnlaceFisico.Observaciones)).HasMaxLength(1000);
        configuracionEnlace.HasIndex(nameof(EnlaceFisico.IdentificadorPuertoOrigen));
        configuracionEnlace.HasIndex(nameof(EnlaceFisico.IdentificadorPuertoDestino));
        configuracionEnlace.HasOne<InterfazEquipo>()
            .WithMany()
            .HasForeignKey(nameof(EnlaceFisico.IdentificadorPuertoOrigen))
            .OnDelete(DeleteBehavior.Restrict);
        configuracionEnlace.HasOne<InterfazEquipo>()
            .WithMany()
            .HasForeignKey(nameof(EnlaceFisico.IdentificadorPuertoDestino))
            .OnDelete(DeleteBehavior.Restrict);
    }
}
