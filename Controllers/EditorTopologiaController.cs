using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoConexionRed.Web.Data;
using ProyectoConexionRed.Web.Models;
using ProyectoConexionRed.Web.Services;

namespace ProyectoConexionRed.Web.Controllers;

/// <summary>
/// Mapa editable: entrega el grafo documentado en JSON y permite crear, validar y eliminar enlaces físicos y guardar la
/// disposición de los equipos. Solo escribe en SQLite; nunca consulta ni cambia la red real.
/// </summary>
public class EditorTopologiaController : Controller
{
    // VistaGeneral es la vista por defecto cuando no se indica ninguna; las vistas válidas son general, switches, todo y foco-IDENTIFICADOR.
    private const string VistaGeneral = "general";

    // MediosPermitidos, EstadosDatosPermitidos y EstadosStpPermitidos replican las listas cerradas de los formularios de enlaces.
    private static readonly string[] MediosPermitidos = ["No confirmado", "Cobre RJ45", "Fibra", "DAC", "Otro"];
    private static readonly string[] EstadosDatosPermitidos = ["Pendiente de revisión", "Confirmado", "A verificar", "Desactualizado"];

    // contextoInventario consulta y modifica solo la base documental; cambiarlo selecciona otra sede.
    private readonly InventarioDbContext contextoInventario;

    /// <summary>Prepara el controlador con el contexto del inventario.</summary>
    /// <param name="contexto">Contexto de EF Core; cambiarlo modifica la fuente de equipos y enlaces.</param>
    public EditorTopologiaController(InventarioDbContext contexto)
    {
        contextoInventario = contexto;
    }

    /// <summary>Muestra la página del editor; los datos se cargan después con <see cref="Datos"/>.</summary>
    /// <returns>Vista con el lienzo, los filtros y los paneles.</returns>
    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>
    /// Devuelve equipos con sus puertos, enlaces físicos, relaciones parciales resueltas, posiciones guardadas y alertas.
    /// </summary>
    /// <param name="cancelacion">Señal de cancelación.</param>
    /// <returns>JSON con nodos, aristas y alertas; no modifica nada.</returns>
    [HttpGet]
    public async Task<IActionResult> Datos(CancellationToken cancelacion)
    {
        var equipos = await contextoInventario.Equipos.AsNoTracking().ToListAsync(cancelacion);
        var interfaces = await contextoInventario.InterfacesEquipo.AsNoTracking().ToListAsync(cancelacion);
        var enlaces = await contextoInventario.EnlacesFisicos.AsNoTracking().ToListAsync(cancelacion);
        var relaciones = await contextoInventario.ConexionesDocumentadas.AsNoTracking().ToListAsync(cancelacion);
        var ipsEnConflicto = await contextoInventario.DireccionesIp.AsNoTracking()
            .Where(direccion => direccion.Estado == SincronizadorIps.EnConflicto).ToListAsync(cancelacion);

        var equipoPorId = equipos.ToDictionary(equipo => equipo.Identificador);
        var interfazPorId = interfaces.ToDictionary(interfaz => interfaz.Identificador);
        // puertosEnlazados marca los puertos ocupados por un enlace físico completo.
        var puertosEnlazados = enlaces
            .SelectMany(enlace => new[] { enlace.IdentificadorPuertoOrigen, enlace.IdentificadorPuertoDestino }).ToHashSet();
        var relacionPorPuerto = relaciones.GroupBy(relacion => relacion.IdentificadorPuertoLocal)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First());

        // parciales son las relaciones documentadas cuyo equipo remoto se pudo resolver de forma única.
        var parciales = new List<RelacionParcialResuelta>();
        var remotoPorRelacion = new Dictionary<int, int>();
        foreach (var relacion in relaciones)
        {
            if (!interfazPorId.TryGetValue(relacion.IdentificadorPuertoLocal, out var puertoLocal)
                || !equipoPorId.TryGetValue(puertoLocal.IdentificadorEquipo, out var equipoLocal)) continue;
            var equipoRemoto = TopologiaController.ResolverEquipoRelacionado(equipos, relacion.EquipoRemoto, relacion.IpRemota);
            if (equipoRemoto is null || equipoRemoto.Identificador == equipoLocal.Identificador) continue;
            parciales.Add(new RelacionParcialResuelta(relacion, puertoLocal, equipoLocal, equipoRemoto));
            remotoPorRelacion[relacion.Identificador] = equipoRemoto.Identificador;
        }

