// Activa búsqueda, estado y paginación local sobre las fichas ya cargadas desde SQLite.
document.addEventListener("DOMContentLoaded", () => {
    // gruposPaginables contiene cada lista con sus controles y estado independiente.
    const gruposPaginables = document.querySelectorAll("[data-paginador]");

    // grupoPaginable configura la interacción de una sección sin afectar las demás.
    for (const grupoPaginable of gruposPaginables) {
        configurarPaginador(grupoPaginable);
    }
});

/**
 * Conecta búsqueda, filtro de estado y selector de cantidad de una lista.
 * @param {HTMLElement} grupoPaginable Sección con fichas; cambiarla selecciona otra colección.
 */
function configurarPaginador(grupoPaginable) {
    // selectorTamano define cuántas coincidencias se ven; cero muestra todas.
    const selectorTamano = grupoPaginable.querySelector("[data-tamano-pagina]");
    // resumenPagina anuncia el rango filtrado que está visible.
    const resumenPagina = grupoPaginable.querySelector("[data-resumen-pagina]");
    // botonAnterior avanza hacia un bloque previo de resultados.
    const botonAnterior = grupoPaginable.querySelector("[data-pagina-anterior]");
    // botonSiguiente avanza hacia un bloque posterior de resultados.
    const botonSiguiente = grupoPaginable.querySelector("[data-pagina-siguiente]");
    // listaPaginada contiene las fichas que la búsqueda y la paginación mostrarán.
    const listaPaginada = grupoPaginable.querySelector("[data-lista-paginada]");
    // campoBusqueda filtra equipo, IP, puerto y texto de la ficha sin alterar SQLite.
    const campoBusqueda = grupoPaginable.querySelector("[data-busqueda-lista]");
    // filtroEstado limita la lista a un estado elegido por el técnico.
    const filtroEstado = grupoPaginable.querySelector("[data-filtro-estado]");

    if (!selectorTamano || !resumenPagina || !botonAnterior || !botonSiguiente || !listaPaginada) {
        return;
    }

    // avisoSinResultados indica cuándo los filtros no encuentran fichas.
    let avisoSinResultados = grupoPaginable.querySelector("[data-sin-resultados]");
    if (!avisoSinResultados) {
        // avisoNuevo crea el estado vacío sin modificar la fuente de datos.
        const avisoNuevo = document.createElement("p");
        avisoNuevo.className = "sin-resultados-conexion";
        avisoNuevo.dataset.sinResultados = "";
        avisoNuevo.hidden = true;
        avisoNuevo.textContent = "No hay conexiones que coincidan con estos filtros.";
        grupoPaginable.append(avisoNuevo);
        avisoSinResultados = avisoNuevo;
    }

    // paginaActual comienza en uno y se reinicia cuando cambian los filtros.
    let paginaActual = 1;

    /**
     * Normaliza tildes y mayúsculas para que las búsquedas sean más permisivas.
     * @param {string} texto Texto original; cambiarlo cambia la cadena que se compara.
     * @returns {string} Texto normalizado para búsquedas.
     */
    function normalizarTexto(texto) {
        return texto.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("es");
    }

    /**
     * Recalcula coincidencias, rango visible y controles de navegación.
     */
    function actualizarPagina() {
        // registrosPaginables contiene todas las fichas disponibles en esta sección.
        const registrosPaginables = Array.from(listaPaginada.querySelectorAll("[data-registro-paginable]"));
        // textoBuscado compara contra el texto completo de cada ficha sin diferenciar tildes.
        const textoBuscado = normalizarTexto(campoBusqueda?.value.trim() || "");
        // estadoBuscado contiene el filtro seleccionado o vacío para mostrar todos los estados.
        const estadoBuscado = normalizarTexto(filtroEstado?.value || "");
        // registrosCoincidentes conserva solo las fichas que cumplen ambos filtros.
        const registrosCoincidentes = registrosPaginables.filter(registro => {
            // textoRegistro resume equipo, IP, puerto y notas visibles de la ficha.
            const textoRegistro = normalizarTexto(registro.textContent || "");
            // estadoRegistro reúne vigencia y uso documentados en los atributos de la ficha.
            const estadoRegistro = normalizarTexto(registro.dataset.estado || "");
            // coincideTexto exige que la ficha incluya lo escrito en el cuadro de búsqueda.
            const coincideTexto = !textoBuscado || textoRegistro.includes(textoBuscado);
            // coincideEstado compara el estado elegido con las etiquetas documentadas.
            const coincideEstado = !estadoBuscado || estadoRegistro.includes(estadoBuscado);
            return coincideTexto && coincideEstado;
        });
        // tamanoElegido controla el tamaño visible; cero significa toda la colección filtrada.
        const tamanoElegido = Number(selectorTamano.value);
        // tamanoPagina se mantiene mayor que cero incluso con una lista vacía.
        const tamanoPagina = tamanoElegido === 0 ? Math.max(registrosCoincidentes.length, 1) : tamanoElegido;
        // totalPaginas determina el límite de navegación para el resultado actual.
        const totalPaginas = Math.max(1, Math.ceil(registrosCoincidentes.length / tamanoPagina));

        paginaActual = Math.min(paginaActual, totalPaginas);

        // indiceInicial marca la primera ficha del bloque actual.
        const indiceInicial = (paginaActual - 1) * tamanoPagina;
        // indiceFinal marca el límite exclusivo del bloque actual.
        const indiceFinal = Math.min(indiceInicial + tamanoPagina, registrosCoincidentes.length);
        // registrosVisibles contiene solo la página seleccionada entre los resultados filtrados.
        const registrosVisibles = new Set(registrosCoincidentes.slice(indiceInicial, indiceFinal));

        registrosPaginables.forEach(registroPaginable => {
            // debeMostrarse indica si el registro coincide y cae dentro de la página activa.
            const debeMostrarse = registrosVisibles.has(registroPaginable);
            registroPaginable.hidden = !debeMostrarse;
            registroPaginable.classList.remove("entrada-suave-conexion");

            if (debeMostrarse) {
                // Reiniciar la clase reproduce la transición al cambiar de búsqueda o página.
                void registroPaginable.offsetWidth;
                registroPaginable.classList.add("entrada-suave-conexion");
            }
        });

        // primerRegistro y ultimoRegistro delimitan el tramo visible de coincidencias.
        const primerRegistro = registrosCoincidentes.length === 0 ? 0 : indiceInicial + 1;
        const ultimoRegistro = indiceFinal;
        resumenPagina.textContent = registrosCoincidentes.length === 0
            ? "Sin coincidencias"
            : `Mostrando ${primerRegistro}–${ultimoRegistro} de ${registrosCoincidentes.length}`;
        avisoSinResultados.hidden = registrosCoincidentes.length > 0;
        botonAnterior.disabled = paginaActual <= 1;
        botonSiguiente.disabled = paginaActual >= totalPaginas || tamanoElegido === 0;
    }

    // cambioTamano reinicia la página para evitar un rango que ya no existe.
    selectorTamano.addEventListener("change", () => {
        paginaActual = 1;
        actualizarPagina();
    });

    // cambioBusqueda recalcula coincidencias mientras se escribe.
    campoBusqueda?.addEventListener("input", () => {
        paginaActual = 1;
        actualizarPagina();
    });

    // cambioEstado recalcula la colección visible usando el estado elegido.
    filtroEstado?.addEventListener("change", () => {
        paginaActual = 1;
        actualizarPagina();
    });

    // paginaSiguiente avanza un bloque de registros coincidentes.
    botonSiguiente.addEventListener("click", () => {
        paginaActual += 1;
        actualizarPagina();
    });

    // paginaAnterior retrocede un bloque de registros coincidentes.
    botonAnterior.addEventListener("click", () => {
        paginaActual -= 1;
        actualizarPagina();
    });

    actualizarPagina();
}
