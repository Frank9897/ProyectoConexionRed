using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Controllers;

/// <summary>
/// Gestiona el inicio y cierre de sesión, además de registrar los intentos de acceso.
/// </summary>
public class CuentaController : Controller
{
    // contextoInventario permite buscar usuarios y guardar eventos; cambiarlo cambia la base autenticada.
    private readonly InventarioDbContext contextoInventario;

    // codificadorContrasenas verifica hashes sin recuperar la contraseña original; cambiarlo altera el algoritmo de verificación.
    private readonly IPasswordHasher<UsuarioAplicacion> codificadorContrasenas;

    /// <summary>
    /// Prepara el controlador con el inventario y el verificador de contraseñas.
    /// </summary>
    /// <param name="contexto">Contexto de datos; cambiarlo selecciona otra base de cuentas e historial.</param>
    /// <param name="codificador">Verificador de contraseñas; cambiarlo modifica cómo se validan los hashes.</param>
    public CuentaController(
        InventarioDbContext contexto,
        IPasswordHasher<UsuarioAplicacion> codificador)
    {
        contextoInventario = contexto;
        codificadorContrasenas = codificador;
    }

    /// <summary>
    /// Presenta el formulario de ingreso y conserva solo destinos internos de retorno.
    /// </summary>
    /// <param name="urlRetorno">Dirección solicitada antes del ingreso; cambiarla cambia el destino local posterior.</param>
    /// <returns>Formulario vacío de acceso con destino seguro, si corresponde.</returns>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Ingresar(string? urlRetorno = null)
    {
        // formularioIngreso conserva el destino local; cambiarlo modifica a qué página volverá una sesión válida.
        var formularioIngreso = new InicioSesionViewModel
        {
            UrlRetorno = Url.IsLocalUrl(urlRetorno) ? urlRetorno : null
        };

        return View(formularioIngreso);
    }

    /// <summary>
    /// Verifica la cuenta, registra el resultado y crea una cookie de sesión si las credenciales son válidas.
    /// </summary>
    /// <param name="formulario">Usuario y contraseña ingresados; cambiarlos modifica qué credenciales se verifican.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la consulta o escritura de auditoría.</param>
    /// <returns>Redirección local tras un ingreso válido o el formulario con error.</returns>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingresar(InicioSesionViewModel formulario, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        // nombreIngresado conserva el identificador escrito; cambiarlo selecciona otra cuenta candidata.
        var nombreIngresado = formulario.NombreUsuario.Trim();

        // nombreNormalizado permite comparar cuentas sin diferencias de mayúsculas; cambiarlo modifica el usuario buscado.
        var nombreNormalizado = nombreIngresado.ToUpperInvariant();

        // usuarioEncontrado busca la cuenta; cambiar el predicado altera qué registro puede autenticarse.
        var usuarioEncontrado = await contextoInventario.Usuarios
            .SingleOrDefaultAsync(
                usuarioCandidato => usuarioCandidato.NombreUsuarioNormalizado == nombreNormalizado,
                cancelacion);

        // credencialesValidas indica si la cuenta existe, está activa y su hash coincide; cambiar el resultado altera el acceso.
        var credencialesValidas = usuarioEncontrado is not null
            && usuarioEncontrado.Activo
            && codificadorContrasenas.VerifyHashedPassword(
                usuarioEncontrado,
                usuarioEncontrado.HashContrasena,
                formulario.Contrasena) != PasswordVerificationResult.Failed;

        // resultadoAcceso describe el intento para auditoría; cambiarlo cambia el estado guardado en el historial.
        var resultadoAcceso = credencialesValidas ? "Correcto" : "Fallido";

        // eventoUsuario usa el nombre enviado; cambiarlo altera el usuario que aparece en el registro.
        await RegistrarEventoAsync(nombreIngresado, "Inicio de sesión", resultadoAcceso, cancelacion);

        if (!credencialesValidas || usuarioEncontrado is null)
        {
            ModelState.AddModelError(string.Empty, "El usuario o la contraseña no son correctos.");
            formulario.Contrasena = string.Empty;
            return View(formulario);
        }

        // identidadUsuario contiene la identidad firmada en la cookie; cambiar sus reclamos altera lo que conoce la sesión.
        var identidadUsuario = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, usuarioEncontrado.Identificador.ToString()),
            new Claim(ClaimTypes.Name, usuarioEncontrado.NombreUsuario),
            new Claim(ClaimTypes.Role, usuarioEncontrado.Rol)
        ],
        CookieAuthenticationDefaults.AuthenticationScheme);

        // sesionUsuario representa la identidad autenticada; cambiarla cambia el usuario asociado a la cookie.
        var sesionUsuario = new ClaimsPrincipal(identidadUsuario);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, sesionUsuario);

        // destinoSeguro evita redirecciones a sitios externos; cambiarlo altera la página posterior al ingreso.
        var destinoSeguro = Url.IsLocalUrl(formulario.UrlRetorno)
            ? formulario.UrlRetorno!
            : Url.Action("Index", "Home")!;

        return LocalRedirect(destinoSeguro);
    }

    /// <summary>
    /// Registra el cierre de sesión y elimina la cookie de autenticación.
    /// </summary>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe el registro del evento.</param>
    /// <returns>Redirección al formulario de ingreso tras cerrar la sesión.</returns>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir(CancellationToken cancelacion)
    {
        // nombreUsuario conserva la identidad actual; cambiarlo altera qué cuenta figura en el cierre.
        var nombreUsuario = User.Identity?.Name ?? "Usuario desconocido";
        await RegistrarEventoAsync(nombreUsuario, "Cierre de sesión", "Correcto", cancelacion);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(Ingresar));
    }

    /// <summary>
    /// Persiste un evento de acceso sin guardar contraseñas, contenido del formulario ni tokens.
    /// </summary>
    /// <param name="nombreUsuario">Cuenta involucrada; cambiarlo modifica el nombre registrado.</param>
    /// <param name="tipoEvento">Tipo de acceso; cambiarlo modifica si se registra ingreso o cierre.</param>
    /// <param name="resultado">Resultado del intento; cambiarlo modifica el estado mostrado en el historial.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la escritura.</param>
    private async Task RegistrarEventoAsync(
        string nombreUsuario,
        string tipoEvento,
        string resultado,
        CancellationToken cancelacion)
    {
        // nuevoEvento contiene solo metadatos del acceso; cambiar sus valores modifica la línea de auditoría.
        var nuevoEvento = new EventoAcceso
        {
            NombreUsuario = nombreUsuario,
            FechaUtc = DateTime.UtcNow,
            TipoEvento = tipoEvento,
            Resultado = resultado
        };

        await contextoInventario.EventosAcceso.AddAsync(nuevoEvento, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);
    }
}