        // aristas reúne los enlaces completos y las relaciones parciales (unidas con su recíproca) en una lista común.
        var aristas = new List<AristaMapa>();
        foreach (var enlace in enlaces)
        {
            if (!interfazPorId.TryGetValue(enlace.IdentificadorPuertoOrigen, out var puertoOrigen)
                || !interfazPorId.TryGetValue(enlace.IdentificadorPuertoDestino, out var puertoDestino)) continue;
            aristas.Add(new AristaMapa(
                $"e{enlace.Identificador}", puertoOrigen.IdentificadorEquipo, puertoDestino.IdentificadorEquipo,
                EtiquetaPuerto(equipoPorId, puertoOrigen), EtiquetaPuerto(equipoPorId, puertoDestino), false,
                enlace.Identificador, enlace.Medio, enlace.EstadoDatos, enlace.EstadoStp, enlace.Fuente, puertoOrigen.Identificador, puertoDestino.Identificador));
        }

        foreach (var grupo in parciales.GroupBy(parcial => (
            Menor: Math.Min(parcial.Local.Identificador, parcial.Remoto.Identificador),
            Mayor: Math.Max(parcial.Local.Identificador, parcial.Remoto.Identificador))))
        {
            // Las filas de cada lado se emparejan una a una; si un lado no tiene recíproca queda como arista de una sola punta.
            var ladoMenor = grupo.Where(parcial => parcial.Local.Identificador == grupo.Key.Menor).ToList();
            var ladoMayor = grupo.Where(parcial => parcial.Local.Identificador != grupo.Key.Menor).ToList();
            for (var indice = 0; indice < Math.Max(ladoMenor.Count, ladoMayor.Count); indice++)
            {
                var uno = indice < ladoMenor.Count ? ladoMenor[indice] : null;
                var otro = indice < ladoMayor.Count ? ladoMayor[indice] : null;
                var principal = uno ?? otro!;
                var origen = principal.Local.Identificador;
                var destino = principal.Remoto.Identificador;
                var etiquetaOrigen = EtiquetaPuerto(equipoPorId, principal.Puerto);
                var etiquetaDestino = otro is not null && uno is not null
                    ? EtiquetaPuerto(equipoPorId, otro.Puerto)
                    : principal.Relacion.PuertoRemoto ?? string.Empty;
                aristas.Add(new AristaMapa(
                    $"p{principal.Relacion.Identificador}", origen, destino, etiquetaOrigen, etiquetaDestino, true,
                    null, principal.Relacion.Medio, principal.Relacion.EstadoDatos, "No documentado",
                    principal.Relacion.FuentePagina, principal.Puerto.Identificador, null));
            }
        }

        var nodos = equipos.Select(equipo =>
        {
            var perfil = equipo.TipoDispositivo == "Switch"
                ? CatalogoVisualesDispositivos.ObtenerPerfil(equipo.Fabricante, equipo.Modelo) : null;
            var puertos = interfaces.Where(interfaz => interfaz.IdentificadorEquipo == equipo.Identificador)
                .Select(interfaz =>
                {
                    relacionPorPuerto.TryGetValue(interfaz.Identificador, out var relacion);
                    return new
                    {
                        id = interfaz.Identificador,
                        etiqueta = interfaz.EtiquetaPuerto,
                        etiquetaVisual = CatalogoVisualesDispositivos.NormalizarEtiquetaPuertoVisual(
                            equipo.Nombre, interfaz.EtiquetaPuerto, interfaz.TipoInterfaz),
                        tipo = interfaz.TipoInterfaz,
                        velocidadMbps = interfaz.VelocidadMbps,
                        enlazado = puertosEnlazados.Contains(interfaz.Identificador),
                        relacionParcial = relacion is null ? null : new
                        {
                            id = relacion.Identificador,
                            destino = $"{relacion.EquipoRemoto} {relacion.IpRemota}".Trim(),
                            equipoId = remotoPorRelacion.TryGetValue(relacion.Identificador, out var remoto) ? remoto : (int?)null
                        }
                    };
                }).ToList();
            // notasSinEquipo cuenta relaciones documentadas de este equipo cuyo destino no se pudo asociar a una ficha.
            var notasSinEquipo = relaciones.Count(relacion =>
                interfazPorId.TryGetValue(relacion.IdentificadorPuertoLocal, out var puerto)
                && puerto.IdentificadorEquipo == equipo.Identificador
                && !remotoPorRelacion.ContainsKey(relacion.Identificador));
            return new
            {
                id = equipo.Identificador,
                nombre = equipo.Nombre,
                tipo = equipo.TipoDispositivo,
                ip = equipo.IpAdministracion,
                area = equipo.Area,
                administrable = equipo.EsAdministrable,
                icono = CatalogoVisualesDispositivos.ObtenerIcono(equipo.TipoDispositivo, perfil),
                notasSinEquipo,
                puertos
            };
        }).ToList();

