(() => {
    "use strict";

    // botonesImprimir permite imprimir el mapa o el detalle del switch desde el navegador.
    const botonesImprimir = Array.from(document.querySelectorAll("[data-imprimir-topologia]"));
    botonesImprimir.forEach(boton => boton.addEventListener("click", () => window.print()));

    // hojasImpresion reúne vistas que tienen una plantilla de una sola hoja A4 horizontal.
    const hojasImpresion = Array.from(document.querySelectorAll("[data-hoja-impresion]"));
    // anchoA4HorizontalPx conserva el ancho imprimible aproximado tras márgenes de 5 mm.
    const anchoA4HorizontalPx = 287 * 96 / 25.4;
    // altoA4HorizontalPx conserva el alto imprimible aproximado tras márgenes de 5 mm.
    const altoA4HorizontalPx = 200 * 96 / 25.4;

    /**
     * Calcula la escala mínima que permite incluir la vista imprimible en una página.
     * Cambiar los límites A4 modifica cuánto se reduce el contenido antes de imprimir.
     */
    const ajustarEscalaImpresion = () => {
        // hoja recorre una plantilla imprimible; cambiar el conjunto cambia qué vistas se escalan.
        hojasImpresion.forEach(hoja => {
            // contenidoImpresion es el único bloque cuyo tamaño se reduce para ajustarse a la página.
            const contenidoImpresion = hoja.querySelector("[data-contenido-impresion]");
            if (!contenidoImpresion) return;

            contenidoImpresion.style.setProperty("--escala-impresion", "1");
            if (hoja.classList.contains("hoja-impresion-switch")) {
                // cantidadConexiones determina cuántas columnas permiten usar mejor la hoja sin encoger todo el contenido.
                const cantidadConexiones = hoja.querySelectorAll(".ficha-conexion").length;
                // columnasImpresion aumenta columnas solo cuando la cantidad de tarjetas lo necesita.
                const columnasImpresion = cantidadConexiones > 24 ? 4 : cantidadConexiones > 12 ? 3 : 2;
                contenidoImpresion.style.setProperty("--columnas-enlaces-impresion", String(columnasImpresion));
            }
            // anchoContenido mide el contenido a escala normal para calcular el ancho que requiere.
            const anchoContenido = contenidoImpresion.scrollWidth;
            // altoContenido mide todo el contenido visible en modo impresión.
            const altoContenido = contenidoImpresion.scrollHeight;
            if (anchoContenido <= 0 || altoContenido <= 0) return;

            // escalaImpresion mantiene la vista en su tamaño natural si cabe y la reduce si se excede.
            const escalaImpresion = Math.min(
                1,
                anchoA4HorizontalPx / anchoContenido,
                altoA4HorizontalPx / altoContenido
            );
            contenidoImpresion.style.setProperty("--escala-impresion", escalaImpresion.toFixed(4));
        });
    };

    /**
     * Limpia las escalas temporales al volver a la vista normal.
     * Cambiar las hojas seleccionadas limita qué bloques se restauran tras cerrar la impresión.
     */
    const restaurarEscalaImpresion = () => {
        // hoja recorre una plantilla imprimible; cambiar el conjunto cambia qué vistas se restauran.
        hojasImpresion.forEach(hoja => {
            // contenidoImpresion recupera su tamaño de pantalla después del diálogo de impresión.
            const contenidoImpresion = hoja.querySelector("[data-contenido-impresion]");
            contenidoImpresion?.style.removeProperty("--escala-impresion");
            contenidoImpresion?.style.removeProperty("--columnas-enlaces-impresion");
        });
    };

    window.addEventListener("beforeprint", ajustarEscalaImpresion);
    window.addEventListener("afterprint", restaurarEscalaImpresion);

    // puertosInteractivos reúne las bocas seleccionables del frontal para teclado y ratón.
    const puertosInteractivos = Array.from(document.querySelectorAll("[data-puerto-topologia]"));
    // tarjetasConexion reúne fichas con una etiqueta local para enlazarlas con el frontal.
    const tarjetasConexion = Array.from(document.querySelectorAll("[data-puerto-local]"));

    /**
     * Activa una boca y, si se solicita, desplaza la vista hasta su ficha de conexión.
     * @param {HTMLElement} puerto Boca seleccionada; su etiqueta determina las fichas relacionadas.
     * @param {boolean} desplazarAConexion Si es true, la página baja hasta la primera ficha coincidente.
     */
    function seleccionarPuertoTopologia(puerto, desplazarAConexion) {
        // etiquetaSeleccionada conserva la identificación visible que relaciona la boca y la ficha.
        const etiquetaSeleccionada = puerto.dataset.etiquetaPuerto;
        // puertoActual recorre las bocas para retirar la selección previa del frontal.
        puertosInteractivos.forEach(puertoActual => {
            // estaSeleccionado indica si la boca recorrida es la que recibió la acción.
            const estaSeleccionado = puertoActual === puerto;
            puertoActual.classList.toggle("puerto-seleccionado", estaSeleccionado);
            puertoActual.setAttribute("aria-pressed", estaSeleccionado ? "true" : "false");
        });
        // tarjeta recorre las conexiones y conserva el resaltado solo para la etiqueta elegida.
        tarjetasConexion.forEach(tarjeta => {
            tarjeta.classList.toggle("conexion-relacionada-activa",
                tarjeta.dataset.puertoLocal === etiquetaSeleccionada);
        });
        // mensaje recorre las regiones accesibles que anuncian la selección.
        // contenedorFrontal limita el aviso a la vista del switch que contiene el puerto pulsado.
        const contenedorFrontal = puerto.closest(".panel-frontal-switch");
        // mensajePuerto evita anunciar la misma selección desde todos los switches del mapa.
        const mensajePuerto = contenedorFrontal?.querySelector("[data-mensaje-puerto]");
        if (mensajePuerto) {
            mensajePuerto.textContent = etiquetaSeleccionada
                ? `Puerto ${etiquetaSeleccionada}: ${puerto.dataset.estadoPuerto}. Destino: ${puerto.dataset.destinoPuerto}.`
                : "Puerto sin etiqueta documentada.";
        }
        if (desplazarAConexion) {
            // tarjetaCoincidente busca la primera ficha cuyo puerto local coincide literalmente.
            const tarjetaCoincidente = tarjetasConexion.find(tarjeta =>
                tarjeta.dataset.puertoLocal === etiquetaSeleccionada);
            tarjetaCoincidente?.scrollIntoView({ behavior: "smooth", block: "center" });
        }
    }

    // puerto recorre las bocas y vincula sus eventos de ratón y teclado.
    puertosInteractivos.forEach(puerto => {
        // activarPuerto selecciona la boca indicada al recibir un clic.
        puerto.addEventListener("click", () => seleccionarPuertoTopologia(puerto, true));
        // activarConTeclado procesa el evento recibido; si es Enter o Espacio, selecciona la boca.
        puerto.addEventListener("keydown", evento => {
            if (evento.key === "Enter" || evento.key === " ") {
                evento.preventDefault();
                seleccionarPuertoTopologia(puerto, true);
            }
        });
    });

    // tarjeta recorre las fichas y vincula su selección con el frontal.
    tarjetasConexion.forEach(tarjeta => {
        // seleccionarDesdeFicha usa el evento de clic para sincronizar una ficha con el frontal.
        tarjeta.addEventListener("click", evento => {
            if (evento.target.closest("a, button, summary, input, select, textarea")) {
                return;
            }
            // puertoRelacionado localiza la boca con la misma etiqueta local de la ficha.
            const puertoRelacionado = puertosInteractivos.find(puerto =>
                puerto.dataset.etiquetaPuerto === tarjeta.dataset.puertoLocal);
            if (puertoRelacionado) {
                seleccionarPuertoTopologia(puertoRelacionado, false);
            }
        });
    });

    // mapaTopologia contiene los nodos y el SVG superpuesto para las aristas documentales.
    const mapaTopologia = document.querySelector("[data-mapa-topologia]");
    if (!mapaTopologia) {
        return;
    }

    // panelMapa contiene controles y lienzo para maximizar la navegación en cualquier capa.
    const panelMapa = mapaTopologia.closest(".panel-grafico-topologia");
    // botonesMaximizarMapa activa la pantalla completa desde el mapa actual.
    const botonesMaximizarMapa = Array.from(document.querySelectorAll("[data-maximizar-mapa]"));
    // botonesRestablecerMapa centra el contenido en su origen navegable.
    const botonesRestablecerMapa = Array.from(document.querySelectorAll("[data-restablecer-mapa]"));
    // arrastreMapaActivo indica si el puntero está desplazando el espacio vacío del lienzo.
    let arrastreMapaActivo = false;
    // posicionPunteroInicialX guarda la coordenada horizontal al iniciar el desplazamiento.
    let posicionPunteroInicialX = 0;
    // posicionPunteroInicialY guarda la coordenada vertical al iniciar el desplazamiento.
    let posicionPunteroInicialY = 0;
    // desplazamientoMapaInicialX conserva el scroll horizontal previo al arrastre.
    let desplazamientoMapaInicialX = 0;
    // desplazamientoMapaInicialY conserva el scroll vertical previo al arrastre.
    let desplazamientoMapaInicialY = 0;


    /**
     * Sincroniza texto y estado accesible del control de maximizado.
     * Cambiar los estados considerados modifica cuándo se ofrece restaurar la ventana.
     */
    const actualizarControlMaximizar = () => {
        // mapaMaximizado mantiene coherente el control con pantalla completa y modo alternativo.
        const mapaMaximizado = Boolean(panelMapa)
            && (document.fullscreenElement === panelMapa || panelMapa.classList.contains("mapa-ampliado"));
        // botonMaximizar actualiza cada control disponible para el mismo mapa.
        botonesMaximizarMapa.forEach(botonMaximizar => {
            // etiquetaControl conserva el nombre accesible sincronizado con la acción disponible.
            const etiquetaControl = mapaMaximizado ? "Restaurar mapa" : "Maximizar mapa";
            botonMaximizar.textContent = etiquetaControl;
            botonMaximizar.setAttribute("aria-label", etiquetaControl);
            botonMaximizar.setAttribute("aria-pressed", mapaMaximizado ? "true" : "false");
        });
    };

    /**
     * Maximiza el panel para recorrer el mapa y permite volver a la página con el mismo botón.
     * Cambiar el panel objetivo modifica el área que ocupa la pantalla completa.
     */
    const alternarMaximizacionMapa = async () => {
        if (!panelMapa) return;
        const mapaYaAmpliado = document.fullscreenElement === panelMapa
            || panelMapa.classList.contains("mapa-ampliado");
        if (mapaYaAmpliado) {
            if (document.fullscreenElement === panelMapa) {
                await document.exitFullscreen?.();
            } else {
                panelMapa.classList.remove("mapa-ampliado");
                document.body.classList.remove("mapa-a-pantalla-completa");
            }
        } else if (typeof panelMapa.requestFullscreen === "function") {
            try {
                await panelMapa.requestFullscreen();
            } catch {
                panelMapa.classList.add("mapa-ampliado");
                document.body.classList.add("mapa-a-pantalla-completa");
            }
        } else {
            panelMapa.classList.add("mapa-ampliado");
            document.body.classList.add("mapa-a-pantalla-completa");
        }
        actualizarControlMaximizar();
        window.requestAnimationFrame(() => dibujarEnlaces());
    };

    botonesMaximizarMapa.forEach(botonMaximizar => {
        // botonMaximizar inicia y restaura el panel completo sin perder la capa seleccionada.
        botonMaximizar.addEventListener("click", () => alternarMaximizacionMapa());
    });
    botonesRestablecerMapa.forEach(botonRestablecer => {
        // botonRestablecer devuelve el viewport al inicio del mapa que siga seleccionado.
        botonRestablecer.addEventListener("click", () => mapaTopologia.scrollTo({ left: 0, top: 0, behavior: "smooth" }));
    });

    /**
     * Inicia el desplazamiento del lienzo al arrastrar su espacio vacío.
     * Cambiar el conjunto de controles excluidos modifica qué objetos se pueden arrastrar.
     * @param {PointerEvent} eventoPuntero Evento de puntero; sus coordenadas establecen el origen del movimiento.
     */
    const iniciarArrastreMapa = eventoPuntero => {
        if (eventoPuntero.button !== 0
            || eventoPuntero.target.closest("[data-dispositivo], button, a, input, select, textarea, summary, path")) return;
        arrastreMapaActivo = true;
        posicionPunteroInicialX = eventoPuntero.clientX;
        posicionPunteroInicialY = eventoPuntero.clientY;
        desplazamientoMapaInicialX = mapaTopologia.scrollLeft;
        desplazamientoMapaInicialY = mapaTopologia.scrollTop;
        mapaTopologia.classList.add("arrastrando-mapa");
        mapaTopologia.setPointerCapture(eventoPuntero.pointerId);
    };

    /**
     * Mueve el viewport con el puntero manteniendo juntas las tarjetas y sus líneas.
     * Cambiar las coordenadas recibidas desplaza el lienzo en el sentido opuesto.
     * @param {PointerEvent} eventoPuntero Posición actual del puntero mientras se arrastra el fondo.
     */
    const moverMapaArrastrado = eventoPuntero => {
        if (!arrastreMapaActivo) return;
        eventoPuntero.preventDefault();
        mapaTopologia.scrollLeft = desplazamientoMapaInicialX - (eventoPuntero.clientX - posicionPunteroInicialX);
        mapaTopologia.scrollTop = desplazamientoMapaInicialY - (eventoPuntero.clientY - posicionPunteroInicialY);
    };

    /**
     * Finaliza el desplazamiento y devuelve el cursor a su estado normal.
     * Cambiar el estado activo determina si las siguientes coordenadas siguen moviendo el mapa.
     */
    const finalizarArrastreMapa = () => {
        arrastreMapaActivo = false;
        mapaTopologia.classList.remove("arrastrando-mapa");
    };

    mapaTopologia.addEventListener("pointerdown", iniciarArrastreMapa);
    mapaTopologia.addEventListener("pointermove", moverMapaArrastrado);
    mapaTopologia.addEventListener("pointerup", finalizarArrastreMapa);
    mapaTopologia.addEventListener("pointercancel", finalizarArrastreMapa);

    document.addEventListener("fullscreenchange", () => {
        actualizarControlMaximizar();
        window.requestAnimationFrame(() => dibujarEnlaces());
    });

    // capaEnlaces aloja las líneas que se recalculan cuando cambia la distribución visual.
    const capaEnlaces = mapaTopologia.querySelector("[data-capa-enlaces]");
    // nodosTopologia son las fichas físicas visibles y filtrables.
    const nodosTopologia = Array.from(mapaTopologia.querySelectorAll("[data-dispositivo]"));
    // datosEnlaces conserva extremos y etiquetas producidos por el servidor.
    const datosEnlaces = Array.from(document.querySelectorAll("[data-enlace-topologia]"));
    // buscarTopologia y filtrarArea reducen el gráfico sin alterar los datos guardados.
    const buscarTopologia = document.querySelector("[data-busqueda-topologia]");
    const filtrarArea = document.querySelector("[data-filtro-area]");
    // espacioSvg identifica el espacio de nombres que necesita el navegador para crear rutas SVG.
    const espacioSvg = "http://www.w3.org/2000/svg";
    // botonesCapa permite elegir una perspectiva sin cambiar el inventario guardado.
    const botonesCapa = Array.from(document.querySelectorAll("[data-seleccionar-capa]"));
    // resumenMapa anuncia cuántos nodos y enlaces quedan visibles tras aplicar filtros.
    const resumenMapa = document.querySelector("[data-resumen-mapa]");
    // modoCapaActual inicia el mapa en la vista física completa.
    let modoCapaActual = "fisica";

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
     * Decide si una relación pertenece a la perspectiva activa del mapa.
     * @param {HTMLElement} enlace Relación documental; su capa asignada decide si se dibuja.
     * @returns {boolean} Verdadero para todas las relaciones físicas o para la capa seleccionada.
     */
    const perteneceALaCapaActual = enlace =>
        modoCapaActual === "fisica" || enlace.dataset.capaRelacional === modoCapaActual;

    /**
     * Omite una relación parcial cuando ya existe un enlace completo con el mismo puerto local y equipos.
     * @param {HTMLElement} relacion Relación parcial candidata a dibujar.
     * @returns {boolean} Verdadero si un registro completo ya representa ese mismo enlace.
     */
    const esParcialDuplicada = relacion => {
        if (relacion.dataset.relacionParcial !== "true") {
            return false;
        }
        return datosEnlaces.some(enlaceCompleto => {
            if (enlaceCompleto.dataset.relacionParcial === "true") {
                return false;
            }
            const mismosEquipos = enlaceCompleto.dataset.equipoOrigen === relacion.dataset.equipoOrigen
                && enlaceCompleto.dataset.equipoDestino === relacion.dataset.equipoDestino
                || enlaceCompleto.dataset.equipoOrigen === relacion.dataset.equipoDestino
                && enlaceCompleto.dataset.equipoDestino === relacion.dataset.equipoOrigen;
            const mismoPuertoLocal = enlaceCompleto.dataset.puertoOrigen === relacion.dataset.puertoOrigen
                || enlaceCompleto.dataset.puertoDestino === relacion.dataset.puertoOrigen;
            return mismosEquipos && mismoPuertoLocal;
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
        capaEnlaces.style.width = `${anchoMapa}px`;
        capaEnlaces.style.height = `${altoMapa}px`;
        capaEnlaces.replaceChildren();

        // nodosPorId resuelve IDs relacionales sin construir selectores con valores externos.
        const nodosPorId = new Map(nodosTopologia.map(nodo => [nodo.dataset.identificadorDispositivo, nodo]));
        // repeticionesPorPar separa visualmente varios cables entre el mismo par de equipos.
        const repeticionesPorPar = new Map();

        datosEnlaces.forEach(enlace => {
            if (!perteneceALaCapaActual(enlace) || esParcialDuplicada(enlace)) {
                return;
            }
            const nodoOrigen = nodosPorId.get(enlace.dataset.equipoOrigen);
            const nodoDestino = nodosPorId.get(enlace.dataset.equipoDestino);
            if (!nodoOrigen || !nodoDestino || nodoOrigen.hidden || nodoDestino.hidden) {
                return;
            }

            // rectangulosExtremos ubica los nodos dentro del mapa actual.
            const rectanguloOrigen = nodoOrigen.getBoundingClientRect();
            const rectanguloDestino = nodoDestino.getBoundingClientRect();
            const centroOrigen = {
                x: rectanguloOrigen.left + rectanguloOrigen.width / 2 - rectanguloMapa.left + mapaTopologia.scrollLeft,
                y: rectanguloOrigen.top + rectanguloOrigen.height / 2 - rectanguloMapa.top + mapaTopologia.scrollTop
            };
            const centroDestino = {
                x: rectanguloDestino.left + rectanguloDestino.width / 2 - rectanguloMapa.left + mapaTopologia.scrollLeft,
                y: rectanguloDestino.top + rectanguloDestino.height / 2 - rectanguloMapa.top + mapaTopologia.scrollTop
            };
            const clavePar = [enlace.dataset.equipoOrigen, enlace.dataset.equipoDestino].sort().join("-");
            const repeticion = repeticionesPorPar.get(clavePar) || 0;
            repeticionesPorPar.set(clavePar, repeticion + 1);

            // rutaEnlace es una curva entre los bordes de las tarjetas, sin marcar una dirección de tráfico.
            const rutaEnlace = document.createElementNS(espacioSvg, "path");
            rutaEnlace.setAttribute("d", crearRuta(centroOrigen, centroDestino, rectanguloOrigen, rectanguloDestino, rectanguloMapa, repeticion));
            // switchSinCotejoReciproco señala un uplink confirmado cuya tabla opuesta no permite elegir un puerto único.
            const switchSinCotejoReciproco = enlace.dataset.relacionParcial === "true"
                && enlace.dataset.capaRelacional === "switching"
                && enlace.dataset.cotejoReciproco !== "true";
            // claseCapaEnlace distingue visualmente acceso, switching y recorrido físico.
            const claseCapaEnlace = enlace.dataset.capaRelacional === "switching" ? "enlace-capa-2"
                : enlace.dataset.capaRelacional === "acceso" ? "enlace-capa-3" : "enlace-capa-1";
            rutaEnlace.setAttribute("class", `enlace-topologia ${claseCapaEnlace} ${switchSinCotejoReciproco
                ? "enlace-parcial-switch"
                : enlace.dataset.confirmado === "true" ? "enlace-confirmado" : "enlace-pendiente"}`);
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
     * Cambia la perspectiva de red; el modo determina qué nodos pueden verse.
     * @param {string} modoSolicitado El valor física, switching o acceso limita la vista sin alterar los datos.
     */
    const seleccionarCapa = modoSolicitado => {
        // modoValido evita activar una vista desconocida si el HTML recibe un valor inesperado.
        const modoValido = ["fisica", "switching", "acceso"].includes(modoSolicitado) ? modoSolicitado : "fisica";
        modoCapaActual = modoValido;
        mapaTopologia.dataset.capaActual = modoValido;
        // boton cambia su estado accesible y visual según la capa seleccionada.
        botonesCapa.forEach(boton => {
            // estaActiva evita marcar simultáneamente dos perspectivas.
            const estaActiva = boton.dataset.seleccionarCapa === modoCapaActual;
            boton.classList.toggle("activa", estaActiva);
            boton.setAttribute("aria-pressed", String(estaActiva));
        });
        actualizarFiltros();
    };

    /**
     * Filtra equipos y enlaces por capa, texto y área sin inferir relaciones.
     */
    const actualizarFiltros = () => {
        // textoBuscado se compara sin distinguir mayúsculas ni tildes.
        const textoBuscado = normalizarTexto(buscarTopologia?.value || "");
        const areaElegida = filtrarArea?.value || "";
        // nodosPorId resuelve extremos de enlaces para construir la vista de acceso documentada.
        const nodosPorId = new Map(nodosTopologia.map(nodo => [nodo.dataset.identificadorDispositivo, nodo]));
        // identificadoresAcceso contiene puestos y switches enlazados explícitamente con puestos.
        const identificadoresAcceso = new Set(nodosTopologia
            // nodo se incluye como puesto si su tipo registrado contiene esa categoría.
            .filter(nodo => normalizarTexto(nodo.dataset.dispositivoTipo || "").includes("puesto"))
            .map(nodo => nodo.dataset.identificadorDispositivo));

        if (modoCapaActual === "acceso") {
            // enlace recorre vínculos de switch a equipo final para incluir ambos nodos en Acceso.
            datosEnlaces.filter(enlace => enlace.dataset.capaRelacional === "acceso")
                .forEach(enlace => {
                    identificadoresAcceso.add(enlace.dataset.equipoOrigen);
                    identificadoresAcceso.add(enlace.dataset.equipoDestino);
                });
        }

        nodosTopologia.forEach(nodo => {
            // coincideCapa limita la ficha a la perspectiva actualmente seleccionada.
            // tipoNormalizado permite aplicar los filtros por familia sin depender de mayúsculas ni tildes.
            const tipoNormalizado = normalizarTexto(nodo.dataset.dispositivoTipo || "");
            const coincideCapa = modoCapaActual === "fisica"
                || (modoCapaActual === "switching" && tipoNormalizado.includes("switch"))
                || (modoCapaActual === "acceso" && identificadoresAcceso.has(nodo.dataset.identificadorDispositivo));
            // datosNodo reúne campos documentados que participan de la búsqueda textual.
            const datosNodo = [
                nodo.dataset.dispositivoNombre,
                nodo.dataset.dispositivoTipo,
                nodo.dataset.dispositivoArea,
                nodo.dataset.dispositivoModelo,
                nodo.dataset.dispositivoIp
            ].join(" ");
            // textoNormalizado permite comparar la búsqueda sin mayúsculas ni acentos.
            const textoNormalizado = normalizarTexto(datosNodo);
            nodo.hidden = !coincideCapa
                || (areaElegida !== "" && nodo.dataset.dispositivoArea !== areaElegida)
                || (textoBuscado !== "" && !textoNormalizado.includes(textoBuscado));
        });
        dibujarEnlaces();
        actualizarResumen();
    };

    /**
     * Actualiza el resumen visible para reflejar solo nodos y enlaces del filtro actual.
     */
    const actualizarResumen = () => {
        // equiposVisibles cuenta los nodos presentes tras filtros de capa, texto y área.
        // equiposVisibles actualiza el resumen tras variar capa, área o búsqueda.
        const equiposVisibles = nodosTopologia.filter(nodo => !nodo.hidden).length;
        // enlacesVisibles cuenta solo relaciones cuyos dos extremos permanecen visibles.
        // enlacesVisibles excluye toda relación cuyo nodo esté oculto por los filtros.
        const enlacesVisibles = datosEnlaces.filter(enlace => {
            // origen y destino permiten contar solo aristas con ambos nodos visibles.
            const origen = nodosPorIdentificador(enlace.dataset.equipoOrigen);
            const destino = nodosPorIdentificador(enlace.dataset.equipoDestino);
            return perteneceALaCapaActual(enlace) && !esParcialDuplicada(enlace)
                && origen && destino && !origen.hidden && !destino.hidden;
        }).length;
        if (resumenMapa) {
            resumenMapa.textContent = `${equiposVisibles} equipos · ${enlacesVisibles} enlaces`;
        }
    };

    /**
     * Busca una ficha por su identificador dentro de los nodos renderizados.
     * @param {string} identificadorEquipo ID documental del nodo; cambiarlo busca otra ficha.
     * @returns {Element|undefined} Nodo correspondiente o indefinido cuando no está en la vista.
     */
    const nodosPorIdentificador = identificadorEquipo =>
        nodosTopologia.find(nodo => nodo.dataset.identificadorDispositivo === identificadorEquipo);

    /**
     * Normaliza texto del filtro para comparar nombres con o sin tildes.
     * @param {string} texto Valor a normalizar; cambiarlo produce otro texto de comparación.
     * @returns {string} Texto en minúsculas y sin marcas diacríticas.
     */
    const normalizarTexto = texto => texto.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("es");

    botonesCapa.forEach(boton => {
        // cada boton cambia solo la perspectiva visual y conserva los filtros de búsqueda y área.
        boton.addEventListener("click", () => seleccionarCapa(boton.dataset.seleccionarCapa || "fisica"));
    });
    buscarTopologia?.addEventListener("input", actualizarFiltros);
    filtrarArea?.addEventListener("change", actualizarFiltros);
    // observadorMapa redibuja las conexiones cuando cambia el tamaño de la cuadrícula.
    const observadorMapa = new ResizeObserver(dibujarEnlaces);
    observadorMapa.observe(mapaTopologia);
    if (panelMapa) observadorMapa.observe(panelMapa);
    window.addEventListener("resize", dibujarEnlaces);
    actualizarFiltros();
})();
