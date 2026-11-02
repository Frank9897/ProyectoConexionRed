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
    /// Define nombres y límites de columnas; cambiar estas reglas requiere actualizar el esquema de la base.
    /// </summary>
    /// <param name="constructorModelo">Constructor de metadatos; cambiar su configuración altera el esquema generado.</param>
    protected override void OnModelCreating(ModelBuilder constructorModelo)
    {
        base.OnModelCreating(constructorModelo);

        // configuracionEquipo reúne el esquema de EquipoRed; cambiar sus límites modifica las columnas e índices de la base.
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
    }
}
