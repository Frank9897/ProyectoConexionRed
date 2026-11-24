using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;
using ProyectoConexionRed.Web.Services;

namespace ProyectoConexionRed.Web.Controllers;

/// <summary>
/// Permite corregir o justificar los conflictos de datos (IP, MAC o nombre repetidos) desde la página de Topología.
/// Solo modifica el inventario; nunca cambia la red real.
/// </summary>
public class ConflictosController : Controller
{
    // contextoInventario consulta y modifica solo la base documental; cambiarlo selecciona otra sede.
    private readonly InventarioDbContext contextoInventario;

    /// <summary>Prepara el controlador con el contexto del inventario.</summary>
    /// <param name="contexto">Contexto de EF Core; cambiarlo modifica qué fichas se corrigen.</param>
    public ConflictosController(InventarioDbContext contexto)
    {
        contextoInventario = contexto;
    }

    /// <summary>
    /// Cambia o quita el valor conflictivo de una ficha: la IP, la MAC o el nombre. Cada cambio queda en el historial.
    /// </summary>
    /// <param name="tipo">IP, MAC o Nombre.</param>
    /// <param name="idEquipo">Ficha que se corrige.</param>
    /// <param name="valor">Valor nuevo; vacío quita la IP o la MAC (el nombre no puede quedar vacío).</param>
    /// <param name="cancelacion">Señal de cancelación.</param>
    /// <returns>Redirección a la sección de conflictos con el resultado.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Corregir(string tipo, int idEquipo, string? valor, CancellationToken cancelacion)
    {
        var equipo = await contextoInventario.Equipos.FirstOrDefaultAsync(e => e.Identificador == idEquipo, cancelacion);
        if (equipo is null) return Volver("La ficha ya no existe.", true);

        var nuevo = valor?.Trim();
        var eventos = new List<EventoModificacion>();
        var usuario = ObtenerUsuario();
        switch (tipo)
        {
            case ConflictosViewModel.TipoIp:
            {
                if (!string.IsNullOrEmpty(nuevo))
                {
                    // Se aplican las mismas reglas que en el formulario de equipos: formato y duplicados.
                    var equipos = await contextoInventario.Equipos.AsNoTracking().ToListAsync(cancelacion);
                    var validacion = ValidadorTopologia.ValidarIpEquipo(nuevo, idEquipo, equipos);
                    if (validacion.TieneErrores) return Volver(string.Join(" ", validacion.Errores.Select(error => error.Mensaje)), true);
                }

                var ipNueva = string.IsNullOrEmpty(nuevo) ? null : nuevo;
                UtilidadesConexiones.AgregarCambio(eventos, equipo.Identificador, equipo.Nombre, usuario, "IP (conflictos)", equipo.IpAdministracion, ipNueva);
                equipo.IpAdministracion = ipNueva;
                break;
            }
            case ConflictosViewModel.TipoMac:
            {
                string? macNueva = null;
                if (!string.IsNullOrEmpty(nuevo))
                {
                    macNueva = ConflictosService.NormalizarMac(nuevo);
                    if (macNueva is null) return Volver("La MAC debe tener 12 dígitos hexadecimales, por ejemplo AA:BB:CC:DD:EE:FF.", true);
                }

                UtilidadesConexiones.AgregarCambio(eventos, equipo.Identificador, equipo.Nombre, usuario, "Dirección MAC (conflictos)", equipo.DireccionMac, macNueva);
                equipo.DireccionMac = macNueva;
                break;
            }
            case ConflictosViewModel.TipoNombre:
            {
                if (string.IsNullOrEmpty(nuevo)) return Volver("El nombre no puede quedar vacío.", true);
                if (nuevo.Length > 120) return Volver("El nombre admite hasta 120 caracteres.", true);
                var nombreMinuscula = nuevo.ToLower();
                if (await contextoInventario.Equipos.AnyAsync(e => e.Identificador != idEquipo && e.Nombre.ToLower() == nombreMinuscula, cancelacion))
                    return Volver("Ya existe otra ficha con ese nombre; elige uno distinto.", true);

                UtilidadesConexiones.AgregarCambio(eventos, equipo.Identificador, equipo.Nombre, usuario, "Nombre (conflictos)", equipo.Nombre, nuevo);
                equipo.Nombre = nuevo;
                break;
            }
            default:
                return Volver("El tipo de conflicto no es válido.", true);
        }

        await contextoInventario.EventosModificacion.AddRangeAsync(eventos, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);
        if (tipo == ConflictosViewModel.TipoIp)
        {
            // La tabla de IP se actualiza al momento para que el estado de las direcciones refleje el cambio.
            await SincronizadorIps.SincronizarAsync(contextoInventario, cancelacion);
        }

        return Volver("Se guardó el cambio y quedó en el historial.", false);
    }

