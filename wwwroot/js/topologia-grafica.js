(() => {
    "use strict";

    // mapaTopologia contiene los nodos y el SVG superpuesto para las aristas documentales.
    const mapaTopologia = document.querySelector("[data-mapa-topologia]");
    if (!mapaTopologia) {
        return;
    }

    // capaEnlaces aloja las líneas que se recalculan cuando cambia la distribución visual.
    const capaEnlaces = mapaTopologia.querySelector("[data-capa-enlaces]");
    // nodosTopologia son las fichas físicas visibles y filtrables.
    const nodosTopologia = Array.from(mapaTopologia.querySelectorAll("[data-dispositivo]"));
    // datosEnlaces conserva extremos y etiquetas producidos por el servidor.
    const datosEnlaces = Array.from(document.querySelectorAll("[data-enlace-topologia]"));
    // buscarTopologia y filtrarArea reducen el gráfico sin alterar los datos guardados.
    const buscarTopologia = document.querySelector("[data-busqueda-topologia]");
    const filtrarArea = document.querySelector("[data-filtro-area]");
    const espacioSvg = "http://www.w3.org/2000/svg";

    /**
     * Marca los puertos extremos al enfocar o señalar una línea del gráfico.
     */
    const resaltarExtremos = (enlace, resaltar) => {
        // identificadoresPuerto vincula la arista con las interfaces guardadas de ambos extremos.
        const identificadoresPuerto = [enlace.dataset.puertoOrigen, enlace.dataset.puertoDestino];
        identificadoresPuerto.forEach(identificador => {
            const puerto = mapaTopologia.querySelector(`[data-identificador-interfaz="${identificador}"]`);
            if (puerto) {
                puerto.classList.toggle("puerto-resaltado", resaltar);
            }
        });
    };

    /**
     * Recalcula las líneas entre centros de nodos, sin crear relaciones nuevas.
     */
    const dibujarEnlaces = () => {
        // rectanguloMapa determina el sistema de coordenadas relativo de cada ruta SVG.
        const rectanguloMapa = mapaTopologia.getBoundingClientRect();
        const anchoMapa = Math.max(mapaTopologia.scrollWidth, mapaTopologia.clientWidth);
        const altoMapa = Math.max(mapaTopologia.scrollHeight, mapaTopologia.clientHeight);
        capaEnlaces.setAttribute("viewBox", `0 0 ${anchoMapa} ${altoMapa}`);
        capaEnlaces.setAttribute("width", anchoMapa);
        capaEnlaces.setAttribute("height", altoMapa);
        capaEnlaces.replaceChildren();

        // nodosPorId resuelve IDs relacionales sin construir selectores con valores externos.
        const nodosPorId = new Map(nodosTopologia.map(nodo => [nodo.dataset.identificadorDispositivo, nodo]));
        // repeticionesPorPar separa visualmente varios cables entre el mismo par de equipos.
        const repeticionesPorPar = new Map();

        datosEnlaces.forEach(enlace => {
            const nodoOrigen = nodosPorId.get(enlace.dataset.equipoOrigen);
            const nodoDestino = nodosPorId.get(enlace.dataset.equipoDestino);
            if (!nodoOrigen || !nodoDestino || nodoOrigen.hidden || nodoDestino.hidden) {
                return;
            }

            // rectangulosExtremos ubica los nodos dentro del mapa actual.
            const rectanguloOrigen = nodoOrigen.getBoundingClientRect();
            const rectanguloDestino = nodoDestino.getBoundingClientRect();
            const centroOrigen = {
                x: rectanguloOrigen.left + rectanguloOrigen.width / 2 - rectanguloMapa.left,
                y: rectanguloOrigen.top + rectanguloOrigen.height / 2 - rectanguloMapa.top
            };
            const centroDestino = {
                x: rectanguloDestino.left + rectanguloDestino.width / 2 - rectanguloMapa.left,
                y: rectanguloDestino.top + rectanguloDestino.height / 2 - rectanguloMapa.top
            };
            const clavePar = [enlace.dataset.equipoOrigen, enlace.dataset.equipoDestino].sort().join("-");
            const repeticion = repeticionesPorPar.get(clavePar) || 0;
            repeticionesPorPar.set(clavePar, repeticion + 1);

            // rutaEnlace es una curva entre los bordes de las tarjetas, sin marcar una dirección de tráfico.
            const rutaEnlace = document.createElementNS(espacioSvg, "path");
            rutaEnlace.setAttribute("d", crearRuta(centroOrigen, centroDestino, rectanguloOrigen, rectanguloDestino, rectanguloMapa, repeticion));
            rutaEnlace.setAttribute("class", enlace.dataset.confirmado === "true"
                ? "enlace-topologia enlace-confirmado"
                : "enlace-topologia enlace-pendiente");
            rutaEnlace.setAttribute("tabindex", "0");
            rutaEnlace.setAttribute("role", "img");
            rutaEnlace.setAttribute("aria-label", enlace.dataset.etiqueta);
            const tituloEnlace = document.createElementNS(espacioSvg, "title");
            tituloEnlace.textContent = enlace.dataset.etiqueta;
            rutaEnlace.append(tituloEnlace);
            rutaEnlace.addEventListener("pointerenter", () => resaltarExtremos(enlace, true));
            rutaEnlace.addEventListener("pointerleave", () => resaltarExtremos(enlace, false));
            rutaEnlace.addEventListener("focus", () => resaltarExtremos(enlace, true));
            rutaEnlace.addEventListener("blur", () => resaltarExtremos(enlace, false));
            capaEnlaces.append(rutaEnlace);
        });
    };

    /**
     * Calcula puntos de salida y llegada y curva una arista entre dos tarjetas.
     */
    const crearRuta = (centroOrigen, centroDestino, rectanguloOrigen, rectanguloDestino, rectanguloMapa, repeticion) => {
        if (centroOrigen.x === centroDestino.x && centroOrigen.y === centroDestino.y) {
            const radioBucle = 28 + repeticion * 12;
            return `M ${centroOrigen.x} ${centroOrigen.y} C ${centroOrigen.x + radioBucle} ${centroOrigen.y - radioBucle * 2}, ${centroOrigen.x - radioBucle} ${centroOrigen.y - radioBucle * 2}, ${centroOrigen.x} ${centroOrigen.y}`;
        }

        // delta mide la dirección entre nodos y ancla cada punta sobre el borde más cercano.
        const diferencia = { x: centroDestino.x - centroOrigen.x, y: centroDestino.y - centroOrigen.y };
        const factorOrigen = 1 / Math.max(Math.abs(diferencia.x) / (rectanguloOrigen.width / 2), Math.abs(diferencia.y) / (rectanguloOrigen.height / 2), 0.001);
        const factorDestino = 1 / Math.max(Math.abs(diferencia.x) / (rectanguloDestino.width / 2), Math.abs(diferencia.y) / (rectanguloDestino.height / 2), 0.001);
        const salida = { x: centroOrigen.x + diferencia.x * factorOrigen, y: centroOrigen.y + diferencia.y * factorOrigen };
        const llegada = { x: centroDestino.x - diferencia.x * factorDestino, y: centroDestino.y - diferencia.y * factorDestino };
        const desplazamiento = (repeticion % 5 - 2) * 14;
        const centroX = (salida.x + llegada.x) / 2;
        return `M ${salida.x} ${salida.y} C ${centroX} ${salida.y + desplazamiento}, ${centroX} ${llegada.y + desplazamiento}, ${llegada.x} ${llegada.y}`;
    };

    /**
     * Filtra nodos por texto y área y oculta aristas con extremos no visibles.
     */
    const actualizarFiltros = () => {
        // textoBuscado se compara sin distinguir mayúsculas ni tildes.
        const textoBuscado = normalizarTexto(buscarTopologia?.value || "");
        const areaElegida = filtrarArea?.value || "";
        nodosTopologia.forEach(nodo => {
            const datosNodo = [
                nodo.dataset.dispositivoNombre,
                nodo.dataset.dispositivoTipo,
                nodo.dataset.dispositivoArea,
                nodo.dataset.dispositivoModelo,
                nodo.dataset.dispositivoIp
            ].join(" ");
            const textoNormalizado = normalizarTexto(datosNodo);
            nodo.hidden = (areaElegida !== "" && nodo.dataset.dispositivoArea !== areaElegida)
                || (textoBuscado !== "" && !textoNormalizado.includes(textoBuscado));
        });
        dibujarEnlaces();
    };

    /**
     * Normaliza texto del filtro para comparar nombres con o sin tildes.
     */
    const normalizarTexto = texto => texto.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("es");

    buscarTopologia?.addEventListener("input", actualizarFiltros);
    filtrarArea?.addEventListener("change", actualizarFiltros);
    // observadorMapa redibuja las conexiones cuando cambia el tamaño de la cuadrícula.
    const observadorMapa = new ResizeObserver(dibujarEnlaces);
    observadorMapa.observe(mapaTopologia);
    window.addEventListener("resize", dibujarEnlaces);
    actualizarFiltros();
})();