        return Json(new
        {
            nodos,
            aristas = aristas.Select(arista => new
            {
                id = arista.Id,
                source = arista.Origen,
                target = arista.Destino,
                etiquetaOrigen = arista.EtiquetaOrigen,
                etiquetaDestino = arista.EtiquetaDestino,
                parcial = arista.Parcial,
                enlaceId = arista.EnlaceId,
                medio = arista.Medio,
                estadoDatos = arista.EstadoDatos,
                estadoStp = arista.EstadoStp,
                fuente = arista.Fuente
            }),
            alertas = CalcularAlertas(aristas, equipoPorId, ipsEnConflicto)
        });
    }

    /// <summary>
    /// Devuelve las posiciones guardadas de una vista del mapa (switches, todo o foco-IDENTIFICADOR del switch).
    /// </summary>
    /// <param name="vista">Nombre de la vista; vacío equivale a general.</param>
    /// <param name="cancelacion">Señal de cancelación.</param>
    /// <returns>JSON con un objeto identificador de equipo a coordenadas x, y; 400 si el nombre de vista no es válido.</returns>
    [HttpGet]
    public async Task<IActionResult> Posiciones(string? vista, CancellationToken cancelacion)
    {
        var nombreVista = NormalizarVista(vista);
        if (nombreVista is null) return BadRequest(new { errores = new[] { "La vista indicada no es válida." } });
        var guardadas = await contextoInventario.PosicionesNodoMapa.AsNoTracking()
            .Where(posicion => posicion.Vista == nombreVista).ToListAsync(cancelacion);
        return Json(guardadas.ToDictionary(posicion => posicion.IdentificadorEquipo.ToString(CultureInfo.InvariantCulture),
            posicion => new { x = posicion.X, y = posicion.Y }));
    }

    /// <summary>
    /// Valida una conexión propuesta antes de guardarla, con las mismas reglas que al crearla.
    /// </summary>
    /// <param name="equipoA">Equipo de origen.</param>
    /// <param name="puertoA">Puerto de origen; 0 si el equipo no tiene interfaces y se creará una.</param>
    /// <param name="equipoB">Equipo de destino.</param>
    /// <param name="puertoB">Puerto de destino; 0 si el equipo no tiene interfaces y se creará una.</param>
    /// <param name="cancelacion">Señal de cancelación.</param>
    /// <returns>JSON con la lista de hallazgos (error, aviso o información); no guarda nada.</returns>
    [HttpGet]
    public async Task<IActionResult> Validar(int equipoA, int puertoA, int equipoB, int puertoB, CancellationToken cancelacion)
    {
        var validacion = await ValidarSolicitudAsync(equipoA, puertoA, equipoB, puertoB, cancelacion);
        return Json(validacion.Resultado.Hallazgos.Select(hallazgo => new
        {
            severidad = hallazgo.Severidad.ToString().ToLowerInvariant(),
            codigo = hallazgo.Codigo,
            mensaje = hallazgo.Mensaje
        }));
    }

    /// <summary>
    /// Crea un enlace físico. Si un equipo no tiene interfaces se crea una «Red 1», y las relaciones parciales entre
    /// los mismos equipos que ocupaban los puertos elegidos se convierten en el enlace completo.
    /// </summary>
    /// <param name="equipoA">Equipo de origen.</param>
    /// <param name="puertoA">Puerto de origen; 0 para crear una interfaz.</param>
    /// <param name="equipoB">Equipo de destino.</param>
    /// <param name="puertoB">Puerto de destino; 0 para crear una interfaz.</param>
    /// <param name="medio">Medio del cable; solo valores permitidos.</param>
    /// <param name="estadoDatos">Estado de confirmación de los datos del enlace.</param>
    /// <param name="estadoStp">Estado STP o RSTP del enlace; documenta la topología activa.</param>
    /// <param name="cancelacion">Señal de cancelación.</param>
    /// <returns>JSON con el identificador del enlace y los avisos, o los errores con código 400.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearEnlace(
        int equipoA, int puertoA, int equipoB, int puertoB, string? medio, string? estadoDatos, string? estadoStp,
        CancellationToken cancelacion)
    {
        var medioFinal = string.IsNullOrWhiteSpace(medio) ? "No confirmado" : medio.Trim();
        var estadoDatosFinal = string.IsNullOrWhiteSpace(estadoDatos) ? "Pendiente de revisión" : estadoDatos.Trim();
        var estadoStpFinal = string.IsNullOrWhiteSpace(estadoStp) ? "No documentado" : estadoStp.Trim();
        if (!MediosPermitidos.Contains(medioFinal) || !EstadosDatosPermitidos.Contains(estadoDatosFinal)
            || !OpcionesTopologia.EstadosStp.Contains(estadoStpFinal))
        {
            return BadRequest(new { errores = new[] { "El medio, el estado de los datos o el estado STP no son válidos." } });
        }

        var validacion = await ValidarSolicitudAsync(equipoA, puertoA, equipoB, puertoB, cancelacion);
        if (validacion.Resultado.TieneErrores)
        {
            return BadRequest(new { errores = validacion.Resultado.Errores.Select(error => error.Mensaje) });
        }

        var origen = validacion.EquipoA!;
        var destino = validacion.EquipoB!;
        var usuario = ObtenerUsuario();
        var eventos = new List<EventoModificacion>();
        await using var transaccion = await contextoInventario.Database.BeginTransactionAsync(cancelacion);

        // Cada extremo usa el puerto elegido o una interfaz de red nueva si el equipo no tenía ninguna.
        var interfazOrigen = await ObtenerOCrearPuertoAsync(origen, puertoA, usuario, eventos, cancelacion);
        var interfazDestino = await ObtenerOCrearPuertoAsync(destino, puertoB, usuario, eventos, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);

        // La relación parcial que ocupaba el puerto queda reemplazada por el enlace completo.
        foreach (var relacion in validacion.RelacionesAConvertir)
        {
            var equipoRelacion = relacion.IdentificadorPuertoLocal == interfazOrigen.Identificador ? origen : destino;
            UtilidadesConexiones.AgregarCambio(eventos, equipoRelacion.Identificador, equipoRelacion.Nombre, usuario,
                "Relación parcial convertida en enlace completo",
                $"{relacion.EquipoRemoto} {relacion.IpRemota}".Trim(), $"{origen.Nombre} ↔ {destino.Nombre}");
        }

        contextoInventario.ConexionesDocumentadas.RemoveRange(validacion.RelacionesAConvertir);

        // Un puerto que figuraba como libre confirmado deja de estarlo al conectarse; el historial conserva el cambio.
        foreach (var (puerto, equipo) in new[] { (interfazOrigen, origen), (interfazDestino, destino) })
        {
            if (puerto.EstadoUso != "Libre confirmado") continue;
            puerto.EstadoUso = "Desconocido";
            UtilidadesConexiones.AgregarCambio(eventos, equipo.Identificador, equipo.Nombre, usuario,
                $"Estado de uso del puerto {puerto.EtiquetaPuerto}", "Libre confirmado", "Desconocido");
        }

        var enlace = new EnlaceFisico
        {
            IdentificadorPuertoOrigen = interfazOrigen.Identificador,
            IdentificadorPuertoDestino = interfazDestino.Identificador,
            Medio = medioFinal,
            EstadoDatos = estadoDatosFinal,
            EstadoStp = estadoStpFinal,
            Fuente = "Editor del mapa"
        };
        await contextoInventario.EnlacesFisicos.AddAsync(enlace, cancelacion);
        UtilidadesConexiones.AgregarCambio(eventos, origen.Identificador, origen.Nombre, usuario, "Enlace físico agregado", null,
            $"{origen.Nombre} · {interfazOrigen.EtiquetaPuerto} → {destino.Nombre} · {interfazDestino.EtiquetaPuerto} · {medioFinal} · {estadoDatosFinal}");

        await contextoInventario.EventosModificacion.AddRangeAsync(eventos, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return Json(new { ok = true, enlaceId = enlace.Identificador, avisos = validacion.Resultado.Avisos.Select(aviso => aviso.Mensaje) });
    }

    /// <summary>Elimina un enlace físico y lo deja registrado en el historial; los puertos quedan sin enlace.</summary>
    /// <param name="id">Identificador del enlace.</param>
    /// <param name="cancelacion">Señal de cancelación.</param>
    /// <returns>JSON de confirmación, o 404 si el enlace ya no existe.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarEnlace(int id, CancellationToken cancelacion)
    {
        var enlace = await contextoInventario.EnlacesFisicos.FirstOrDefaultAsync(e => e.Identificador == id, cancelacion);
        if (enlace is null) return NotFound(new { errores = new[] { "El enlace ya no existe." } });

        var extremos = await ObtenerExtremosAsync(enlace, cancelacion);
        contextoInventario.EnlacesFisicos.Remove(enlace);
        var eventos = new List<EventoModificacion>();
        UtilidadesConexiones.AgregarCambio(eventos, extremos.IdEquipoOrigen, extremos.NombreOrigen, ObtenerUsuario(),
            "Enlace físico eliminado", $"{extremos.Descripcion} · {enlace.Medio}", null);
        await contextoInventario.EventosModificacion.AddRangeAsync(eventos, cancelacion);
        await contextoInventario.SaveChangesAsync(cancelacion);
        return Json(new { ok = true });
    }

    /// <summary>
    /// Actualiza el medio, el estado de los datos y el estado STP o RSTP de un enlace; cada campo cambiado queda en el historial.
    /// </summary>
    /// <param name="id">Identificador del enlace.</param>
    /// <param name="medio">Uno de los medios permitidos.</param>
    /// <param name="estadoDatos">Uno de los estados de confirmación permitidos.</param>
    /// <param name="estadoStp">Uno de los estados STP permitidos; documenta qué camino queda activo.</param>
    /// <param name="cancelacion">Señal de cancelación.</param>
    /// <returns>JSON de confirmación, 400 si algún valor no es válido o 404 si el enlace no existe.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarEnlace(int id, string medio, string estadoDatos, string estadoStp, CancellationToken cancelacion)
    {
        if (!MediosPermitidos.Contains(medio) || !EstadosDatosPermitidos.Contains(estadoDatos) || !OpcionesTopologia.EstadosStp.Contains(estadoStp))
            return BadRequest(new { errores = new[] { "El medio, el estado de los datos o el estado STP no son válidos." } });
        var enlace = await contextoInventario.EnlacesFisicos.FirstOrDefaultAsync(e => e.Identificador == id, cancelacion);
        if (enlace is null) return NotFound(new { errores = new[] { "El enlace ya no existe." } });

        var extremos = await ObtenerExtremosAsync(enlace, cancelacion);
        var usuario = ObtenerUsuario();
        var eventos = new List<EventoModificacion>();
        // Cada campo se compara contra el valor guardado; solo los que cambian generan un evento.
        void Registrar(string campo, string anterior, string nuevo)
        {
            if (anterior == nuevo) return;
            UtilidadesConexiones.AgregarCambio(eventos, extremos.IdEquipoOrigen, extremos.NombreOrigen, usuario, campo,
                $"{extremos.Descripcion}: {anterior}", $"{extremos.Descripcion}: {nuevo}");
        }

        Registrar("Medio del enlace", enlace.Medio, medio);
        Registrar("Estado de los datos del enlace", enlace.EstadoDatos, estadoDatos);
        Registrar("Estado STP del enlace", enlace.EstadoStp, estadoStp);
        enlace.Medio = medio;
        enlace.EstadoDatos = estadoDatos;
        enlace.EstadoStp = estadoStp;
        if (eventos.Count > 0)
        {
            await contextoInventario.EventosModificacion.AddRangeAsync(eventos, cancelacion);
        }

        await contextoInventario.SaveChangesAsync(cancelacion);
        return Json(new { ok = true, cambios = eventos.Count });
    }

    /// <summary>Guarda la posición de los equipos movidos en el lienzo, compartida entre técnicos.</summary>
    /// <param name="posiciones">JSON con una lista de objetos id, x, y; se ignoran equipos inexistentes y valores no numéricos.</param>
    /// <param name="vista">Vista a la que pertenecen (switches, todo o foco-IDENTIFICADOR); vacío equivale a general.</param>
    /// <param name="cancelacion">Señal de cancelación.</param>
    /// <returns>JSON con la cantidad de posiciones guardadas.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarPosiciones(string posiciones, string? vista, CancellationToken cancelacion)
    {
        var nombreVista = NormalizarVista(vista);
        if (nombreVista is null) return BadRequest(new { errores = new[] { "La vista indicada no es válida." } });

        List<PosicionRecibida>? recibidas;
        try
        {
            recibidas = JsonSerializer.Deserialize<List<PosicionRecibida>>(posiciones ?? string.Empty,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return BadRequest(new { errores = new[] { "Las posiciones no tienen el formato esperado." } });
        }

        if (recibidas is null || recibidas.Count == 0) return Json(new { ok = true, guardadas = 0 });
        if (recibidas.Count > 3000) return BadRequest(new { errores = new[] { "Demasiadas posiciones en una sola solicitud." } });

        // validas descarta coordenadas que no son números finitos y duplicados del mismo equipo.
        var validas = recibidas.Where(p => double.IsFinite(p.X) && double.IsFinite(p.Y))
            .GroupBy(p => p.Id).Select(grupo => grupo.Last()).ToList();
        var identificadores = validas.Select(p => p.Id).ToList();
        var equiposExistentes = (await contextoInventario.Equipos.AsNoTracking()
            .Where(equipo => identificadores.Contains(equipo.Identificador))
            .Select(equipo => equipo.Identificador).ToListAsync(cancelacion)).ToHashSet();
        var guardadas = await contextoInventario.PosicionesNodoMapa
            .Where(posicion => posicion.Vista == nombreVista && identificadores.Contains(posicion.IdentificadorEquipo))
            .ToDictionaryAsync(posicion => posicion.IdentificadorEquipo, cancelacion);

        var ahora = DateTime.UtcNow;
        var usuario = ObtenerUsuario();
        var total = 0;
        foreach (var recibida in validas.Where(p => equiposExistentes.Contains(p.Id)))
        {
            if (!guardadas.TryGetValue(recibida.Id, out var posicion))
            {
                posicion = new PosicionNodoMapa { IdentificadorEquipo = recibida.Id, Vista = nombreVista };
                await contextoInventario.PosicionesNodoMapa.AddAsync(posicion, cancelacion);
            }

            posicion.X = recibida.X;
            posicion.Y = recibida.Y;
            posicion.FechaActualizacionUtc = ahora;
            posicion.NombreUsuario = usuario;
            total++;
        }

        await contextoInventario.SaveChangesAsync(cancelacion);
        return Json(new { ok = true, guardadas = total });
    }

    /// <summary>
    /// Valida una conexión entre dos equipos y determina qué relaciones parciales se convertirán en enlace completo.
    /// </summary>
    private async Task<ValidacionSolicitud> ValidarSolicitudAsync(
        int equipoA, int puertoA, int equipoB, int puertoB, CancellationToken cancelacion)
    {
        var resultado = new ResultadoValidacion();
        var equipos = await contextoInventario.Equipos.AsNoTracking().ToListAsync(cancelacion);
        var origen = equipos.FirstOrDefault(equipo => equipo.Identificador == equipoA);
        var destino = equipos.FirstOrDefault(equipo => equipo.Identificador == equipoB);
        if (origen is null || destino is null)
        {
            resultado.Agregar(SeveridadValidacion.Error, "EQUIPO_INEXISTENTE", "Selecciona dos equipos existentes.");
            return new ValidacionSolicitud(resultado, [], null, null);
        }

        if (equipoA == equipoB)
        {
            resultado.Agregar(SeveridadValidacion.Error, "MISMO_EQUIPO", "Un equipo no se puede conectar consigo mismo.");
            return new ValidacionSolicitud(resultado, [], origen, destino);
        }

        // Cada extremo debe tener un puerto elegido, salvo que el equipo no tenga interfaces (se creará una).
        var puertosElegidos = new List<InterfazEquipo>();
        foreach (var (equipo, puertoId) in new[] { (origen, puertoA), (destino, puertoB) })
        {
            if (puertoId > 0)
            {
                var puerto = await contextoInventario.InterfacesEquipo.AsNoTracking()
                    .FirstOrDefaultAsync(interfaz => interfaz.Identificador == puertoId, cancelacion);
                if (puerto is null || puerto.IdentificadorEquipo != equipo.Identificador)
                {
                    resultado.Agregar(SeveridadValidacion.Error, "PUERTO_AJENO",
                        $"El puerto elegido no pertenece a {equipo.Nombre}.");
                    continue;
                }

                puertosElegidos.Add(puerto);
            }
            else if (await contextoInventario.InterfacesEquipo.AnyAsync(interfaz => interfaz.IdentificadorEquipo == equipo.Identificador, cancelacion))
            {
                resultado.Agregar(SeveridadValidacion.Error, "PUERTO_REQUERIDO", $"Elige el puerto de {equipo.Nombre}.");
            }
        }

        if (resultado.TieneErrores) return new ValidacionSolicitud(resultado, [], origen, destino);

        ResultadoValidacion tecnico;
        if (puertoA > 0 && puertoB > 0)
        {
            tecnico = await ValidadorTopologia.ValidarEnlaceAsync(contextoInventario, puertoA, puertoB, null, cancelacion);
        }
        else
        {
            // Con un extremo sin interfaz solo se pueden comprobar el equipo y la ocupación del puerto que sí existe.
            tecnico = await ValidadorTopologia.ValidarEquiposAsync(contextoInventario, equipoA, equipoB, null, cancelacion);
            foreach (var puerto in puertosElegidos)
            {
                if (await contextoInventario.EnlacesFisicos.AnyAsync(enlace =>
                        enlace.IdentificadorPuertoOrigen == puerto.Identificador || enlace.IdentificadorPuertoDestino == puerto.Identificador, cancelacion))
                {
                    tecnico.Agregar(SeveridadValidacion.Error, "PUERTO_OCUPADO",
                        $"El puerto {puerto.EtiquetaPuerto} ya tiene un enlace. Un puerto no puede conectarse a dos equipos a la vez, salvo mediante un switch no administrable intermedio.");
                }
            }
        }

        // Una relación parcial se convierte solo si su destino resuelto es el otro equipo de este enlace.
        var identificadoresPuertos = puertosElegidos.Select(puerto => puerto.Identificador).ToList();
        var relacionesEnPuertos = await contextoInventario.ConexionesDocumentadas
            .Where(relacion => identificadoresPuertos.Contains(relacion.IdentificadorPuertoLocal)).ToListAsync(cancelacion);
        var convertibles = relacionesEnPuertos.Where(relacion =>
        {
            var duenoPuerto = puertosElegidos.First(puerto => puerto.Identificador == relacion.IdentificadorPuertoLocal).IdentificadorEquipo;
            var otroEquipo = duenoPuerto == origen.Identificador ? destino : origen;
            return TopologiaController.ResolverEquipoRelacionado(equipos, relacion.EquipoRemoto, relacion.IpRemota)?.Identificador
                == otroEquipo.Identificador;
        }).ToList();
        var todasConvertibles = relacionesEnPuertos.Count > 0 && convertibles.Count == relacionesEnPuertos.Count;

        foreach (var hallazgo in tecnico.Hallazgos)
        {
            if (hallazgo.Codigo == "RELACION_PARCIAL" && todasConvertibles) continue;
            resultado.Agregar(hallazgo.Severidad, hallazgo.Codigo, hallazgo.Mensaje);
        }

        if (todasConvertibles)
        {
            resultado.Agregar(SeveridadValidacion.Informacion, "CONVERSION_PARCIAL",
                "El puerto ya estaba documentado hacia este equipo con una relación parcial; al guardar se convertirá en un enlace completo.");
        }

        return new ValidacionSolicitud(resultado, todasConvertibles ? convertibles : [], origen, destino);
    }

    /// <summary>Devuelve el puerto elegido o crea la interfaz «Red 1» en un equipo que no tiene ninguna.</summary>
    private async Task<InterfazEquipo> ObtenerOCrearPuertoAsync(
        EquipoRed equipo, int puertoId, string usuario, List<EventoModificacion> eventos, CancellationToken cancelacion)
    {
        if (puertoId > 0)
        {
            return await contextoInventario.InterfacesEquipo.FirstAsync(interfaz => interfaz.Identificador == puertoId, cancelacion);
        }

        // interfazNueva no afirma el tipo de conector: el técnico solo dibujó la conexión.
        var interfazNueva = new InterfazEquipo
        {
            IdentificadorEquipo = equipo.Identificador,
            EtiquetaPuerto = "Red 1",
            TipoInterfaz = "No confirmado",
            EstadoDatos = "A verificar",
            EstadoUso = "Desconocido",
            Observaciones = "Interfaz de red creada desde el editor del mapa."
        };
        await contextoInventario.InterfacesEquipo.AddAsync(interfazNueva, cancelacion);
        UtilidadesConexiones.AgregarCambio(eventos, equipo.Identificador, equipo.Nombre, usuario,
            "Interfaz de red creada desde el editor del mapa", null, interfazNueva.EtiquetaPuerto);
        return interfazNueva;
    }

    /// <summary>Obtiene nombres y puertos de los extremos de un enlace para describirlo en el historial.</summary>
    private async Task<ExtremosEnlace> ObtenerExtremosAsync(EnlaceFisico enlace, CancellationToken cancelacion)
    {
        var puertos = await contextoInventario.InterfacesEquipo.AsNoTracking()
            .Where(interfaz => interfaz.Identificador == enlace.IdentificadorPuertoOrigen
                || interfaz.Identificador == enlace.IdentificadorPuertoDestino).ToListAsync(cancelacion);
        var identificadoresEquipo = puertos.Select(puerto => puerto.IdentificadorEquipo).Distinct().ToList();
        var equipos = await contextoInventario.Equipos.AsNoTracking()
            .Where(equipo => identificadoresEquipo.Contains(equipo.Identificador)).ToListAsync(cancelacion);
        var puertoOrigen = puertos.FirstOrDefault(puerto => puerto.Identificador == enlace.IdentificadorPuertoOrigen);
        var puertoDestino = puertos.FirstOrDefault(puerto => puerto.Identificador == enlace.IdentificadorPuertoDestino);
        var equipoOrigen = equipos.FirstOrDefault(equipo => equipo.Identificador == puertoOrigen?.IdentificadorEquipo);
        var equipoDestino = equipos.FirstOrDefault(equipo => equipo.Identificador == puertoDestino?.IdentificadorEquipo);
        return new ExtremosEnlace(
            equipoOrigen?.Identificador ?? 0, equipoOrigen?.Nombre ?? "Equipo desconocido",
            $"{equipoOrigen?.Nombre} · {puertoOrigen?.EtiquetaPuerto} ↔ {equipoDestino?.Nombre} · {puertoDestino?.EtiquetaPuerto}");
    }

    /// <summary>
    /// Detecta enlaces que cierran un ciclo y direcciones IP en conflicto. Solo informa: con STP o RSTP un ciclo es normal
    /// y lo que se pide es documentar la topología activa.
    /// </summary>
    private static List<object> CalcularAlertas(
        List<AristaMapa> aristas, Dictionary<int, EquipoRed> equipoPorId, List<DireccionIp> ipsEnConflicto)
    {
        var alertas = new List<object>();
        // padre implementa una estructura de conjuntos disjuntos: dos equipos del mismo conjunto ya están conectados.
        var padre = new Dictionary<int, int>();
        int Raiz(int nodo)
        {
            if (!padre.TryGetValue(nodo, out var actual)) { padre[nodo] = nodo; return nodo; }
            while (padre[nodo] != nodo) { padre[nodo] = padre[padre[nodo]]; nodo = padre[nodo]; }
            return nodo;
        }

        // Los enlaces completos se procesan primero; entre switches también cuentan las relaciones parciales.
        foreach (var arista in aristas.OrderBy(a => a.Parcial).ThenBy(a => a.EnlaceId ?? int.MaxValue))
        {
            if (arista.Parcial && !(equipoPorId.TryGetValue(arista.Origen, out var origen) && origen.TipoDispositivo == "Switch"
                && equipoPorId.TryGetValue(arista.Destino, out var destino) && destino.TipoDispositivo == "Switch")) continue;

            var raizOrigen = Raiz(arista.Origen);
            var raizDestino = Raiz(arista.Destino);
            if (raizOrigen != raizDestino)
            {
                padre[raizOrigen] = raizDestino;
                continue;
            }

            var nombreOrigen = equipoPorId.TryGetValue(arista.Origen, out var equipoOrigen) ? equipoOrigen.Nombre : "?";
            var nombreDestino = equipoPorId.TryGetValue(arista.Destino, out var equipoDestino) ? equipoDestino.Nombre : "?";
            var documentado = !arista.Parcial && arista.EstadoStp != "No documentado";
            alertas.Add(new
            {
                tipo = "ciclo",
                aristaId = arista.Id,
                documentado,
                texto = documentado
                    ? $"{nombreOrigen} ↔ {nombreDestino} cierra un ciclo y su estado STP está documentado ({arista.EstadoStp})."
                    : $"{nombreOrigen} ↔ {nombreDestino} cierra un ciclo. Con STP o RSTP es normal, pero documenta qué camino queda activo y cuál bloqueado."
                        + (arista.Parcial ? " Es una relación parcial: conviértela en enlace completo para registrar el estado STP." : string.Empty)
            });
        }

        foreach (var direccion in ipsEnConflicto)
        {
            alertas.Add(new { tipo = "ip", aristaId = (string?)null, documentado = false, texto = $"{direccion.Direccion}: {direccion.Observaciones}" });
        }

        return alertas;
    }

    /// <summary>Etiqueta de un puerto con la numeración visual (por ejemplo SFP 28 se muestra como SFP 4).</summary>
    private static string EtiquetaPuerto(Dictionary<int, EquipoRed> equipoPorId, InterfazEquipo interfaz) =>
        CatalogoVisualesDispositivos.NormalizarEtiquetaPuertoVisual(
            equipoPorId.TryGetValue(interfaz.IdentificadorEquipo, out var equipo) ? equipo.Nombre : string.Empty,
            interfaz.EtiquetaPuerto, interfaz.TipoInterfaz);

    /// <summary>Valida el nombre de una vista del mapa; devuelve nulo si no es general, switches, todo ni foco-número.</summary>
    private static string? NormalizarVista(string? vista)
    {
        var nombre = string.IsNullOrWhiteSpace(vista) ? VistaGeneral : vista.Trim();
        return Regex.IsMatch(nombre, @"^(general|switches|todo|foco-\d{1,9})$") ? nombre : null;
    }

    /// <summary>Nombre del usuario que edita, o la marca de uso sin sesión mientras el login esté desactivado.</summary>
    private string ObtenerUsuario() => User.Identity?.Name ?? "Pruebas sin sesión";

    // Registros internos que agrupan datos para armar la respuesta y las validaciones.
    private sealed record RelacionParcialResuelta(ConexionDocumentada Relacion, InterfazEquipo Puerto, EquipoRed Local, EquipoRed Remoto);

    private sealed record AristaMapa(
        string Id, int Origen, int Destino, string EtiquetaOrigen, string EtiquetaDestino, bool Parcial, int? EnlaceId,
        string Medio, string EstadoDatos, string EstadoStp, string? Fuente, int PuertoOrigenId, int? PuertoDestinoId);

    private sealed record ValidacionSolicitud(
        ResultadoValidacion Resultado, IReadOnlyList<ConexionDocumentada> RelacionesAConvertir, EquipoRed? EquipoA, EquipoRed? EquipoB);

    private sealed record ExtremosEnlace(int IdEquipoOrigen, string NombreOrigen, string Descripcion);

    /// <summary>Posición recibida desde el navegador; los nombres coinciden con el JSON enviado por el script del mapa.</summary>
    private sealed class PosicionRecibida
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }
}