    /// <summary>
    /// Justifica un conflicto: queda como revisado con su motivo y deja de contarse como pendiente.
    /// </summary>
    /// <param name="tipo">IP, MAC o Nombre.</param>
    /// <param name="valor">Valor repetido (la IP, la MAC o el nombre).</param>
    /// <param name="motivo">Uno de los motivos ofrecidos para ese tipo.</param>
    /// <param name="detalle">Explicación libre; obligatoria si el motivo es «Otro».</param>
    /// <param name="cancelacion">Señal de cancelación.</param>
    /// <returns>Redirección a la sección de conflictos con el resultado.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Justificar(string tipo, string valor, string motivo, string? detalle, CancellationToken cancelacion)
    {
        if (!ConflictosViewModel.MotivosPorTipo.TryGetValue(tipo, out var motivos) || !motivos.Contains(motivo))
            return Volver("Selecciona un motivo de la lista.", true);
        var clave = ConflictosService.Clave(tipo, valor);
        if (clave is null) return Volver("El valor del conflicto no es válido.", true);
        var detalleLimpio = string.IsNullOrWhiteSpace(detalle) ? null : detalle.Trim();
        if (motivo == "Otro" && (detalleLimpio?.Length ?? 0) < 10)
            return Volver("Con el motivo «Otro» explica la situación (al menos 10 caracteres).", true);
        if (detalleLimpio?.Length > 500) return Volver("La explicación admite hasta 500 caracteres.", true);

        var justificacion = await contextoInventario.ConflictosJustificados
            .FirstOrDefaultAsync(c => c.Tipo == tipo && c.Clave == clave, cancelacion);
        if (justificacion is null)
        {
            justificacion = new ConflictoJustificado { Tipo = tipo, Clave = clave };
            await contextoInventario.ConflictosJustificados.AddAsync(justificacion, cancelacion);
        }

        justificacion.Motivo = motivo;
        justificacion.Detalle = detalleLimpio;
        justificacion.NombreUsuario = ObtenerUsuario();
        justificacion.FechaUtc = DateTime.UtcNow;

        // Cada ficha involucrada recibe un evento para que la justificación se vea también en su historial.
        var eventos = new List<EventoModificacion>();
        var equipos = await contextoInventario.Equipos.AsNoTracking().ToListAsync(cancelacion);
        foreach (var equipo in equipos.Where(e => ConflictosService.Clave(tipo, CampoDelConflicto(tipo, e)) == clave))
        {
            UtilidadesConexiones.AgregarCambio(eventos, equipo.Identificador, equipo.Nombre, ObtenerUsuario(),
                "Conflicto justificado", null, $"{tipo} {clave}: {motivo}{(detalleLimpio is null ? string.Empty : " · " + detalleLimpio)}");
        }

        await contextoInventario.EventosModificacion.AddRangeAsync(eventos, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);
        return Volver("El conflicto quedó justificado y se registró en el historial de las fichas.", false);
    }

    /// <summary>Quita la justificación de un conflicto para que vuelva a figurar como pendiente.</summary>
    /// <param name="tipo">IP, MAC o Nombre.</param>
    /// <param name="valor">Valor repetido.</param>
    /// <param name="cancelacion">Señal de cancelación.</param>
    /// <returns>Redirección a la sección de conflictos con el resultado.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revocar(string tipo, string valor, CancellationToken cancelacion)
    {
        var clave = ConflictosService.Clave(tipo, valor);
        var justificacion = clave is null ? null
            : await contextoInventario.ConflictosJustificados.FirstOrDefaultAsync(c => c.Tipo == tipo && c.Clave == clave, cancelacion);
        if (justificacion is null) return Volver("La justificación ya no existe.", true);

        var eventos = new List<EventoModificacion>();
        var equipos = await contextoInventario.Equipos.AsNoTracking().ToListAsync(cancelacion);
        foreach (var equipo in equipos.Where(e => ConflictosService.Clave(tipo, CampoDelConflicto(tipo, e)) == clave))
        {
            UtilidadesConexiones.AgregarCambio(eventos, equipo.Identificador, equipo.Nombre, ObtenerUsuario(),
                "Justificación de conflicto retirada", $"{tipo} {clave}: {justificacion.Motivo}", null);
        }

        contextoInventario.ConflictosJustificados.Remove(justificacion);
        await contextoInventario.EventosModificacion.AddRangeAsync(eventos, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);
        return Volver("Se retiró la justificación: el conflicto vuelve a figurar como pendiente.", false);
    }

    /// <summary>Devuelve el campo de la ficha que se compara según el tipo de conflicto.</summary>
    private static string? CampoDelConflicto(string tipo, EquipoRed equipo) => tipo switch
    {
        ConflictosViewModel.TipoIp => equipo.IpAdministracion,
        ConflictosViewModel.TipoMac => equipo.DireccionMac,
        _ => equipo.Nombre
    };

    /// <summary>Vuelve a la sección de conflictos de Topología mostrando un mensaje de éxito o de error.</summary>
    private IActionResult Volver(string mensaje, bool esError)
    {
        TempData[esError ? "ErrorConflictos" : "MensajeConflictos"] = mensaje;
        return RedirectToAction("Index", "Topologia", null, "conflictos");
    }

    /// <summary>Nombre del usuario que edita, o la marca de uso sin sesión mientras el login esté desactivado.</summary>
    private string ObtenerUsuario() => User.Identity?.Name ?? "Pruebas sin sesión";
}
