(() => {
    "use strict";

    // controlesPaginacion localiza el selector y controles; cambiar el selector raíz limita la tabla afectada.
    const controlesPaginacion = document.querySelector("[data-paginacion-inventario]");
    // filasInventario contiene todas las coincidencias de la búsqueda actual.
    const filasInventario = Array.from(document.querySelectorAll("[data-registro-inventario]"));

    if (!controlesPaginacion) {
        return;
    }

    // selectorLimite aporta la cantidad visible; cambiar su opción modifica el tamaño de página.
    const selectorLimite = controlesPaginacion.querySelector("[data-limite-registros]");
    // resumenPagina comunica el rango visible; cambiar el texto altera el estado que anuncia la interfaz.
    const resumenPagina = controlesPaginacion.querySelector("[data-resumen-pagina-inventario]");
    // botonAnterior y botonSiguiente desplazan las filas visibles por páginas.
    const botonAnterior = controlesPaginacion.querySelector("[data-pagina-anterior]");
    const botonSiguiente = controlesPaginacion.querySelector("[data-pagina-siguiente]");
    // paginaActual usa base cero; cambiar el valor selecciona otro grupo de filas.
    let paginaActual = 0;

    /**
     * Actualiza las filas, el resumen y el estado de los botones según el límite elegido.
     */
    const actualizarTabla = () => {
        // limiteSeleccionado convierte "todos" en un tamaño que cubre todas las filas.
        const limiteSeleccionado = selectorLimite.value === "todos"
            ? Math.max(filasInventario.length, 1)
            : Number.parseInt(selectorLimite.value, 10);
        // totalPaginas mantiene una página disponible aun cuando no haya coincidencias.
        const totalPaginas = Math.max(1, Math.ceil(filasInventario.length / limiteSeleccionado));
        paginaActual = Math.min(paginaActual, totalPaginas - 1);

        // inicio y fin delimitan el segmento visible de la tabla.
        const inicio = paginaActual * limiteSeleccionado;
        const fin = Math.min(inicio + limiteSeleccionado, filasInventario.length);
        filasInventario.forEach((fila, indice) => {
            fila.hidden = indice < inicio || indice >= fin;
        });

        resumenPagina.textContent = filasInventario.length === 0
            ? "Sin registros"
            : `${inicio + 1}–${fin} de ${filasInventario.length}`;
        botonAnterior.disabled = paginaActual === 0;
        botonSiguiente.disabled = paginaActual >= totalPaginas - 1;
    };

    // selectorLimite reinicia a la primera página cuando cambia la cantidad elegida.
    selectorLimite.addEventListener("change", () => {
        paginaActual = 0;
        actualizarTabla();
    });
    // botonAnterior retrocede una página sin salir del rango disponible.
    botonAnterior.addEventListener("click", () => {
        paginaActual -= 1;
        actualizarTabla();
    });
    // botonSiguiente avanza una página sin superar el último bloque.
    botonSiguiente.addEventListener("click", () => {
        paginaActual += 1;
        actualizarTabla();
    });

    actualizarTabla();
})();
