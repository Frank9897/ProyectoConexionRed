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

        constructorModelo.Entity<EquipoRed>(entidad =>
        {
            entidad.ToTable("EquiposRed");
            entidad.HasKey(equipo => equipo.Identificador);
            entidad.Property(equipo => equipo.Nombre).HasMaxLength(160).IsRequired();
            entidad.Property(equipo => equipo.Fabricante).HasMaxLength(100);
            entidad.Property(equipo => equipo.Modelo).HasMaxLength(120);
            entidad.Property(equipo => equipo.IpAdministracion).HasMaxLength(45);
            entidad.Property(equipo => equipo.DireccionMac).HasMaxLength(32);
            entidad.Property(equipo => equipo.Area).HasMaxLength(120);
            entidad.Property(equipo => equipo.RolEnArea).HasMaxLength(60).IsRequired();
            entidad.Property(equipo => equipo.TipoDispositivo).HasMaxLength(100).IsRequired();
            entidad.Property(equipo => equipo.EstadoAdministrativo).HasMaxLength(100).IsRequired();
            entidad.Property(equipo => equipo.EstadoDatos).HasMaxLength(60).IsRequired();
            entidad.Property(equipo => equipo.Observaciones).HasMaxLength(1000);
            entidad.HasIndex(equipo => equipo.Nombre);
            entidad.HasIndex(equipo => equipo.IpAdministracion);
        });
    }
}
