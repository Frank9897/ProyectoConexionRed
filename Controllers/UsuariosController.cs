using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;

namespace ProyectoConexionRed.Web.Controllers;

/// <summary>
/// Permite al administrador crear, consultar y desactivar cuentas técnicas de la aplicación.
/// </summary>
[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    // contextoInventario persiste las cuentas; cambiarlo dirige la administración a otra base.
    private readonly InventarioDbContext contextoInventario;

    // codificadorContrasenas transforma la clave en un hash; cambiarlo modifica el formato de almacenamiento de contraseñas.
    private readonly IPasswordHasher<UsuarioAplicacion> codificadorContrasenas;

    /// <summary>
    /// Prepara la administración de cuentas con el contexto y el codificador configurados.
    /// </summary>
    /// <param name="contexto">Contexto de datos; cambiarlo selecciona otra base de usuarios.</param>
    /// <param name="codificador">Codificador; cambiarlo cambia cómo se generan los hashes de nuevas cuentas.</param>
    public UsuariosController(
        InventarioDbContext contexto,
        IPasswordHasher<UsuarioAplicacion> codificador)
    {
        contextoInventario = contexto;
        codificadorContrasenas = codificador;
    }

    /// <summary>
    /// Muestra las cuentas en orden alfabético.
    /// </summary>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe la consulta.</param>
    /// <returns>Vista administrativa con las cuentas existentes.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancelacion)
    {
        // cuentasRegistradas contiene los usuarios visibles; cambiar el orden altera cómo aparecen en la tabla.
        var cuentasRegistradas = await contextoInventario.Usuarios
            .AsNoTracking()
            .OrderBy(cuenta => cuenta.NombreUsuario)
            .ToListAsync(cancelacion);

        return View(cuentasRegistradas);
    }

    /// <summary>
    /// Presenta el formulario para dar de alta un técnico.
    /// </summary>
    /// <returns>Formulario vacío de alta.</returns>
    [HttpGet]
    public IActionResult Crear()
    {
        return View(new AltaUsuarioViewModel());
    }

    /// <summary>
    /// Crea una cuenta técnica activa si no excede el límite de cinco y el nombre no está ocupado.
    /// </summary>
    /// <param name="formulario">Nombre y clave inicial; cambiarlos determina la cuenta que se crea.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe consultas o escritura.</param>
    /// <returns>Redirección al listado o formulario con los errores encontrados.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(AltaUsuarioViewModel formulario, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            formulario.Contrasena = string.Empty;
            return View(formulario);
        }

        // totalCuentas limita el total previsto por el usuario; cambiar el límite permite más o menos cuentas.
        const int totalCuentas = 5;
        // cantidadActual cuenta todas las cuentas, incluso desactivadas; cambiar el conteo modifica el límite aplicado.
        var cantidadActual = await contextoInventario.Usuarios.CountAsync(cancelacion);
        if (cantidadActual >= totalCuentas)
        {
            ModelState.AddModelError(string.Empty, "Se alcanzó el máximo previsto de cinco cuentas.");
            formulario.Contrasena = string.Empty;
            return View(formulario);
        }

        // nombreNormalizado evita duplicados que solo difieren en mayúsculas; cambiarlo modifica la identidad única.
        var nombreNormalizado = formulario.NombreUsuario.Trim().ToUpperInvariant();
        // nombreEnUso comprueba la clave única antes de guardar; cambiar la comparación altera qué nombres se aceptan.
        var nombreEnUso = await contextoInventario.Usuarios
            .AnyAsync(cuenta => cuenta.NombreUsuarioNormalizado == nombreNormalizado, cancelacion);
        if (nombreEnUso)
        {
            ModelState.AddModelError(nameof(formulario.NombreUsuario), "Ese nombre ya está en uso.");
            formulario.Contrasena = string.Empty;
            return View(formulario);
        }

        // nuevaCuenta representa el técnico; cambiar el rol aquí concedería privilegios administrativos a las altas.
        var nuevaCuenta = new UsuarioAplicacion
        {
            NombreUsuario = formulario.NombreUsuario.Trim(),
            NombreUsuarioNormalizado = nombreNormalizado,
            Rol = "Tecnico",
            Activo = true,
            FechaCreacionUtc = DateTime.UtcNow
        };
        nuevaCuenta.HashContrasena = codificadorContrasenas.HashPassword(nuevaCuenta, formulario.Contrasena);

        await contextoInventario.Usuarios.AddAsync(nuevaCuenta, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);

        TempData["Mensaje"] = $"Se creó la cuenta técnica {nuevaCuenta.NombreUsuario}.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Activa o desactiva una cuenta, evitando el auto bloqueo y la desactivación del último administrador activo.
    /// </summary>
    /// <param name="id">Identificador de cuenta; cambiarlo selecciona otra cuenta objetivo.</param>
    /// <param name="cancelacion">Señal de cancelación; activarla interrumpe lectura o escritura.</param>
    /// <returns>Redirección al listado con el resultado de la operación.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, CancellationToken cancelacion)
    {
        // cuentaObjetivo selecciona el registro de destino; cambiar id modifica cuál cuenta se intenta activar o desactivar.
        var cuentaObjetivo = await contextoInventario.Usuarios
            .SingleOrDefaultAsync(cuenta => cuenta.Identificador == id, cancelacion);
        if (cuentaObjetivo is null)
        {
            return NotFound();
        }

        // identificadorActual evita que el administrador desactive su propia sesión; cambiarlo altera la cuenta protegida.
        var identificadorActual = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (identificadorActual == id.ToString() && cuentaObjetivo.Activo)
        {
            TempData["Error"] = "No se puede desactivar la cuenta que está usando.";
            return RedirectToAction(nameof(Index));
        }

        if (cuentaObjetivo.Activo && cuentaObjetivo.Rol == "Administrador")
        {
            // administradoresActivos cuenta las cuentas capaces de administrar; cambiar el criterio altera la protección de último administrador.
            var administradoresActivos = await contextoInventario.Usuarios
                .CountAsync(cuenta => cuenta.Activo && cuenta.Rol == "Administrador", cancelacion);
            if (administradoresActivos <= 1)
            {
                TempData["Error"] = "No se puede desactivar la última cuenta administradora activa.";
                return RedirectToAction(nameof(Index));
            }
        }

        cuentaObjetivo.Activo = !cuentaObjetivo.Activo;
        await contextoInventario.SaveChangesAsync(cancelacion);
        TempData["Mensaje"] = $"La cuenta {cuentaObjetivo.NombreUsuario} ahora está {(cuentaObjetivo.Activo ? "activa" : "desactivada")}.";
        return RedirectToAction(nameof(Index));
    }
}
