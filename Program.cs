using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
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

        // marcaInicioAplicacion cambia al iniciar el proceso y permite invalidar las cookies de ejecuciones anteriores.
        constructorWeb.Services.AddSingleton<IdentificadorInicioAplicacion>();

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
                // contextoValidacion consulta el estado actual en cada solicitud autenticada; cambiar esta política altera cuándo se revocan sesiones.
                opcionesCookie.Events.OnValidatePrincipal = async contextoValidacion =>
                {
                    // identificadorInicioVigente representa esta ejecución; reemplazarlo invalida los ingresos previos al reinicio.
                    var identificadorInicioVigente = contextoValidacion.HttpContext.RequestServices
                        .GetRequiredService<IdentificadorInicioAplicacion>().Valor;
                    // identificadorInicioCookie lee la marca firmada; cambiarla determina si esta cookie pertenece al proceso actual.
                    var identificadorInicioCookie = contextoValidacion.Principal?
                        .FindFirstValue(IdentificadorInicioAplicacion.TipoReclamo);
                    if (!string.Equals(identificadorInicioCookie, identificadorInicioVigente, StringComparison.Ordinal))
                    {
                        contextoValidacion.RejectPrincipal();
                        await contextoValidacion.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return;
                    }

                    // identificadorCuenta conserva el ID firmado al iniciar sesión; cambiar su origen altera qué cuenta se valida.
                    var identificadorCuenta = contextoValidacion.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    // baseDeCuentas consulta el estado persistido; cambiar el contexto cambia la fuente de validación.
                    var baseDeCuentas = contextoValidacion.HttpContext.RequestServices.GetRequiredService<InventarioDbContext>();
                    // idCuenta convierte el identificador firmado; cambiarlo modifica la cuenta que se consulta.
                    var idCuenta = 0;
                    // cuentaActualizada determina si la cuenta sigue activa y qué rol debe tener la sesión.
                    var cuentaActualizada = int.TryParse(identificadorCuenta, out idCuenta)
                        ? await baseDeCuentas.Usuarios.AsNoTracking()
                            .SingleOrDefaultAsync(cuenta => cuenta.Identificador == idCuenta)
                        : null;

                    if (cuentaActualizada is null || !cuentaActualizada.Activo)
                    {
                        contextoValidacion.RejectPrincipal();
                        await contextoValidacion.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return;
                    }

                    // rolEnCookie compara privilegios firmados con el rol vigente; cambiar la comparación altera cuándo se renuevan.
                    var rolEnCookie = contextoValidacion.Principal?.FindFirstValue(ClaimTypes.Role);
                    if (rolEnCookie != cuentaActualizada.Rol)
                    {
                        // identidadActualizada conserva la sesión y reemplaza sus datos por los vigentes.
                        var identidadActualizada = (ClaimsIdentity)contextoValidacion.Principal!.Identity!;
                        // reclamoRolAnterior permite reemplazar el rol si existía; cambiarlo altera qué reclamo se retira.
                        var reclamoRolAnterior = identidadActualizada.FindFirst(ClaimTypes.Role);
                        if (reclamoRolAnterior is not null)
                        {
                            identidadActualizada.RemoveClaim(reclamoRolAnterior);
                        }

                        identidadActualizada.AddClaim(new Claim(ClaimTypes.Role, cuentaActualizada.Rol));
                        contextoValidacion.ShouldRenew = true;
                    }
                };
            });

        constructorWeb.Services.AddAuthorization();
        constructorWeb.Services.AddScoped<IPasswordHasher<UsuarioAplicacion>, PasswordHasher<UsuarioAplicacion>>();

        // opcionesMvc exige sesión válida para los controladores; cambiar el filtro global altera el acceso a las páginas.
        // MODO TEMPORAL SIN LOGIN: el filtro global queda comentado para abrir los módulos durante las pruebas.
        // Al finalizar, descomenta estas líneas para volver a exigir una identidad autenticada.
        // constructorWeb.Services.AddControllersWithViews(opcionesMvc =>
        //     opcionesMvc.Filters.Add(new AuthorizeFilter()));
        constructorWeb.Services.AddControllersWithViews();

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
