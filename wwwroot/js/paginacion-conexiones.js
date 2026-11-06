// Activa páginas locales para las fichas de puertos y enlaces ya cargadas desde el servidor.
document.addEventListener("DOMContentLoaded", () => {
    // gruposPaginables contiene cada lista con sus propios controles y estado.
    const gruposPaginables = document.querySelectorAll("[data-paginador]");

    // grupoPaginable mantiene el control independiente de cada colección.
    for (const grupoPaginable of gruposPaginables) {
        configurarPaginador(grupoPaginable);
    }
});

/**
 * Conecta el selector de cantidad y las flechas de un listado.
 * @param {HTMLElement} grupoPaginable Grupo que contiene fichas y controles; cambiarlo selecciona otra lista.
 */
function configurarPaginador(grupoPaginable) {
    // selectorTamano determina cuántos elementos se muestran; cambiarlo ajusta el tamaño de página.
    const selectorTamano = grupoPaginable.querySelector("[data-tamano-pagina]");
    // resumenPagina anuncia el rango visible; cambiarlo modifica el resumen accesible.
    const resumenPagina = grupoPaginable.querySelector("[data-resumen-pagina]");
    // botonAnterior retrocede; cambiarlo reemplaza el control para la página previa.
    const botonAnterior = grupoPaginable.querySelector("[data-pagina-anterior]");
    // botonSiguiente avanza; cambiarlo reemplaza el control para la página siguiente.
    const botonSiguiente = grupoPaginable.querySelector("[data-pagina-siguiente]");
    // listaPaginada contiene las fichas; cambiarla selecciona otra colección.
    const listaPaginada = grupoPaginable.querySelector("[data-lista-paginada]");

    if (!selectorTamano || !resumenPagina || !botonAnterior || !botonSiguiente || !listaPaginada) {
        return;
    }

    // paginaActual comienza en uno; cambiarla muestra otro bloque de registros.
    let paginaActual = 1;

    /**
     * Actualiza la visibilidad, los botones y el rango anunciado.
     * No recibe parámetros; usa el selector, la colección y paginaActual.
     */
    function actualizarPagina() {
        // registrosPaginables reúne las fichas de esta sección; cambiar la colección altera el total.
        const registrosPaginables = Array.from(listaPaginada.querySelectorAll("[data-registro-paginable]"));
        // totalRegistros fija el último índice; cambiarlo cambia el rango y la cantidad de páginas.
        const totalRegistros = registrosPaginables.length;
        // tamanoElegido lee el valor escogido; cero significa mostrar la lista completa.
        const tamanoElegido = Number(selectorTamano.value);
        // tamanoPagina define el tamaño del bloque; cambiarlo modifica cuántas fichas aparecen.
        const tamanoPagina = tamanoElegido === 0 ? Math.max(totalRegistros, 1) : tamanoElegido;
        // totalPaginas calcula el máximo de avance; cambiar el tamaño altera cuántas páginas existen.
        const totalPaginas = Math.max(1, Math.ceil(totalRegistros / tamanoPagina));

        paginaActual = Math.min(paginaActual, totalPaginas);

        // indiceInicial marca la primera ficha del bloque actual.
        const indiceInicial = (paginaActual - 1) * tamanoPagina;
        // indiceFinal marca el límite exclusivo del bloque actual.
        const indiceFinal = Math.min(indiceInicial + tamanoPagina, totalRegistros);

        // registroPaginable representa una ficha individual; indiceRegistro define si pertenece a esta página.
        registrosPaginables.forEach((registroPaginable, indiceRegistro) => {
            // debeMostrarse indica si la ficha cae dentro del rango activo.
            const debeMostrarse = indiceRegistro >= indiceInicial && indiceRegistro < indiceFinal;

            registroPaginable.hidden = !debeMostrarse;
            registroPaginable.classList.remove("entrada-suave-conexion");

            if (debeMostrarse) {
                // Reiniciar la clase reproduce la transición al cambiar de página.
                void registroPaginable.offsetWidth;
                registroPaginable.classList.add("entrada-suave-conexion");
            }
        });

        // primerRegistro y ultimoRegistro delimitan el rango que se anuncia.
        const primerRegistro = totalRegistros === 0 ? 0 : indiceInicial + 1;
        const ultimoRegistro = indiceFinal;
        resumenPagina.textContent = `Mostrando ${primerRegistro}–${ultimoRegistro} de ${totalRegistros}`;

        botonAnterior.disabled = paginaActual <= 1;
        botonSiguiente.disabled = paginaActual >= totalPaginas || tamanoElegido === 0;
    }

    // Cambiar el tamaño vuelve al principio para evitar saltos a una página inexistente.
    selectorTamano.addEventListener("change", () => {
        paginaActual = 1;
        actualizarPagina();
    });

    // La flecha siguiente mueve el rango una página.
    botonSiguiente.addEventListener("click", () => {
        paginaActual += 1;
        actualizarPagina();
    });

    // La flecha anterior devuelve el rango una página.
    botonAnterior.addEventListener("click", () => {
        paginaActual -= 1;
        actualizarPagina();
    });

    actualizarPagina();
}
