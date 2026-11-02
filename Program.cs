using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;

namespace ProyectoConexionRed.Web;

/// <summary>
/// Inicia el servidor web, conecta la base SQLite y carga el inventario inicial.
/// </summary>
public class Program
{
    /// <summary>
    /// Configura y ejecuta la aplicación web.
    /// </summary>
    /// <param name="args">Argumentos de inicio; modificarlos cambia las opciones de arranque recibidas por ASP.NET Core.</param>
    public static async Task Main(string[] args)
    {
        // constructorWeb contiene servicios, configuración y opciones de inicio; cambiar su configuración afecta toda la aplicación.
        var constructorWeb = WebApplication.CreateBuilder(args);

        // opcionesSqlite define el proveedor y la conexión; cambiar la cadena apunta la aplicación a otro archivo de base de datos.
        constructorWeb.Services.AddDbContext<InventarioDbContext>(opcionesSqlite =>
            opcionesSqlite.UseSqlite(constructorWeb.Configuration.GetConnectionString("ConexionRed")));

        // Se registran controladores y vistas para habilitar la navegación MVC.
        constructorWeb.Services.AddControllersWithViews();

        // aplicacionWeb representa el servidor construido; cambiar su canalización modifica cómo se atienden las solicitudes.
        var aplicacionWeb = constructorWeb.Build();

        // Enrutamiento convencional: cambiar el patrón altera la dirección predeterminada de las páginas.
        aplicacionWeb.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        // La inicialización crea el esquema prototipo y carga los equipos solo si la base aún está vacía.
        await InicializadorInventario.InicializarAsync(aplicacionWeb.Services);

        await aplicacionWeb.RunAsync();
    }
}
