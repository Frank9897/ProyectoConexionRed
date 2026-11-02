using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web;

/// <summary>
/// Inicia el servidor web, conecta la base SQLite y aplica el control de acceso.
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

        // opcionesCookie configura el inicio de sesión; cambiar la duración o las propiedades afecta el uso de las sesiones.
        constructorWeb.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(opcionesCookie =>
            {
                opcionesCookie.LoginPath = "/Cuenta/Ingresar";
                opcionesCookie.AccessDeniedPath = "/Cuenta/Ingresar";
                opcionesCookie.Cookie.Name = "ProyectoConexionRed.Acceso";
                opcionesCookie.Cookie.HttpOnly = true;
                opcionesCookie.Cookie.SameSite = SameSiteMode.Lax;
                opcionesCookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                opcionesCookie.ExpireTimeSpan = TimeSpan.FromHours(8);
                opcionesCookie.SlidingExpiration = true;
            });

        constructorWeb.Services.AddAuthorization();
        constructorWeb.Services.AddScoped<IPasswordHasher<UsuarioAplicacion>, PasswordHasher<UsuarioAplicacion>>();

        // opcionesMvc exige sesión válida para los controladores; cambiar el filtro global altera el acceso a las páginas.
        constructorWeb.Services.AddControllersWithViews(opcionesMvc =>
            opcionesMvc.Filters.Add(new AuthorizeFilter()));

        // aplicacionWeb representa el servidor construido; cambiar su canalización modifica cómo se atienden las solicitudes.
        var aplicacionWeb = constructorWeb.Build();

        aplicacionWeb.UseStaticFiles();
        aplicacionWeb.UseAuthentication();
        aplicacionWeb.UseAuthorization();

        // Enrutamiento convencional: cambiar el patrón altera la dirección predeterminada de las páginas.
        aplicacionWeb.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        // La inicialización crea el esquema, carga equipos y crea el primer administrador si las variables están configuradas.
        await InicializadorInventario.InicializarAsync(aplicacionWeb.Services);

        await aplicacionWeb.RunAsync();
    }
}
