(() => {
    "use strict";

    // botonesImprimir permite imprimir el mapa o el detalle del switch desde el navegador.
    const botonesImprimir = Array.from(document.querySelectorAll("[data-imprimir-topologia]"));
    botonesImprimir.forEach(boton => boton.addEventListener("click", () => window.print()));

    // hojasImpresion reúne vistas que tienen una plantilla de una sola hoja A4 horizontal.
    const hojasImpresion = Array.from(document.querySelectorAll("[data-hoja-impresion]"));
    // anchoA4HorizontalPx conserva el ancho imprimible aproximado tras márgenes de 5 mm.
    const anchoA4HorizontalPx = 287 * 96 / 25.4;

    /**
     * Prepara las páginas imprimibles sin reducir la hoja completa según el alto de la página web.
     * Cambiar el número de columnas altera la densidad de las conexiones en el detalle del switch.
     */
    const ajustarEscalaImpresion = () => {
        // hoja recorre las plantillas A4; cambiar el conjunto altera qué páginas se preparan.
        hojasImpresion.forEach(hoja => {
            // contenidoImpresion conserva el ancho imprimible real; cambiarlo cambia la escala de toda la página.
            const contenidoImpresion = hoja.querySelector("[data-contenido-impresion]");
            if (!contenidoImpresion) return;

            // La vista se imprime a escala 1; la topología aplica su propio zoom al lienzo.
            contenidoImpresion.style.setProperty("--escala-impresion", "1");
            if (hoja.classList.contains("hoja-impresion-switch")) {
                // cantidadConexiones define cuántas columnas compactan la lista sin encoger el frontal.
                const cantidadConexiones = hoja.querySelectorAll(".ficha-conexion").length;
                // columnasImpresion reparte mejor las fichas al crecer su número.
                const columnasImpresion = cantidadConexiones > 24 ? 4 : cantidadConexiones > 12 ? 3 : 2;
                contenidoImpresion.style.setProperty("--columnas-enlaces-impresion", String(columnasImpresion));
            }
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


    // mapaTopologia es la ventana que recorta el lienzo; si no existe, la página no tiene mapa y no hay nada más que enlazar.
    const mapaTopologia = document.querySelector("[data-mapa-topologia]");
    if (!mapaTopologia) {
        return;
    }

    // marcoMapa contiene la caja del mapa y es el único elemento que se expande dentro de la página.
    const marcoMapa = mapaTopologia.closest("[data-marco-mapa]");
    // lienzoTopologia es el plano donde viven tarjetas y líneas; se traslada y escala como un solo bloque al desplazar o acercar.
    const lienzoTopologia = mapaTopologia.querySelector("[data-lienzo-topologia]");
    // capaEnlaces dibuja las conexiones por debajo de las tarjetas.
    const capaEnlaces = lienzoTopologia.querySelector("[data-capa-enlaces]");
    // capaEnlacesActivos repite por encima de las tarjetas solo las conexiones de la tarjeta seleccionada.
    const capaEnlacesActivos = lienzoTopologia.querySelector("[data-capa-enlaces-activos]");
    // avisoMapaVacio se muestra cuando ningún equipo cumple capa, filtros y conexiones.
    const avisoMapaVacio = mapaTopologia.querySelector("[data-mapa-vacio]");
    // estadoMapa anuncia a lectores de pantalla qué tarjeta está seleccionada.
    const estadoMapa = document.querySelector("[data-estado-mapa]");
    // botonesMaximizarMapa expande o restaura la caja sin activar pantalla completa del navegador.
    const botonesMaximizarMapa = Array.from(document.querySelectorAll("[data-maximizar-mapa]"));
    // botonesRestablecerMapa devuelve la vista inicial: zoom legible centrado en el equipo principal.
    const botonesRestablecerMapa = Array.from(document.querySelectorAll("[data-restablecer-mapa]"));
    // controlZoom cambia el acercamiento con una sola barra en vez de dos botones.
    const controlZoom = document.querySelector("[data-control-zoom]");
    // botonVerTodo encaja todas las tarjetas visibles en la ventana, aunque el zoom quede pequeño.
    const botonVerTodo = document.querySelector("[data-ver-todo-mapa]");
    // botonDistribuir vuelve a aplicar el modo de distribución elegido, descartando las posiciones de la capa actual.
    const botonDistribuir = document.querySelector("[data-distribuir-mapa]");
    // selectorDistribucion elige cómo se reparten las tarjetas: mosaico, filas o cuadrícula.
    const selectorDistribucion = document.querySelector("[data-modo-distribucion]");
    // botonContraerTodo y botonExpandirTodo reducen o restauran el contenido de todas las tarjetas.
    const botonContraerTodo = document.querySelector("[data-contraer-todo]");
    const botonExpandirTodo = document.querySelector("[data-expandir-todo]");
    // botonColapsarGrupos y botonExpandirGrupos ocultan o muestran los equipos que cuelgan de cada switch.
    const botonColapsarGrupos = document.querySelector("[data-colapsar-grupos]");
    const botonExpandirGrupos = document.querySelector("[data-expandir-grupos]");
    // casillaSoloConectados oculta los equipos sin conexión en la capa elegida; desmarcarla muestra todo el inventario.
    const casillaSoloConectados = document.querySelector("[data-solo-conectados]");
    // etiquetaZoom muestra el nivel de acercamiento actual como porcentaje.
    const etiquetaZoom = document.querySelector("[data-nivel-zoom]");
    // nodosTopologia son las tarjetas de equipo; cada una se puede mover de forma independiente.
    const nodosTopologia = Array.from(lienzoTopologia.querySelectorAll("[data-dispositivo]"));
    // nodosPorId resuelve el ID de ficha a su tarjeta sin construir selectores con valores externos.
    const nodosPorId = new Map(nodosTopologia.map(nodo => [nodo.dataset.identificadorDispositivo, nodo]));
    // datosEnlaces conserva extremos y etiquetas producidos por el servidor.
    const datosEnlaces = Array.from(document.querySelectorAll("[data-enlace-topologia]"));
    // buscarTopologia y filtrarArea reducen el mapa sin alterar los datos guardados.
    const buscarTopologia = document.querySelector("[data-busqueda-topologia]");
    const filtrarArea = document.querySelector("[data-filtro-area]");
    // botonesCapa permite elegir una perspectiva sin cambiar el inventario guardado.
    const botonesCapa = Array.from(document.querySelectorAll("[data-seleccionar-capa]"));
    // resumenMapa anuncia cuántos equipos y enlaces quedan visibles tras aplicar filtros.
    const resumenMapa = document.querySelector("[data-resumen-mapa]");
    // espacioSvg identifica el espacio de nombres que necesita el navegador para crear elementos SVG.
    const espacioSvg = "http://www.w3.org/2000/svg";

    // escalaMinima y escalaMaxima limitan el acercamiento; ampliar el rango permite ver más lejos o más cerca.
    const escalaMinima = 0.1;
    const escalaMaxima = 1.6;
    // claveAlmacenamiento nombra el registro local de posiciones movidas; cambiarla descarta las posiciones guardadas.
    const claveAlmacenamiento = "topologia-tarjetas-v1";
    // separacionVertical es el espacio entre tarjetas apiladas; aumentarlo deja más aire entre ellas.
    const separacionVertical = 28;
    // separacionNiveles es el espacio horizontal entre niveles de la red; es el pasillo por donde se ven las líneas.
    const separacionNiveles = 170;
    // separacionSubcolumnas es el espacio entre columnas del mismo nivel cuando se parte una columna larga.
    const separacionSubcolumnas = 60;
    // alturaMaximaColumna parte una columna en varias cuando suma más alto que este valor (px del lienzo).
    const alturaMaximaColumna = 1500;
    // altoMapaImpresionPx reserva casi toda la altura imprimible del mapa A4 horizontal.
    const altoMapaImpresionPx = 180 * 96 / 25.4;

    // modoCapaActual inicia el mapa en la vista física completa.
    let modoCapaActual = "fisica";
    // vista guarda la traslación (px de pantalla) y el zoom del lienzo; cambiarla mueve o acerca todo el mapa.
    const vista = { x: 0, y: 0, escala: 1 };
    // posicionesPorCapa guarda la esquina superior izquierda de cada tarjeta en coordenadas del lienzo, por capa.
    const posicionesPorCapa = { fisica: new Map(), switching: new Map(), acceso: new Map() };
    // movidasPorCapa lista las tarjetas que la persona reubicó; solo esas se recuerdan entre visitas.
    const movidasPorCapa = { fisica: new Set(), switching: new Set(), acceso: new Set() };
    // raizPorCapa recuerda la tarjeta central de cada capa para abrir la vista centrada en ella.
    const raizPorCapa = { fisica: null, switching: null, acceso: null };
    // escalaInicialMinima evita abrir el mapa tan alejado que el texto no se lea; subirla acerca la vista inicial.
    const escalaInicialMinima = 0.5;
    // capaDispuesta indica si la capa ya recibió su disposición automática inicial.
    const capaDispuesta = { fisica: false, switching: false, acceso: false };
    // modoDistribucion define la disposición inicial jerárquica, con cada nivel a la derecha de su padre.
    let modoDistribucion = "filas";
    // tarjetasContraidas son las tarjetas que solo muestran su cabecera; contraerlas las hace mucho más pequeñas.
    const tarjetasContraidas = new Set();
    // gruposColapsados guarda, por capa, las tarjetas cuyos equipos dependientes están ocultos.
    const gruposColapsados = { fisica: new Set(), switching: new Set(), acceso: new Set() };
    // arbolActual indica qué tarjetas cuelgan de cuál; se usa para saber qué oculta colapsar un grupo.
    let arbolActual = new Map();
    // tamanosNodos conserva el tamaño medido de cada tarjeta visible; sirve para ubicar los extremos de las líneas.
    const tamanosNodos = new Map();
    // registrosEnlaces son las conexiones dibujadas ahora, con sus elementos SVG y anclas.
    let registrosEnlaces = [];
    // registrosPorNodo permite redibujar solo las líneas de la tarjeta que se mueve.
    const registrosPorNodo = new Map();
    // identificadorSeleccionado es la tarjeta resaltada; nulo indica que no hay selección.
    let identificadorSeleccionado = null;
    // profundidadApilado sube con cada tarjeta tocada para dejarla por encima de las demás.
    let profundidadApilado = 10;
    // tarjetasPendientes reúne las tarjetas cuyas líneas hay que recalcular en el próximo cuadro de animación.
    const tarjetasPendientes = new Set();
    // cuadroPendiente evita programar varios recálculos en el mismo cuadro de animación.
    let cuadroPendiente = 0;
    // temporizadorGuardado agrupa varios movimientos seguidos en una sola escritura de almacenamiento.
    let temporizadorGuardado = 0;
    // vistaAntesDeImprimir recuerda la vista para restaurarla tras cerrar el diálogo de impresión.
    let vistaAntesDeImprimir = null;

    /**
     * Normaliza texto del filtro para comparar nombres con o sin tildes.
     * @param {string} texto Valor a normalizar; cambiarlo produce otro texto de comparación.
     * @returns {string} Texto en minúsculas y sin marcas diacríticas.
     */
    const normalizarTexto = texto => texto.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("es");

    /**
     * Restringe un número a un intervalo.
     * @param {number} valor Número de entrada; fuera del rango se acerca al límite más próximo.
     * @param {number} minimo Límite inferior permitido.
     * @param {number} maximo Límite superior permitido.
     * @returns {number} El valor dentro de [minimo, maximo].
     */
    const limitar = (valor, minimo, maximo) => Math.min(maximo, Math.max(minimo, valor));

    /**
     * Devuelve las tarjetas que están visibles con los filtros actuales.
     * @returns {HTMLElement[]} Tarjetas sin el atributo hidden.
     */
    const listarNodosVisibles = () => nodosTopologia.filter(nodo => !nodo.hidden);

    /**
     * Obtiene la posición actual de una tarjeta en la capa activa.
     * @param {HTMLElement} nodo Tarjeta consultada; cambiarla devuelve la posición de otra ficha.
     * @returns {{x:number,y:number}} Coordenadas del lienzo; (0,0) si aún no se ubicó.
     */
    const obtenerPosicion = nodo =>
        posicionesPorCapa[modoCapaActual].get(nodo.dataset.identificadorDispositivo) || { x: 0, y: 0 };

    /**
     * Devuelve el tamaño medido de una tarjeta, o una estimación si todavía no se midió.
     * @param {HTMLElement} nodo Tarjeta consultada.
     * @returns {{ancho:number,alto:number}} Tamaño en px del lienzo.
     */
    const obtenerTamano = nodo => {
        const medido = tamanosNodos.get(nodo);
        if (medido && medido.ancho > 0) return medido;
        return { ancho: nodo.offsetWidth || 460, alto: nodo.offsetHeight || 220 };
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
     * Lista las conexiones de la capa activa cuyos dos extremos están visibles.
     * No crea relaciones: solo filtra las que el servidor documentó.
     * @returns {Array<{enlace:HTMLElement,idOrigen:string,idDestino:string,nodoOrigen:HTMLElement,nodoDestino:HTMLElement}>}
     */
    const listarEnlacesVisibles = () => {
        // visibles acumula las conexiones dibujables con sus tarjetas ya resueltas.
        const visibles = [];
        datosEnlaces.forEach(enlace => {
            if (!perteneceALaCapaActual(enlace) || esParcialDuplicada(enlace)) {
                return;
            }
            const nodoOrigen = nodosPorId.get(enlace.dataset.equipoOrigen);
            const nodoDestino = nodosPorId.get(enlace.dataset.equipoDestino);
            if (!nodoOrigen || !nodoDestino || nodoOrigen === nodoDestino || nodoOrigen.hidden || nodoDestino.hidden) {
                return;
            }
            visibles.push({
                enlace,
                idOrigen: enlace.dataset.equipoOrigen,
                idDestino: enlace.dataset.equipoDestino,
                nodoOrigen,
                nodoDestino
            });
        });
        return visibles;
    };

    // ---------------------------------------------------------------------------------------------
    // Vista: desplazamiento y zoom del lienzo
    // ---------------------------------------------------------------------------------------------

    /**
     * Aplica la vista actual al lienzo, al fondo punteado y a la etiqueta de zoom.
     * Cambiar vista.x/y desplaza el mapa; cambiar vista.escala lo acerca o aleja.
     */
    const aplicarVista = () => {
        lienzoTopologia.style.transform = `translate(${vista.x}px, ${vista.y}px) scale(${vista.escala})`;
        mapaTopologia.style.backgroundPosition = `${vista.x}px ${vista.y}px`;
        mapaTopologia.style.backgroundSize = `${26 * vista.escala}px ${26 * vista.escala}px`;
        if (etiquetaZoom) {
            etiquetaZoom.textContent = `${Math.round(vista.escala * 100)} %`;
        }
        if (controlZoom) {
            controlZoom.value = String(Math.round(vista.escala * 100));
        }
    };

    /**
     * Cambia el zoom manteniendo fijo el punto del lienzo que hay bajo el centro indicado.
     * @param {number} factor Multiplicador; mayor que 1 acerca y menor que 1 aleja.
     * @param {number} centroX Coordenada X del punto fijo, relativa a la ventana del mapa.
     * @param {number} centroY Coordenada Y del punto fijo, relativa a la ventana del mapa.
     */
    const cambiarZoom = (factor, centroX, centroY) => {
        // escalaNueva respeta los límites para evitar perder el mapa por un zoom extremo.
        const escalaNueva = limitar(vista.escala * factor, escalaMinima, escalaMaxima);
        // razonReal es el cambio efectivo después de aplicar los límites.
        const razonReal = escalaNueva / vista.escala;
        vista.x = centroX - (centroX - vista.x) * razonReal;
        vista.y = centroY - (centroY - vista.y) * razonReal;
        vista.escala = escalaNueva;
        aplicarVista();
    };

    /**
     * Encaja todas las tarjetas visibles dentro de la ventana, sin acercar más de 100 %.
     * @param {number} [escalaMinimaPermitida] Zoom mínimo al encajar; si el conjunto no cabe, se alinea arriba a la izquierda.
     * @param {number} [anchoVentana] Ancho de la ventana; por defecto el real. Cambiarlo ajusta la vista a otro tamaño (impresión).
     * @param {number} [altoVentana] Alto de la ventana; por defecto el real.
     */
    const ajustarVista = (escalaMinimaPermitida = escalaMinima, anchoVentana = mapaTopologia.clientWidth, altoVentana = mapaTopologia.clientHeight, permitirAmpliacion = false) => {
        const visibles = listarNodosVisibles();
        if (visibles.length === 0) {
            vista.x = 0;
            vista.y = 0;
            vista.escala = 1;
            aplicarVista();
            return;
        }
        // límites de la caja que encierra todas las tarjetas visibles.
        let minimoX = Infinity, minimoY = Infinity, maximoX = -Infinity, maximoY = -Infinity;
        visibles.forEach(nodo => {
            const posicion = obtenerPosicion(nodo);
            const tamano = obtenerTamano(nodo);
            minimoX = Math.min(minimoX, posicion.x);
            minimoY = Math.min(minimoY, posicion.y);
            maximoX = Math.max(maximoX, posicion.x + tamano.ancho);
            maximoY = Math.max(maximoY, posicion.y + tamano.alto);
        });
        // margen deja aire alrededor de las tarjetas; aumentarlo aleja el borde de la ventana.
        const margen = 40;
        // anchoCaja y altoCaja son el tamaño total del conjunto de tarjetas.
        const anchoCaja = maximoX - minimoX;
        const altoCaja = maximoY - minimoY;
        // escalaDeEncaje aprovecha toda la zona; permitirAmpliacion quita el tope de 100 % solo al imprimir.
        const escalaDeEncaje = Math.min(
            (anchoVentana - margen * 2) / anchoCaja,
            (altoVentana - margen * 2) / altoCaja,
            permitirAmpliacion ? 4 : 1);
        vista.escala = limitar(escalaDeEncaje, escalaMinimaPermitida, permitirAmpliacion ? 4 : escalaMaxima);
        // Si el conjunto cabe se centra; si no, se alinea arriba a la izquierda para empezar a leer desde ahí.
        vista.x = anchoCaja * vista.escala + margen * 2 <= anchoVentana
            ? (anchoVentana - anchoCaja * vista.escala) / 2 - minimoX * vista.escala
            : margen - minimoX * vista.escala;
        vista.y = altoCaja * vista.escala + margen * 2 <= altoVentana
            ? (altoVentana - altoCaja * vista.escala) / 2 - minimoY * vista.escala
            : margen - minimoY * vista.escala;
        aplicarVista();
    };

    /**
     * Abre la vista en un zoom legible: si todo cabe con al menos el zoom inicial mínimo se encaja todo;
     * si no, se centra verticalmente la tarjeta principal de la capa, con su borde izquierdo en la ventana.
     */
    const vistaInicial = () => {
        // nodoRaiz es la tarjeta central de la capa; sin ella se encaja desde la esquina superior izquierda.
        const nodoRaiz = nodosPorId.get(raizPorCapa[modoCapaActual] || "");
        ajustarVista(escalaInicialMinima);
        if (!nodoRaiz || nodoRaiz.hidden || vista.escala > escalaInicialMinima) return;
        const posicionRaiz = obtenerPosicion(nodoRaiz);
        const tamanoRaiz = obtenerTamano(nodoRaiz);
        vista.x = 40 - posicionRaiz.x * vista.escala;
        vista.y = mapaTopologia.clientHeight / 2 - (posicionRaiz.y + tamanoRaiz.alto / 2) * vista.escala;
        aplicarVista();
    };

    // ---------------------------------------------------------------------------------------------
    // Posiciones: persistencia y disposición automática
    // ---------------------------------------------------------------------------------------------

    /**
     * Recupera del navegador las tarjetas que la persona movió en visitas anteriores.
     * Si el almacenamiento no está disponible o el contenido no es válido, se ignora sin afectar al mapa.
     */
    const leerPosicionesGuardadas = () => {
        try {
            const contenidoCrudo = window.localStorage.getItem(claveAlmacenamiento);
            if (!contenidoCrudo) return;
            const datosGuardados = JSON.parse(contenidoCrudo);
            Object.keys(posicionesPorCapa).forEach(capa => {
                const guardadasCapa = datosGuardados?.[capa];
                if (!guardadasCapa || typeof guardadasCapa !== "object") return;
                Object.entries(guardadasCapa).forEach(([identificador, par]) => {
                    if (Array.isArray(par) && Number.isFinite(par[0]) && Number.isFinite(par[1])) {
                        posicionesPorCapa[capa].set(identificador, { x: par[0], y: par[1] });
                        movidasPorCapa[capa].add(identificador);
                    }
                });
            });
        } catch {
            // Sin almacenamiento disponible el mapa funciona igual; solo no recuerda las posiciones.
        }
    };

    /**
     * Guarda las tarjetas movidas manualmente, agrupando movimientos consecutivos.
     * Aumentar la espera reduce escrituras; reducirla guarda más rápido.
     */
    const programarGuardadoPosiciones = () => {
        window.clearTimeout(temporizadorGuardado);
        temporizadorGuardado = window.setTimeout(() => {
            try {
                const paraGuardar = {};
                Object.keys(posicionesPorCapa).forEach(capa => {
                    paraGuardar[capa] = {};
                    movidasPorCapa[capa].forEach(identificador => {
                        const posicion = posicionesPorCapa[capa].get(identificador);
                        if (posicion) paraGuardar[capa][identificador] = [Math.round(posicion.x), Math.round(posicion.y)];
                    });
                });
                window.localStorage.setItem(claveAlmacenamiento, JSON.stringify(paraGuardar));
            } catch {
                // Si el navegador bloquea el almacenamiento, las posiciones valen solo para esta visita.
            }
        }, 250);
    };

    /**
     * Agrupa las tarjetas en un árbol de recorrido por componente conectado. La raíz de cada componente es su
     * tarjeta más central (menor distancia máxima al resto); de ella cuelgan, por niveles, las demás.
     * No crea ni infiere conexiones: usa solo las ya documentadas y visibles.
     * @param {HTMLElement[]} nodosParaDisponer Tarjetas a agrupar; cambiar el conjunto cambia los árboles.
     * @param {Array} enlacesParaDisponer Conexiones visibles; sin ellas todas las tarjetas quedan aisladas.
     * @returns {{arboles:Array<{raiz:string,hijosDe:Map<string,string[]>}>,aislados:HTMLElement[],hijosGlobal:Map<string,string[]>,nodoPorId:Map<string,HTMLElement>,compararPorNombre:Function}}
     */
    const calcularArbol = (nodosParaDisponer, enlacesParaDisponer) => {
        // vecinos asocia cada tarjeta con las tarjetas a las que se conecta.
        const vecinos = new Map(nodosParaDisponer.map(nodo => [nodo.dataset.identificadorDispositivo, new Set()]));
        enlacesParaDisponer.forEach(registro => {
            if (vecinos.has(registro.idOrigen) && vecinos.has(registro.idDestino)) {
                vecinos.get(registro.idOrigen).add(registro.idDestino);
                vecinos.get(registro.idDestino).add(registro.idOrigen);
            }
        });
        // nodoPorId permite recuperar tarjeta y nombre a partir del ID.
        const nodoPorId = new Map(nodosParaDisponer.map(nodo => [nodo.dataset.identificadorDispositivo, nodo]));
        // visitados evita asignar una tarjeta a dos componentes.
        const visitados = new Set();
        // componentes agrupa tarjetas conectadas entre sí; los aislados se tratan aparte.
        const componentes = [];
        // aislados son tarjetas sin conexión visible; se acomodan en una cuadrícula al final.
        const aislados = [];
        nodosParaDisponer.forEach(nodo => {
            const identificador = nodo.dataset.identificadorDispositivo;
            if (visitados.has(identificador)) return;
            if (vecinos.get(identificador).size === 0) {
                visitados.add(identificador);
                aislados.push(nodo);
                return;
            }
            // pendientes y componente recorren el grafo en anchura desde esta tarjeta.
            const pendientes = [identificador];
            const componente = [];
            visitados.add(identificador);
            while (pendientes.length > 0) {
                const actual = pendientes.shift();
                componente.push(actual);
                vecinos.get(actual).forEach(vecino => {
                    if (!visitados.has(vecino)) {
                        visitados.add(vecino);
                        pendientes.push(vecino);
                    }
                });
            }
            componentes.push(componente);
        });
        // Los componentes más grandes se ubican primero.
        componentes.sort((a, b) => b.length - a.length);

        /**
         * Recorre en anchura desde una raíz y devuelve la distancia y el padre de cada tarjeta alcanzable.
         * @param {string} raiz ID de la tarjeta de partida; cambiarlo cambia las distancias y el árbol.
         * @returns {{distancias:Map<string,number>,padres:Map<string,string>}} Distancia y padre por tarjeta.
         */
        const recorrerDesde = raiz => {
            const distancias = new Map([[raiz, 0]]);
            const padres = new Map();
            const orden = [raiz];
            for (let indice = 0; indice < orden.length; indice++) {
                vecinos.get(orden[indice]).forEach(vecino => {
                    if (!distancias.has(vecino)) {
                        distancias.set(vecino, distancias.get(orden[indice]) + 1);
                        padres.set(vecino, orden[indice]);
                        orden.push(vecino);
                    }
                });
            }
            return { distancias, padres };
        };

        /**
         * Compara tarjetas por nombre con orden natural (PC2 antes que PC10).
         * @param {string} a ID de la primera tarjeta.
         * @param {string} b ID de la segunda tarjeta.
         * @returns {number} Negativo si a va antes que b.
         */
        const compararPorNombre = (a, b) => (nodoPorId.get(a).dataset.dispositivoNombre || "").localeCompare(
            nodoPorId.get(b).dataset.dispositivoNombre || "", "es", { numeric: true });

        // hijosGlobal une los hijos de todos los componentes; sirve para saber qué cuelga de cada tarjeta.
        const hijosGlobal = new Map();
        const arboles = componentes.map(componente => {
            // raiz es la tarjeta más central: la de menor excentricidad; a igualdad, la de más conexiones y luego un switch.
            let raiz = componente[0];
            let mejorPuntaje = null;
            componente.forEach(candidato => {
                const excentricidad = Math.max(...recorrerDesde(candidato).distancias.values());
                const grado = vecinos.get(candidato).size;
                const esSwitch = normalizarTexto(nodoPorId.get(candidato).dataset.dispositivoTipo || "").includes("switch") ? 1 : 0;
                const puntaje = [excentricidad, -grado, -esSwitch];
                if (!mejorPuntaje || puntaje[0] < mejorPuntaje[0]
                    || (puntaje[0] === mejorPuntaje[0] && (puntaje[1] < mejorPuntaje[1]
                        || (puntaje[1] === mejorPuntaje[1] && puntaje[2] < mejorPuntaje[2])))) {
                    mejorPuntaje = puntaje;
                    raiz = candidato;
                }
            });
            // hijosDe es el árbol de recorrido: cada tarjeta cuelga del primer vecino que la alcanzó.
            const hijosDe = new Map();
            recorrerDesde(raiz).padres.forEach((padre, hijo) => {
                if (!hijosDe.has(padre)) hijosDe.set(padre, []);
                hijosDe.get(padre).push(hijo);
            });
            hijosDe.forEach((hijos, padre) => {
                hijos.sort(compararPorNombre);
                hijosGlobal.set(padre, hijos);
            });
            return { raiz, hijosDe };
        });
        return { arboles, aislados, hijosGlobal, nodoPorId, compararPorNombre };
    };

    /**
     * Reparte tarjetas en filas que se envuelven al llegar a un ancho objetivo, con relación de aspecto similar a la ventana.
     * @param {HTMLElement[]} nodosParaDisponer Tarjetas a repartir; el orden recibido se conserva.
     * @param {number} xInicial Coordenada X donde empieza cada fila.
     * @param {number} yInicial Coordenada Y de la primera fila.
     * @param {Map<string,{x:number,y:number}>} resultado Mapa donde se anotan las posiciones.
     * @returns {number} Coordenada Y del borde inferior de lo repartido.
     */
    const repartirEnFilas = (nodosParaDisponer, xInicial, yInicial, resultado) => {
        if (nodosParaDisponer.length === 0) return yInicial;
        // relacionVentana es ancho/alto de la ventana; el reparto intenta parecerse a ella para que todo se vea a la vez.
        const relacionVentana = Math.max(0.8, mapaTopologia.clientWidth / Math.max(1, mapaTopologia.clientHeight));
        // areaTotal es la superficie aproximada que ocupan las tarjetas con su separación.
        const areaTotal = nodosParaDisponer.reduce((suma, nodo) => {
            const tamano = obtenerTamano(nodo);
            return suma + (tamano.ancho + separacionVertical) * (tamano.alto + separacionVertical);
        }, 0);
        // anchoObjetivo es el ancho a partir del cual la fila se envuelve; nunca menor que la tarjeta más ancha.
        const anchoObjetivo = Math.max(
            Math.sqrt(areaTotal * relacionVentana),
            ...nodosParaDisponer.map(nodo => obtenerTamano(nodo).ancho));
        let cursorX = 0;
        let cursorY = yInicial;
        let altoFila = 0;
        nodosParaDisponer.forEach(nodo => {
            const tamano = obtenerTamano(nodo);
            if (cursorX > 0 && cursorX + tamano.ancho > anchoObjetivo) {
                cursorX = 0;
                cursorY += altoFila + separacionVertical;
                altoFila = 0;
            }
            resultado.set(nodo.dataset.identificadorDispositivo, { x: xInicial + cursorX, y: cursorY });
            cursorX += tamano.ancho + separacionVertical;
            altoFila = Math.max(altoFila, tamano.alto);
        });
        return cursorY + altoFila;
    };

    /**
     * Calcula una disposición automática de tarjetas según el modo elegido:
     * - mosaico: clústeres (equipo central a la izquierda; cada switch con sus equipos a su derecha) repartidos
     *   en varias columnas para que el esquema completo quepa en una pantalla;
     * - filas: los mismos clústeres apilados en una sola columna;
     * - cuadricula: todas las tarjetas en filas ordenadas por área, sin considerar las conexiones.
     * Los equipos sin conexión visible van siempre al final. No crea ni infiere conexiones.
     * @param {HTMLElement[]} nodosParaDisponer Tarjetas a ubicar; cambiar el conjunto cambia el resultado.
     * @param {Array} enlacesParaDisponer Conexiones visibles; sin ellas todas las tarjetas se tratan como aisladas.
     * @param {string} modo Uno de mosaico, filas o cuadricula; un valor desconocido se trata como mosaico.
     * @returns {Map<string,{x:number,y:number}>} Posición propuesta por ID de ficha, con origen en (0,0); la propiedad raizPrincipal indica la tarjeta central del grupo más grande.
     */
    const calcularDisposicion = (nodosParaDisponer, enlacesParaDisponer, modo = "mosaico") => {
        // resultado acumula la posición propuesta por tarjeta; raizPrincipal es la tarjeta central del componente más grande.
        const resultado = new Map();
        resultado.raizPrincipal = null;

        if (modo === "cuadricula") {
            // ordenadas agrupa por área y deja primero los switches de cada área.
            const ordenadas = [...nodosParaDisponer].sort((a, b) => {
                const porArea = (a.dataset.dispositivoArea || "").localeCompare(b.dataset.dispositivoArea || "", "es", { numeric: true });
                if (porArea !== 0) return porArea;
                const esSwitchA = normalizarTexto(a.dataset.dispositivoTipo || "").includes("switch") ? 0 : 1;
                const esSwitchB = normalizarTexto(b.dataset.dispositivoTipo || "").includes("switch") ? 0 : 1;
                return esSwitchA - esSwitchB
                    || (a.dataset.dispositivoNombre || "").localeCompare(b.dataset.dispositivoNombre || "", "es", { numeric: true });
            });
            repartirEnFilas(ordenadas, 0, 0, resultado);
            return resultado;
        }

        const { arboles, aislados, nodoPorId, compararPorNombre } = calcularArbol(nodosParaDisponer, enlacesParaDisponer);

        /**
         * Acomoda tarjetas sin hijos en una cuadrícula compacta de hasta tres columnas.
         * @param {string[]} identificadoresHojas Tarjetas a apilar; más elementos agregan filas y, hasta tres, columnas.
         * @returns {{ancho:number,alto:number,elementos:Array<{id:string,dx:number,dy:number}>}} Bloque con posiciones relativas.
         */
        const construirCuadriculaHojas = identificadoresHojas => {
            // columnasHojas crece con la cantidad de tarjetas para que el bloque no sea demasiado alto.
            const columnasHojas = Math.min(3, Math.ceil(identificadoresHojas.length / 6));
            const filasHojas = Math.ceil(identificadoresHojas.length / columnasHojas);
            const elementos = [];
            let cursorX = 0;
            let altoMaximo = 0;
            for (let columna = 0; columna < columnasHojas; columna++) {
                const grupoColumna = identificadoresHojas.slice(columna * filasHojas, (columna + 1) * filasHojas);
                if (grupoColumna.length === 0) continue;
                let cursorY = 0;
                let anchoColumna = 0;
                grupoColumna.forEach(identificador => {
                    const tamano = obtenerTamano(nodoPorId.get(identificador));
                    elementos.push({ id: identificador, dx: cursorX, dy: cursorY });
                    cursorY += tamano.alto + separacionVertical;
                    anchoColumna = Math.max(anchoColumna, tamano.ancho);
                });
                altoMaximo = Math.max(altoMaximo, cursorY - separacionVertical);
                cursorX += anchoColumna + separacionSubcolumnas;
            }
            return { ancho: cursorX - separacionSubcolumnas, alto: altoMaximo, elementos };
        };

        /**
         * Reparte bloques en k columnas, siempre en la más baja, para equilibrar alturas.
         * @param {Array} bloques Bloques a repartir, en el orden recibido.
         * @param {number} cantidadColumnas Cantidad de columnas; mayor reparte más ancho y menos alto.
         * @returns {Array<{bloques:Array,alto:number,ancho:number}>} Columnas no vacías.
         */
        const repartirBloquesEnColumnas = (bloques, cantidadColumnas) => {
            const columnas = Array.from({ length: cantidadColumnas }, () => ({ bloques: [], alto: 0, ancho: 0 }));
            bloques.forEach(bloque => {
                const columnaMasBaja = columnas.reduce((menor, actual) => actual.alto < menor.alto ? actual : menor);
                columnaMasBaja.alto += (columnaMasBaja.bloques.length > 0 ? separacionVertical : 0) + bloque.alto;
                columnaMasBaja.ancho = Math.max(columnaMasBaja.ancho, bloque.ancho);
                columnaMasBaja.bloques.push(bloque);
            });
            return columnas.filter(columna => columna.bloques.length > 0);
        };

        /**
         * Construye el bloque de una tarjeta y todo lo que cuelga de ella: la tarjeta queda a la izquierda,
         * centrada respecto de sus hijos, que se apilan a su derecha (cada hijo con su propio bloque).
         * Si es la raíz y el modo es mosaico, sus bloques hijos se reparten en varias columnas.
         * @param {string} identificador Tarjeta raíz del bloque.
         * @param {Map<string,string[]>} hijosDe Hijos de cada tarjeta en el árbol de recorrido.
         * @param {boolean} esRaiz Verdadero para la raíz del componente.
         * @returns {{ancho:number,alto:number,elementos:Array<{id:string,dx:number,dy:number}>}} Bloque con posiciones relativas.
         */
        const construirBloque = (identificador, hijosDe, esRaiz) => {
            const tamano = obtenerTamano(nodoPorId.get(identificador));
            const hijos = hijosDe.get(identificador) || [];
            if (hijos.length === 0) {
                return { ancho: tamano.ancho, alto: tamano.alto, elementos: [{ id: identificador, dx: 0, dy: 0 }] };
            }
            // hojas no tienen hijos y se agrupan en cuadrícula; internos tienen su propio bloque.
            const hojas = hijos.filter(hijo => (hijosDe.get(hijo) || []).length === 0).sort(compararPorNombre);
            const internos = hijos.filter(hijo => (hijosDe.get(hijo) || []).length > 0).sort(compararPorNombre);
            const bloquesHijos = internos.map(hijo => construirBloque(hijo, hijosDe, false));
            if (hojas.length > 0) bloquesHijos.push(construirCuadriculaHojas(hojas));
            // xPila deja entre el padre y sus hijos el pasillo por donde se ven las líneas.
            const xPila = tamano.ancho + separacionNiveles;

            // Cantidad de columnas de clústeres: en mosaico se elige la que acerque el conjunto a la forma de la ventana.
            let cantidadColumnas = 1;
            if (esRaiz && modo !== "filas" && bloquesHijos.length > 1) {
                const relacionVentana = Math.max(0.8, mapaTopologia.clientWidth / Math.max(1, mapaTopologia.clientHeight));
                let mejorError = Infinity;
                for (let candidata = 1; candidata <= Math.min(10, bloquesHijos.length); candidata++) {
                    const columnasCandidatas = repartirBloquesEnColumnas(bloquesHijos, candidata);
                    const anchoTotal = xPila + columnasCandidatas.reduce((suma, columna) => suma + columna.ancho, 0)
                        + separacionSubcolumnas * (columnasCandidatas.length - 1);
                    const altoTotal = Math.max(tamano.alto, ...columnasCandidatas.map(columna => columna.alto));
                    const error = Math.abs(Math.log(anchoTotal / altoTotal) - Math.log(relacionVentana));
                    if (error < mejorError) {
                        mejorError = error;
                        cantidadColumnas = candidata;
                    }
                }
            }
            const columnasHijos = repartirBloquesEnColumnas(bloquesHijos, cantidadColumnas);
            const altoBloque = Math.max(tamano.alto, ...columnasHijos.map(columna => columna.alto));
            const elementos = [{ id: identificador, dx: 0, dy: (altoBloque - tamano.alto) / 2 }];
            let cursorX = xPila;
            columnasHijos.forEach(columna => {
                let cursorY = columnasHijos.length === 1 ? (altoBloque - columna.alto) / 2 : 0;
                columna.bloques.forEach(bloque => {
                    bloque.elementos.forEach(elemento => elementos.push({ id: elemento.id, dx: cursorX + elemento.dx, dy: cursorY + elemento.dy }));
                    cursorY += bloque.alto + separacionVertical;
                });
                cursorX += columna.ancho + separacionSubcolumnas;
            });
            return { ancho: cursorX - separacionSubcolumnas, alto: altoBloque, elementos };
        };

        // desplazamientoY separa verticalmente un componente del siguiente.
        let desplazamientoY = 0;
        arboles.forEach(arbol => {
            const bloque = construirBloque(arbol.raiz, arbol.hijosDe, true);
            bloque.elementos.forEach(elemento => resultado.set(elemento.id, { x: elemento.dx, y: desplazamientoY + elemento.dy }));
            if (resultado.raizPrincipal === null) resultado.raizPrincipal = arbol.raiz;
            desplazamientoY += bloque.alto + 110;
        });

        // Los equipos sin conexión visible se reparten en filas debajo del resto.
        repartirEnFilas(aislados, 0, desplazamientoY, resultado);
        return resultado;
    };

    /**
     * Mueve una tarjeta a unas coordenadas del lienzo y registra su posición en la capa activa.
     * @param {HTMLElement} nodo Tarjeta a mover.
     * @param {number} x Coordenada X de la esquina superior izquierda.
     * @param {number} y Coordenada Y de la esquina superior izquierda.
     */
    const colocarNodo = (nodo, x, y) => {
        posicionesPorCapa[modoCapaActual].set(nodo.dataset.identificadorDispositivo, { x, y });
        nodo.style.transform = `translate(${x}px, ${y}px)`;
    };

    /**
     * Refleja en pantalla las posiciones guardadas en memoria para las tarjetas visibles.
     */
    const pintarPosiciones = () => {
        listarNodosVisibles().forEach(nodo => {
            const posicion = obtenerPosicion(nodo);
            nodo.style.transform = `translate(${posicion.x}px, ${posicion.y}px)`;
        });
    };

    /**
     * Mide las tarjetas visibles para que la disposición y las líneas usen su tamaño real.
     */
    const medirNodosVisibles = () => {
        listarNodosVisibles().forEach(nodo => {
            tamanosNodos.set(nodo, { ancho: nodo.offsetWidth, alto: nodo.offsetHeight });
        });
    };

    /**
     * Dispone la capa activa: la primera vez calcula la disposición completa respetando las tarjetas
     * movidas por la persona; en adelante solo ubica las tarjetas visibles que todavía no tienen lugar
     * (por ejemplo, tras quitar un filtro), debajo del conjunto ya ubicado.
     * @param {Array} enlacesVisibles Conexiones visibles usadas para ordenar las tarjetas.
     * @returns {boolean} Verdadero si se hizo una disposición completa nueva.
     */
    const asegurarDisposicion = enlacesVisibles => {
        medirNodosVisibles();
        // visibles son las tarjetas que hay que ubicar o mantener en la capa.
        const visibles = listarNodosVisibles();
        // posicionesCapa conserva las posiciones de la capa activa.
        const posicionesCapa = posicionesPorCapa[modoCapaActual];
        if (!capaDispuesta[modoCapaActual]) {
            // propuesta es la disposición automática; las tarjetas movidas por la persona conservan su lugar.
            const propuesta = calcularDisposicion(visibles, enlacesVisibles, modoDistribucion);
            visibles.forEach(nodo => {
                const identificador = nodo.dataset.identificadorDispositivo;
                if (!movidasPorCapa[modoCapaActual].has(identificador)) {
                    const lugar = propuesta.get(identificador) || { x: 0, y: 0 };
                    posicionesCapa.set(identificador, lugar);
                }
            });
            raizPorCapa[modoCapaActual] = propuesta.raizPrincipal;
            capaDispuesta[modoCapaActual] = true;
            pintarPosiciones();
            return true;
        }
        // sinLugar son las tarjetas visibles que nunca se ubicaron en esta capa.
        const sinLugar = visibles.filter(nodo => !posicionesCapa.has(nodo.dataset.identificadorDispositivo));
        if (sinLugar.length > 0) {
            // baseY es el borde inferior del conjunto ya ubicado; las nuevas tarjetas se ubican debajo.
            let baseY = 0;
            visibles.forEach(nodo => {
                if (posicionesCapa.has(nodo.dataset.identificadorDispositivo)) {
                    baseY = Math.max(baseY, posicionesCapa.get(nodo.dataset.identificadorDispositivo).y + obtenerTamano(nodo).alto);
                }
            });
            const propuesta = calcularDisposicion(sinLugar, enlacesVisibles, modoDistribucion);
            sinLugar.forEach(nodo => {
                const lugar = propuesta.get(nodo.dataset.identificadorDispositivo) || { x: 0, y: 0 };
                posicionesCapa.set(nodo.dataset.identificadorDispositivo, { x: lugar.x, y: lugar.y + (baseY > 0 ? baseY + 110 : 0) });
            });
        }
        pintarPosiciones();
        return false;
    };

    // ---------------------------------------------------------------------------------------------
    // Líneas: creación, geometría y resaltado
    // ---------------------------------------------------------------------------------------------

    /**
     * Busca el puerto de una tarjeta por su ID de interfaz.
     * @param {HTMLElement} nodo Tarjeta donde se busca; cambiarla cambia el equipo.
     * @param {string|undefined} identificadorPuerto ID de interfaz; vacío indica que la fuente no documenta ese puerto.
     * @returns {HTMLElement|null} Elemento del puerto o nulo si no se documenta o no está en la tarjeta.
     */
    const buscarPuerto = (nodo, identificadorPuerto) => {
        if (!identificadorPuerto) return null;
        return nodo.querySelector(`[data-identificador-interfaz="${CSS.escape(identificadorPuerto)}"]`);
    };

    /**
     * Obtiene el texto visible de un puerto para rotular el extremo de una línea.
     * @param {HTMLElement|null} puerto Elemento del puerto; nulo devuelve vacío.
     * @returns {string} Etiqueta como aparece en la documentación.
     */
    const etiquetaDePuerto = puerto => {
        if (!puerto) return "";
        return (puerto.dataset.etiquetaPuerto || puerto.querySelector("span")?.textContent || "").trim();
    };

    /**
     * Mide dónde está un puerto dentro de su tarjeta, en coordenadas del lienzo.
     * Se usa para que la línea salga a la altura (o columna) del puerto documentado.
     * @param {HTMLElement} nodo Tarjeta que contiene el puerto.
     * @param {HTMLElement|null} puerto Puerto a medir; nulo devuelve nulo.
     * @returns {{x:number,y:number}|null} Centro del puerto relativo a la esquina de la tarjeta.
     */
    const medirAncla = (nodo, puerto) => {
        if (!puerto) return null;
        const rectanguloNodo = nodo.getBoundingClientRect();
        const rectanguloPuerto = puerto.getBoundingClientRect();
        if (rectanguloPuerto.width === 0) return null;
        return {
            x: (rectanguloPuerto.left + rectanguloPuerto.width / 2 - rectanguloNodo.left) / vista.escala,
            y: (rectanguloPuerto.top + rectanguloPuerto.height / 2 - rectanguloNodo.top) / vista.escala
        };
    };

    /**
     * Crea un elemento SVG con atributos.
     * @param {string} etiqueta Nombre del elemento (path, circle, g, rect, text).
     * @param {Object<string,string>} atributos Pares atributo/valor; cambiarlos cambia el aspecto.
     * @returns {SVGElement} Elemento creado, sin insertar.
     */
    const crearElementoSvg = (etiqueta, atributos = {}) => {
        const elemento = document.createElementNS(espacioSvg, etiqueta);
        Object.entries(atributos).forEach(([nombre, valor]) => elemento.setAttribute(nombre, valor));
        return elemento;
    };

    /**
     * Crea una pastilla con el número de puerto para un extremo de línea.
     * @param {string} texto Etiqueta del puerto; vacía devuelve nulo (no se dibuja).
     * @returns {{grupo:SVGGElement,fondo:SVGRectElement,texto:SVGTextElement,ancho:number}|null}
     */
    const crearPastillaPuerto = texto => {
        if (!texto) return null;
        // anchoPastilla se estima por la cantidad de caracteres; cambiar el factor ajusta el margen lateral.
        const anchoPastilla = 10 + texto.length * 6.6;
        const grupo = crearElementoSvg("g", { class: "pastilla-puerto-enlace" });
        const fondo = crearElementoSvg("rect", { width: anchoPastilla, height: 16, rx: 4 });
        const contenido = crearElementoSvg("text", { x: anchoPastilla / 2, y: 12, "text-anchor": "middle" });
        contenido.textContent = texto;
        grupo.append(fondo, contenido);
        return { grupo, fondo, texto: contenido, ancho: anchoPastilla };
    };

    /**
     * Marca u oculta el resaltado de los puertos de los extremos de una línea.
     * @param {Object} registro Registro de la línea; sus IDs de puerto determinan qué puertos se resaltan.
     * @param {boolean} resaltar Verdadero marca los puertos; falso los desmarca.
     */
    const resaltarExtremos = (registro, resaltar) => {
        [registro.puertoOrigen, registro.puertoDestino].forEach(puerto => puerto?.classList.toggle("puerto-resaltado", resaltar));
        registro.ruta.classList.toggle("enlace-resaltado", resaltar);
    };

    /**
     * Calcula el trazado de una conexión según las posiciones y tamaños actuales de sus tarjetas y lo aplica.
     * La línea sale del lado que mira a la otra tarjeta, a la altura del puerto documentado (o al centro
     * si ese extremo no tiene puerto), y termina con un punto y la pastilla del puerto.
     * @param {Object} registro Registro de la conexión; cambiarlo redibuja otra línea.
     */
    const actualizarGeometriaRegistro = registro => {
        const posicionOrigen = obtenerPosicion(registro.nodoOrigen);
        const posicionDestino = obtenerPosicion(registro.nodoDestino);
        const tamanoOrigen = obtenerTamano(registro.nodoOrigen);
        const tamanoDestino = obtenerTamano(registro.nodoDestino);
        // holgura es el hueco mínimo entre tarjetas para considerar que una está a un lado de la otra.
        const holgura = 24;
        // anclaOrigen y anclaDestino usan el centro de la tarjeta cuando el puerto no está documentado.
        const anclaOrigen = registro.anclaOrigen || { x: tamanoOrigen.ancho / 2, y: tamanoOrigen.alto / 2 };
        const anclaDestino = registro.anclaDestino || { x: tamanoDestino.ancho / 2, y: tamanoDestino.alto / 2 };
        // alturaSegura mantiene el punto de salida dentro del borde, lejos de las esquinas redondeadas.
        const dentro = (valor, maximo) => limitar(valor, 18, Math.max(18, maximo - 18));
        // separacionRepetidas separa visualmente varios cables entre el mismo par de tarjetas.
        const separacionRepetidas = (registro.repeticion % 5 - 2) * 9;
        let salida, llegada, ladoSalida, ladoLlegada, ruta;

        if (posicionOrigen.x + tamanoOrigen.ancho + holgura <= posicionDestino.x
            || posicionDestino.x + tamanoDestino.ancho + holgura <= posicionOrigen.x) {
            // Tarjetas una al lado de la otra: salida horizontal.
            const origenIzquierda = posicionOrigen.x < posicionDestino.x;
            ladoSalida = origenIzquierda ? "derecha" : "izquierda";
            ladoLlegada = origenIzquierda ? "izquierda" : "derecha";
            salida = {
                x: posicionOrigen.x + (origenIzquierda ? tamanoOrigen.ancho : 0),
                y: posicionOrigen.y + dentro(anclaOrigen.y, tamanoOrigen.alto) + separacionRepetidas
            };
            llegada = {
                x: posicionDestino.x + (origenIzquierda ? 0 : tamanoDestino.ancho),
                y: posicionDestino.y + dentro(anclaDestino.y, tamanoDestino.alto) + separacionRepetidas
            };
            // tension aleja los puntos de control para que la curva sea suave; aumentarla la hace más abierta.
            const tension = Math.max(60, Math.abs(llegada.x - salida.x) / 2) * (origenIzquierda ? 1 : -1);
            ruta = `M ${salida.x} ${salida.y} C ${salida.x + tension} ${salida.y}, ${llegada.x - tension} ${llegada.y}, ${llegada.x} ${llegada.y}`;
        } else if (posicionOrigen.y + tamanoOrigen.alto + holgura <= posicionDestino.y
            || posicionDestino.y + tamanoDestino.alto + holgura <= posicionOrigen.y) {
            // Tarjetas una sobre otra: salida vertical.
            const origenArriba = posicionOrigen.y < posicionDestino.y;
            ladoSalida = origenArriba ? "abajo" : "arriba";
            ladoLlegada = origenArriba ? "arriba" : "abajo";
            salida = {
                x: posicionOrigen.x + dentro(anclaOrigen.x, tamanoOrigen.ancho) + separacionRepetidas,
                y: posicionOrigen.y + (origenArriba ? tamanoOrigen.alto : 0)
            };
            llegada = {
                x: posicionDestino.x + dentro(anclaDestino.x, tamanoDestino.ancho) + separacionRepetidas,
                y: posicionDestino.y + (origenArriba ? 0 : tamanoDestino.alto)
            };
            const tension = Math.max(60, Math.abs(llegada.y - salida.y) / 2) * (origenArriba ? 1 : -1);
            ruta = `M ${salida.x} ${salida.y} C ${salida.x} ${salida.y + tension}, ${llegada.x} ${llegada.y - tension}, ${llegada.x} ${llegada.y}`;
        } else {
            // Tarjetas superpuestas: línea recta entre centros, solo como último recurso visible.
            ladoSalida = "derecha";
            ladoLlegada = "izquierda";
            salida = { x: posicionOrigen.x + tamanoOrigen.ancho / 2, y: posicionOrigen.y + tamanoOrigen.alto / 2 };
            llegada = { x: posicionDestino.x + tamanoDestino.ancho / 2, y: posicionDestino.y + tamanoDestino.alto / 2 };
            ruta = `M ${salida.x} ${salida.y} L ${llegada.x} ${llegada.y}`;
        }

        registro.ruta.setAttribute("d", ruta);
        registro.rutaSuperpuesta?.setAttribute("d", ruta);
        registro.puntoOrigen.setAttribute("cx", salida.x);
        registro.puntoOrigen.setAttribute("cy", salida.y);
        registro.puntoDestino.setAttribute("cx", llegada.x);
        registro.puntoDestino.setAttribute("cy", llegada.y);
        colocarPastilla(registro.pastillaOrigen, salida, ladoSalida);
        colocarPastilla(registro.pastillaDestino, llegada, ladoLlegada);
    };

    /**
     * Ubica la pastilla del puerto junto al punto de salida, fuera de la tarjeta.
     * @param {Object|null} pastilla Pastilla creada por crearPastillaPuerto; nulo no hace nada.
     * @param {{x:number,y:number}} punto Punto del borde de la tarjeta donde nace la línea.
     * @param {string} lado Lado por el que sale la línea: derecha, izquierda, arriba o abajo.
     */
    const colocarPastilla = (pastilla, punto, lado) => {
        if (!pastilla) return;
        // altoPastilla coincide con el alto del rectángulo creado.
        const altoPastilla = 16;
        let x = punto.x + 7;
        let y = punto.y - altoPastilla - 3;
        if (lado === "izquierda") x = punto.x - 7 - pastilla.ancho;
        if (lado === "abajo") { x = punto.x + 6; y = punto.y + 7; }
        if (lado === "arriba") { x = punto.x + 6; y = punto.y - 7 - altoPastilla; }
        pastilla.grupo.setAttribute("transform", `translate(${x} ${y})`);
    };

    /**
     * Recalcula la geometría de todas las líneas de las tarjetas indicadas (o de todas si no se indica).
     * @param {Iterable<HTMLElement>} [nodosAfectados] Tarjetas movidas; solo se redibujan sus líneas.
     */
    const actualizarGeometria = nodosAfectados => {
        // registrosAfectados evita redibujar toda la red cuando solo se mueve una tarjeta.
        const registrosAfectados = new Set();
        if (nodosAfectados) {
            for (const nodo of nodosAfectados) {
                (registrosPorNodo.get(nodo.dataset.identificadorDispositivo) || []).forEach(registro => registrosAfectados.add(registro));
            }
        } else {
            registrosEnlaces.forEach(registro => registrosAfectados.add(registro));
        }
        registrosAfectados.forEach(actualizarGeometriaRegistro);
    };

    /**
     * Pide recalcular las líneas de una tarjeta en el siguiente cuadro de animación.
     * Agrupa varios eventos de puntero seguidos para dibujar una sola vez por cuadro.
     * @param {HTMLElement} nodo Tarjeta que cambió de posición o tamaño.
     */
    const programarActualizacionEnlaces = nodo => {
        tarjetasPendientes.add(nodo);
        if (cuadroPendiente) return;
        cuadroPendiente = window.requestAnimationFrame(() => {
            cuadroPendiente = 0;
            const lote = Array.from(tarjetasPendientes);
            tarjetasPendientes.clear();
            actualizarGeometria(lote);
        });
    };

    /**
     * Crea los elementos SVG de las conexiones visibles; no inventa relaciones nuevas.
     * @param {Array} enlacesVisibles Conexiones a dibujar; cambiar la lista cambia las líneas que existen.
     */
    const construirEnlaces = enlacesVisibles => {
        capaEnlaces.replaceChildren();
        capaEnlacesActivos.replaceChildren();
        registrosEnlaces = [];
        registrosPorNodo.clear();
        // repeticionesPorPar numera los cables repetidos entre el mismo par de equipos.
        const repeticionesPorPar = new Map();

        enlacesVisibles.forEach(candidato => {
            const { enlace, idOrigen, idDestino, nodoOrigen, nodoDestino } = candidato;
            const clavePar = [idOrigen, idDestino].sort().join("-");
            const repeticion = repeticionesPorPar.get(clavePar) || 0;
            repeticionesPorPar.set(clavePar, repeticion + 1);

            const puertoOrigen = buscarPuerto(nodoOrigen, enlace.dataset.puertoOrigen);
            const puertoDestino = buscarPuerto(nodoDestino, enlace.dataset.puertoDestino);
            // capaDelEnlace da a cada capa un color propio; sin dato se toma la física.
            const capaDelEnlace = ["switching", "acceso"].includes(enlace.dataset.capaRelacional) ? enlace.dataset.capaRelacional : "fisica";
            // switchSinCotejoReciproco señala un uplink confirmado cuya tabla opuesta no permite elegir un puerto único.
            const switchSinCotejoReciproco = enlace.dataset.relacionParcial === "true"
                && enlace.dataset.capaRelacional === "switching"
                && enlace.dataset.cotejoReciproco !== "true";
            const clasesEstado = switchSinCotejoReciproco
                ? "enlace-parcial-switch"
                : enlace.dataset.confirmado === "true" ? "enlace-confirmado" : "enlace-pendiente";

            const ruta = crearElementoSvg("path", {
                class: `enlace-topologia capa-${capaDelEnlace} ${clasesEstado}`,
                tabindex: "0",
                role: "img",
                "aria-label": enlace.dataset.etiqueta
            });
            const titulo = crearElementoSvg("title");
            titulo.textContent = enlace.dataset.etiqueta;
            ruta.append(titulo);
            const puntoOrigen = crearElementoSvg("circle", { r: 4.5, class: `punto-enlace capa-${capaDelEnlace}` });
            const puntoDestino = crearElementoSvg("circle", { r: 4.5, class: `punto-enlace capa-${capaDelEnlace}` });
            const pastillaOrigen = crearPastillaPuerto(etiquetaDePuerto(puertoOrigen));
            const pastillaDestino = crearPastillaPuerto(etiquetaDePuerto(puertoDestino));
            capaEnlaces.append(ruta, puntoOrigen, puntoDestino);
            if (pastillaOrigen) capaEnlaces.append(pastillaOrigen.grupo);
            if (pastillaDestino) capaEnlaces.append(pastillaDestino.grupo);

            const registro = {
                enlace, idOrigen, idDestino, nodoOrigen, nodoDestino, repeticion, capaDelEnlace,
                puertoOrigen, puertoDestino, ruta, puntoOrigen, puntoDestino, pastillaOrigen, pastillaDestino,
                anclaOrigen: medirAncla(nodoOrigen, puertoOrigen),
                anclaDestino: medirAncla(nodoDestino, puertoDestino),
                rutaSuperpuesta: null
            };
            ruta.addEventListener("pointerenter", () => resaltarExtremos(registro, true));
            ruta.addEventListener("pointerleave", () => resaltarExtremos(registro, false));
            ruta.addEventListener("focus", () => resaltarExtremos(registro, true));
            ruta.addEventListener("blur", () => resaltarExtremos(registro, false));
            registrosEnlaces.push(registro);
            [idOrigen, idDestino].forEach(identificador => {
                if (!registrosPorNodo.has(identificador)) registrosPorNodo.set(identificador, []);
                registrosPorNodo.get(identificador).push(registro);
            });
        });
    };

    /**
     * Resalta una tarjeta y sus conexiones: las líneas activas se repiten por encima de las tarjetas
     * y el resto del mapa se atenúa. Pasar nulo quita la selección.
     * @param {string|null} identificador ID de la ficha a resaltar; nulo limpia el resaltado.
     */
    const seleccionarNodo = identificador => {
        identificadorSeleccionado = identificador;
        // vecinosSeleccionados son las tarjetas unidas a la seleccionada por una conexión visible.
        const vecinosSeleccionados = new Set();
        capaEnlacesActivos.replaceChildren();
        registrosEnlaces.forEach(registro => {
            const activo = identificador !== null && (registro.idOrigen === identificador || registro.idDestino === identificador);
            registro.ruta.classList.toggle("enlace-activo", activo);
            registro.rutaSuperpuesta = null;
            if (activo) {
                vecinosSeleccionados.add(registro.idOrigen);
                vecinosSeleccionados.add(registro.idDestino);
                registro.rutaSuperpuesta = crearElementoSvg("path", {
                    class: `enlace-topologia enlace-superpuesto capa-${registro.capaDelEnlace}`
                });
                capaEnlacesActivos.append(registro.rutaSuperpuesta);
            }
        });
        nodosTopologia.forEach(nodo => {
            const identificadorNodo = nodo.dataset.identificadorDispositivo;
            nodo.classList.toggle("nodo-seleccionado", identificador !== null && identificadorNodo === identificador);
            nodo.classList.toggle("nodo-vecino", identificador !== null && identificadorNodo !== identificador && vecinosSeleccionados.has(identificadorNodo));
        });
        mapaTopologia.classList.toggle("mapa-con-seleccion", identificador !== null);
        actualizarGeometria();
        if (estadoMapa) {
            const nodoElegido = identificador === null ? null : nodosPorId.get(identificador);
            estadoMapa.textContent = nodoElegido
                ? `${nodoElegido.dataset.dispositivoNombre} seleccionado: ${(registrosPorNodo.get(identificador) || []).length} conexiones visibles.`
                : "";
        }
    };

    // ---------------------------------------------------------------------------------------------
    // Interacción: arrastrar tarjetas, desplazar el fondo, zoom y teclado
    // ---------------------------------------------------------------------------------------------

    /**
     * Elementos dentro de una tarjeta que conservan su acción normal y no inician un arrastre.
     */
    const selectorControlesInternos = "a, button, input, select, textarea, summary, [data-puerto-topologia]";

    /**
     * Vincula el arrastre de una tarjeta: mover el puntero la reubica y sus líneas la siguen.
     * Con ratón se arrastra desde cualquier punto libre de la tarjeta; con pantalla táctil, desde su cabecera.
     * Un clic sin movimiento selecciona la tarjeta y sus conexiones.
     * @param {HTMLElement} nodo Tarjeta que se vuelve arrastrable.
     */
    const habilitarArrastreTarjeta = nodo => {
        // identificadorNodo es el ID de ficha con el que se guarda la posición.
        const identificadorNodo = nodo.dataset.identificadorDispositivo;
        // estadoArrastre conserva el origen del gesto; nulo indica que no hay arrastre en curso.
        let estadoArrastre = null;
        // umbralArrastre distingue un clic de un arrastre; aumentarlo exige mover más antes de reubicar.
        const umbralArrastre = 4;

        nodo.addEventListener("pointerdown", evento => {
            if (evento.button !== 0 || evento.target.closest(selectorControlesInternos)) return;
            if (evento.pointerType === "touch" && !evento.target.closest(".cabecera-nodo-topologia")) return;
            const posicionInicial = obtenerPosicion(nodo);
            estadoArrastre = {
                identificadorPuntero: evento.pointerId,
                inicioX: evento.clientX,
                inicioY: evento.clientY,
                xInicial: posicionInicial.x,
                yInicial: posicionInicial.y,
                movido: false
            };
            nodo.setPointerCapture(evento.pointerId);
            evento.stopPropagation();
        });

        nodo.addEventListener("pointermove", evento => {
            if (!estadoArrastre || evento.pointerId !== estadoArrastre.identificadorPuntero) return;
            // desplazamientoX/Y se dividen por el zoom para que la tarjeta siga al puntero a cualquier escala.
            const desplazamientoX = (evento.clientX - estadoArrastre.inicioX) / vista.escala;
            const desplazamientoY = (evento.clientY - estadoArrastre.inicioY) / vista.escala;
            if (!estadoArrastre.movido && Math.hypot(evento.clientX - estadoArrastre.inicioX, evento.clientY - estadoArrastre.inicioY) < umbralArrastre) {
                return;
            }
            if (!estadoArrastre.movido) {
                estadoArrastre.movido = true;
                nodo.classList.add("arrastrando");
                nodo.style.zIndex = String(++profundidadApilado);
            }
            colocarNodo(nodo, estadoArrastre.xInicial + desplazamientoX, estadoArrastre.yInicial + desplazamientoY);
            programarActualizacionEnlaces(nodo);
        });

        /**
         * Cierra el gesto: guarda la nueva posición si hubo movimiento o selecciona la tarjeta si fue un clic.
         * @param {PointerEvent} evento Evento de fin o cancelación del puntero.
         */
        const terminarArrastre = evento => {
            if (!estadoArrastre || evento.pointerId !== estadoArrastre.identificadorPuntero) return;
            const huboMovimiento = estadoArrastre.movido;
            estadoArrastre = null;
            nodo.classList.remove("arrastrando");
            if (nodo.hasPointerCapture(evento.pointerId)) nodo.releasePointerCapture(evento.pointerId);
            if (huboMovimiento) {
                movidasPorCapa[modoCapaActual].add(identificadorNodo);
                programarGuardadoPosiciones();
                // Al soltar, la línea del extremo debe seguir anclada aunque la tarjeta haya cambiado de lado.
                programarActualizacionEnlaces(nodo);
            } else if (evento.type === "pointerup") {
                nodo.style.zIndex = String(++profundidadApilado);
                seleccionarNodo(identificadorSeleccionado === identificadorNodo ? null : identificadorNodo);
            }
        };
        nodo.addEventListener("pointerup", terminarArrastre);
        nodo.addEventListener("pointercancel", terminarArrastre);

        // Teclado: con la tarjeta enfocada, las flechas la mueven (Mayús para pasos grandes) y Enter/Espacio la selecciona.
        nodo.addEventListener("keydown", evento => {
            if (evento.target !== nodo) return;
            // paso es la distancia por pulsación; aumentarlo mueve más rápido con el teclado.
            const paso = evento.shiftKey ? 40 : 8;
            const movimientos = { ArrowLeft: [-paso, 0], ArrowRight: [paso, 0], ArrowUp: [0, -paso], ArrowDown: [0, paso] };
            if (movimientos[evento.key]) {
                evento.preventDefault();
                const posicionActual = obtenerPosicion(nodo);
                colocarNodo(nodo, posicionActual.x + movimientos[evento.key][0], posicionActual.y + movimientos[evento.key][1]);
                movidasPorCapa[modoCapaActual].add(identificadorNodo);
                programarGuardadoPosiciones();
                programarActualizacionEnlaces(nodo);
            } else if (evento.key === "Enter" || evento.key === " ") {
                evento.preventDefault();
                seleccionarNodo(identificadorSeleccionado === identificadorNodo ? null : identificadorNodo);
            }
        });
    };

    // Seleccionar un puerto del frontal resalta además la conexión que lo usa.
    lienzoTopologia.addEventListener("click", evento => {
        const puertoPulsado = evento.target.closest("[data-puerto-topologia]");
        if (!puertoPulsado) return;
        // identificadorPuerto es el ID de interfaz del puerto pulsado; vacío indica una posición sin interfaz guardada.
        const identificadorPuerto = puertoPulsado.dataset.identificadorInterfaz;
        registrosEnlaces.forEach(registro => {
            const usaElPuerto = Boolean(identificadorPuerto)
                && (registro.enlace.dataset.puertoOrigen === identificadorPuerto || registro.enlace.dataset.puertoDestino === identificadorPuerto);
            registro.ruta.classList.toggle("enlace-resaltado", usaElPuerto);
        });
    });

    nodosTopologia.forEach(habilitarArrastreTarjeta);

    // Desplazar el fondo con el puntero.
    // estadoDesplazamiento guarda el gesto de desplazar el fondo; nulo indica que no hay uno activo.
    let estadoDesplazamiento = null;
    mapaTopologia.addEventListener("pointerdown", evento => {
        if (evento.button !== 0 || evento.target.closest("[data-dispositivo], .enlace-topologia")) return;
        estadoDesplazamiento = {
            identificadorPuntero: evento.pointerId,
            inicioX: evento.clientX,
            inicioY: evento.clientY,
            xInicial: vista.x,
            yInicial: vista.y,
            movido: false
        };
        mapaTopologia.setPointerCapture(evento.pointerId);
    });
    mapaTopologia.addEventListener("pointermove", evento => {
        if (!estadoDesplazamiento || evento.pointerId !== estadoDesplazamiento.identificadorPuntero) return;
        if (!estadoDesplazamiento.movido && Math.hypot(evento.clientX - estadoDesplazamiento.inicioX, evento.clientY - estadoDesplazamiento.inicioY) < 4) return;
        estadoDesplazamiento.movido = true;
        mapaTopologia.classList.add("mapa-desplazando");
        vista.x = estadoDesplazamiento.xInicial + evento.clientX - estadoDesplazamiento.inicioX;
        vista.y = estadoDesplazamiento.yInicial + evento.clientY - estadoDesplazamiento.inicioY;
        aplicarVista();
    });
    /**
     * Termina el desplazamiento del fondo; un toque sin movimiento limpia la selección.
     * @param {PointerEvent} evento Evento de fin o cancelación del puntero.
     */
    const terminarDesplazamiento = evento => {
        if (!estadoDesplazamiento || evento.pointerId !== estadoDesplazamiento.identificadorPuntero) return;
        const huboMovimiento = estadoDesplazamiento.movido;
        estadoDesplazamiento = null;
        mapaTopologia.classList.remove("mapa-desplazando");
        if (mapaTopologia.hasPointerCapture(evento.pointerId)) mapaTopologia.releasePointerCapture(evento.pointerId);
        if (!huboMovimiento && evento.type === "pointerup" && identificadorSeleccionado !== null) {
            seleccionarNodo(null);
        }
    };
    mapaTopologia.addEventListener("pointerup", terminarDesplazamiento);
    mapaTopologia.addEventListener("pointercancel", terminarDesplazamiento);

    // Rueda: Ctrl (o el gesto de pellizco del trackpad) acerca y aleja; sin Ctrl desplaza el lienzo.
    mapaTopologia.addEventListener("wheel", evento => {
        evento.preventDefault();
        if (evento.ctrlKey || evento.metaKey) {
            const rectanguloVentana = mapaTopologia.getBoundingClientRect();
            // factorZoom crece con el movimiento de la rueda; cambiar la base cambia la sensibilidad.
            const factorZoom = Math.exp(-evento.deltaY * 0.0025);
            cambiarZoom(factorZoom, evento.clientX - rectanguloVentana.left, evento.clientY - rectanguloVentana.top);
        } else {
            vista.x -= evento.deltaX;
            vista.y -= evento.deltaY;
            aplicarVista();
        }
    }, { passive: false });

    // Teclado sobre la ventana del mapa: flechas desplazan, + y - acercan, 0 encaja todo.
    mapaTopologia.addEventListener("keydown", evento => {
        if (evento.target !== mapaTopologia) return;
        // pasoDesplazamiento es la distancia en px por pulsación de flecha.
        const pasoDesplazamiento = evento.shiftKey ? 160 : 40;
        const centroX = mapaTopologia.clientWidth / 2;
        const centroY = mapaTopologia.clientHeight / 2;
        const acciones = {
            ArrowLeft: () => { vista.x += pasoDesplazamiento; aplicarVista(); },
            ArrowRight: () => { vista.x -= pasoDesplazamiento; aplicarVista(); },
            ArrowUp: () => { vista.y += pasoDesplazamiento; aplicarVista(); },
            ArrowDown: () => { vista.y -= pasoDesplazamiento; aplicarVista(); },
            "+": () => cambiarZoom(1.2, centroX, centroY),
            "=": () => cambiarZoom(1.2, centroX, centroY),
            "-": () => cambiarZoom(1 / 1.2, centroX, centroY),
            "0": () => vistaInicial()
        };
        if (acciones[evento.key]) {
            evento.preventDefault();
            acciones[evento.key]();
        }
    });

    controlZoom?.addEventListener("input", () => {
        // escalaSolicitada convierte el porcentaje del control a escala decimal.
        const escalaSolicitada = Number(controlZoom.value) / 100;
        // razonZoom ajusta la escala actual al valor elegido desde el centro del mapa.
        const razonZoom = escalaSolicitada / vista.escala;
        cambiarZoom(razonZoom, mapaTopologia.clientWidth / 2, mapaTopologia.clientHeight / 2);
    });
    botonVerTodo?.addEventListener("click", () => ajustarVista());
    /**
     * Descarta las posiciones de la capa actual y reparte de nuevo las tarjetas visibles con el modo elegido.
     * Se usa al pulsar Distribuir, al cambiar de modo y tras contraer o colapsar muchas tarjetas.
     */
    const distribuir = () => {
        movidasPorCapa[modoCapaActual].clear();
        posicionesPorCapa[modoCapaActual].clear();
        capaDispuesta[modoCapaActual] = false;
        programarGuardadoPosiciones();
        actualizarFiltros();
        vistaInicial();
    };
    botonDistribuir?.addEventListener("click", distribuir);

    /**
     * Borra posiciones y estados temporales de todas las capas y restaura el esquema jerárquico inicial.
     * Ejecutar esta acción elimina la caché de posiciones del navegador para este mapa.
     */
    const restablecerMapa = () => {
        // temporizadorGuardado cancela escrituras pendientes que podrían recuperar una posición anterior.
        window.clearTimeout(temporizadorGuardado);
        // filtroTexto y filtroArea se limpian para que el mapa vuelva a mostrar la topología completa.
        if (buscarTopologia) buscarTopologia.value = "";
        if (filtrarArea) filtrarArea.value = "";
        // casillaSoloConectados vuelve a la opción inicial que oculta fichas sin conexiones documentadas.
        if (casillaSoloConectados) casillaSoloConectados.checked = true;
        // selectorDistribucion coincide con el orden jerárquico genérico de primera carga.
        modoDistribucion = "filas";
        if (selectorDistribucion) selectorDistribucion.value = modoDistribucion;
        // capa recorre cada vista para borrar sus coordenadas guardadas y permitir recomputar el origen.
        Object.keys(posicionesPorCapa).forEach(capa => {
            posicionesPorCapa[capa].clear();
            movidasPorCapa[capa].clear();
            capaDispuesta[capa] = false;
            raizPorCapa[capa] = null;
            gruposColapsados[capa].clear();
        });
        tarjetasContraidas.clear();
        // nodo recorre las tarjetas y retira los estados visuales que alteran el mapa inicial.
        nodosTopologia.forEach(nodo => {
            nodo.classList.remove("nodo-contraido", "nodo-seleccionado", "nodo-vecino");
        });
        identificadorSeleccionado = null;
        mapaTopologia.classList.remove("mapa-con-seleccion");
        try {
            window.localStorage.removeItem(claveAlmacenamiento);
        } catch {
            // Si el navegador bloquea localStorage, el mapa igual vuelve a su distribución inicial.
        }
        seleccionarCapa("fisica");
        if (estadoMapa) estadoMapa.textContent = "Mapa restablecido a su distribución jerárquica inicial.";
    };
    botonesRestablecerMapa.forEach(botonRestablecer =>
        botonRestablecer.addEventListener("click", restablecerMapa));
    selectorDistribucion?.addEventListener("change", () => {
        modoDistribucion = ["mosaico", "filas", "cuadricula"].includes(selectorDistribucion.value) ? selectorDistribucion.value : "mosaico";
        distribuir();
    });

    // ---------------------------------------------------------------------------------------------
    // Colapsar: contraer tarjetas y ocultar grupos
    // ---------------------------------------------------------------------------------------------

    /**
     * Lista todas las tarjetas que cuelgan, directa o indirectamente, de una tarjeta.
     * @param {string} identificador ID de la tarjeta de partida; cambiarlo cambia el grupo.
     * @returns {string[]} IDs de las tarjetas dependientes.
     */
    const obtenerDescendientes = identificador => {
        const descendientes = [];
        const pila = [...(arbolActual.get(identificador) || [])];
        while (pila.length > 0) {
            const actual = pila.pop();
            descendientes.push(actual);
            (arbolActual.get(actual) || []).forEach(hijo => pila.push(hijo));
        }
        return descendientes;
    };

    /**
     * Contrae o expande el contenido de una tarjeta; contraída solo muestra su cabecera.
     * @param {HTMLElement} nodo Tarjeta a cambiar.
     * @param {boolean} contraer Verdadero deja solo la cabecera; falso muestra todo el contenido.
     */
    const establecerContraida = (nodo, contraer) => {
        nodo.classList.toggle("nodo-contraido", contraer);
        if (contraer) tarjetasContraidas.add(nodo.dataset.identificadorDispositivo);
        else tarjetasContraidas.delete(nodo.dataset.identificadorDispositivo);
        const botonContraer = nodo.querySelector("[data-contraer-tarjeta]");
        if (botonContraer) {
            botonContraer.textContent = contraer ? "Expandir" : "Contraer";
            botonContraer.setAttribute("aria-pressed", String(contraer));
        }
    };

    /**
     * Actualiza el botón de grupo de cada tarjeta: solo aparece si de ella cuelgan otras tarjetas
     * y muestra cuántas oculta o mostraría.
     */
    const actualizarControlesGrupo = () => {
        nodosTopologia.forEach(nodo => {
            const botonGrupo = nodo.querySelector("[data-colapsar-grupo]");
            if (!botonGrupo) return;
            const identificador = nodo.dataset.identificadorDispositivo;
            const cantidad = obtenerDescendientes(identificador).length;
            const colapsado = gruposColapsados[modoCapaActual].has(identificador);
            botonGrupo.hidden = cantidad === 0;
            botonGrupo.textContent = colapsado ? `Mostrar ${cantidad}` : `Ocultar ${cantidad}`;
            botonGrupo.setAttribute("aria-pressed", String(colapsado));
            botonGrupo.title = colapsado
                ? `Muestra los ${cantidad} equipos que dependen de esta tarjeta`
                : `Oculta los ${cantidad} equipos que dependen de esta tarjeta`;
        });
    };

    /**
     * Agrega a cada tarjeta una fila con los controles de contraer y de ocultar su grupo.
     * Son botones de texto, por lo que no inician un arrastre y se pueden usar con teclado.
     * @param {HTMLElement} nodo Tarjeta que recibe los controles.
     */
    const agregarControlesTarjeta = nodo => {
        const acciones = document.createElement("div");
        acciones.className = "acciones-tarjeta-topologia";
        const botonGrupo = document.createElement("button");
        botonGrupo.type = "button";
        botonGrupo.className = "control-tarjeta";
        botonGrupo.setAttribute("data-colapsar-grupo", "");
        botonGrupo.hidden = true;
        const botonContraer = document.createElement("button");
        botonContraer.type = "button";
        botonContraer.className = "control-tarjeta";
        botonContraer.setAttribute("data-contraer-tarjeta", "");
        botonContraer.title = "Reduce la tarjeta a su cabecera";
        botonContraer.setAttribute("aria-pressed", "false");
        botonContraer.textContent = "Contraer";
        acciones.append(botonGrupo, botonContraer);
        nodo.prepend(acciones);
    };
    nodosTopologia.forEach(agregarControlesTarjeta);

    lienzoTopologia.addEventListener("click", evento => {
        const botonContraer = evento.target.closest("[data-contraer-tarjeta]");
        const botonGrupo = evento.target.closest("[data-colapsar-grupo]");
        if (botonContraer) {
            const nodo = botonContraer.closest("[data-dispositivo]");
            establecerContraida(nodo, !nodo.classList.contains("nodo-contraido"));
        } else if (botonGrupo) {
            const identificador = botonGrupo.closest("[data-dispositivo]").dataset.identificadorDispositivo;
            const colapsados = gruposColapsados[modoCapaActual];
            if (colapsados.has(identificador)) colapsados.delete(identificador);
            else colapsados.add(identificador);
            actualizarFiltros();
        }
    });

    botonContraerTodo?.addEventListener("click", () => {
        nodosTopologia.forEach(nodo => establecerContraida(nodo, true));
        distribuir();
    });
    botonExpandirTodo?.addEventListener("click", () => {
        nodosTopologia.forEach(nodo => establecerContraida(nodo, false));
        distribuir();
    });
    botonColapsarGrupos?.addEventListener("click", () => {
        // Se colapsan las tarjetas de las que cuelgan solo equipos finales (por ejemplo, un switch con sus PCs).
        arbolActual.forEach((hijos, identificador) => {
            if (hijos.length > 0 && hijos.every(hijo => (arbolActual.get(hijo) || []).length === 0)) {
                gruposColapsados[modoCapaActual].add(identificador);
            }
        });
        actualizarFiltros();
        distribuir();
    });
    botonExpandirGrupos?.addEventListener("click", () => {
        gruposColapsados[modoCapaActual].clear();
        actualizarFiltros();
        distribuir();
    });

    // ---------------------------------------------------------------------------------------------
    // Maximizar dentro de la página
    // ---------------------------------------------------------------------------------------------

    /**
     * Sincroniza el texto del botón con el estado de la caja expandida.
     * Cambiar la clase del marco modifica si se ofrece maximizar o restaurar.
     */
    const actualizarControlMaximizar = () => {
        // mapaExpandido indica que la caja ocupa temporalmente el área visible de la página.
        const mapaExpandido = Boolean(marcoMapa?.classList.contains("mapa-ampliado"));
        botonesMaximizarMapa.forEach(botonMaximizar => {
            // etiquetaControl nombra la acción siguiente sin confundirla con F11 del navegador.
            const etiquetaControl = mapaExpandido ? "Restaurar mapa" : "Maximizar mapa";
            botonMaximizar.textContent = etiquetaControl;
            botonMaximizar.setAttribute("aria-label", etiquetaControl);
            botonMaximizar.setAttribute("aria-pressed", mapaExpandido ? "true" : "false");
        });
    };

    /**
     * Restaura la caja en la página.
     * Cambiar el marco afecta únicamente la vista, nunca los datos de conexiones.
     */
    const restaurarMapaEnPagina = () => {
        marcoMapa?.classList.remove("mapa-ampliado");
        document.body.classList.remove("mapa-ampliado-en-pagina");
        actualizarControlMaximizar();
    };

    /**
     * Expande la caja sobre la página manteniendo sus capas, filtros y posiciones.
     */
    const alternarMaximizacionMapa = () => {
        if (!marcoMapa) return;
        if (marcoMapa.classList.contains("mapa-ampliado")) {
            restaurarMapaEnPagina();
        } else {
            marcoMapa.classList.add("mapa-ampliado");
            document.body.classList.add("mapa-ampliado-en-pagina");
            actualizarControlMaximizar();
        }
    };

    botonesMaximizarMapa.forEach(botonMaximizar => botonMaximizar.addEventListener("click", alternarMaximizacionMapa));
    document.addEventListener("keydown", eventoTecla => {
        // eventoTecla permite cerrar la caja expandida con Escape sin depender de controles del navegador.
        if (eventoTecla.key === "Escape" && marcoMapa?.classList.contains("mapa-ampliado")) {
            restaurarMapaEnPagina();
        }
    });

    // ---------------------------------------------------------------------------------------------
    // Capas, filtros y resumen
    // ---------------------------------------------------------------------------------------------

    /**
     * Actualiza el resumen visible para reflejar solo nodos y enlaces del filtro actual.
     * @param {number} cantidadEnlaces Conexiones dibujadas.
     * @param {number} cantidadSinConexion Equipos ocultos por no tener conexión en la capa.
     * @param {number} cantidadColapsados Equipos ocultos porque su grupo está colapsado.
     */
    const actualizarResumen = (cantidadEnlaces, cantidadSinConexion, cantidadColapsados = 0) => {
        // equiposVisibles actualiza el resumen tras variar capa, área, búsqueda o conexiones.
        const equiposVisibles = listarNodosVisibles().length;
        if (resumenMapa) {
            resumenMapa.textContent = `${equiposVisibles} equipos · ${cantidadEnlaces} enlaces`
                + (cantidadSinConexion > 0 ? ` · ${cantidadSinConexion} sin conexión ocultos` : "")
                + (cantidadColapsados > 0 ? ` · ${cantidadColapsados} en grupos colapsados` : "");
        }
        avisoMapaVacio?.toggleAttribute("hidden", equiposVisibles > 0);
    };

    /**
     * Filtra equipos y enlaces por capa, texto, área y conexión; ubica las tarjetas y redibuja las líneas.
     * No infiere relaciones: solo decide qué parte de lo documentado se muestra.
     */
    const actualizarFiltros = () => {
        // textoBuscado se compara sin distinguir mayúsculas ni tildes.
        const textoBuscado = normalizarTexto(buscarTopologia?.value || "");
        const areaElegida = filtrarArea?.value || "";
        // identificadoresAcceso contiene puestos y los equipos finales enlazados a un puerto de switch.
        const identificadoresAcceso = new Set(nodosTopologia
            .filter(nodo => normalizarTexto(nodo.dataset.dispositivoTipo || "").includes("puesto"))
            .map(nodo => nodo.dataset.identificadorDispositivo));
        if (modoCapaActual === "acceso") {
            datosEnlaces.filter(enlace => enlace.dataset.capaRelacional === "acceso")
                .forEach(enlace => {
                    identificadoresAcceso.add(enlace.dataset.equipoOrigen);
                    identificadoresAcceso.add(enlace.dataset.equipoDestino);
                });
        }

        nodosTopologia.forEach(nodo => {
            // tipoNormalizado permite aplicar los filtros por familia sin depender de mayúsculas ni tildes.
            const tipoNormalizado = normalizarTexto(nodo.dataset.dispositivoTipo || "");
            // coincideCapa limita la tarjeta a la perspectiva actualmente seleccionada.
            const coincideCapa = modoCapaActual === "fisica"
                || (modoCapaActual === "switching" && tipoNormalizado.includes("switch"))
                || (modoCapaActual === "acceso" && identificadoresAcceso.has(nodo.dataset.identificadorDispositivo));
            // textoNormalizado reúne los campos documentados que participan de la búsqueda.
            const textoNormalizado = normalizarTexto([
                nodo.dataset.dispositivoNombre,
                nodo.dataset.dispositivoTipo,
                nodo.dataset.dispositivoArea,
                nodo.dataset.dispositivoModelo,
                nodo.dataset.dispositivoIp
            ].join(" "));
            nodo.hidden = !coincideCapa
                || (areaElegida !== "" && nodo.dataset.dispositivoArea !== areaElegida)
                || (textoBuscado !== "" && !textoNormalizado.includes(textoBuscado));
        });

        // enlacesVisibles son las conexiones cuyos dos extremos pasaron los filtros anteriores.
        let enlacesVisibles = listarEnlacesVisibles();
        // cantidadSinConexion cuenta los equipos ocultos solo por no tener conexión en esta capa.
        let cantidadSinConexion = 0;
        if (casillaSoloConectados?.checked) {
            const conectados = new Set();
            enlacesVisibles.forEach(registro => {
                conectados.add(registro.idOrigen);
                conectados.add(registro.idDestino);
            });
            nodosTopologia.forEach(nodo => {
                if (!nodo.hidden && !conectados.has(nodo.dataset.identificadorDispositivo)) {
                    nodo.hidden = true;
                    cantidadSinConexion++;
                }
            });
        }

        // El árbol se calcula antes de ocultar grupos para saber qué cuelga de cada tarjeta aunque esté oculta.
        arbolActual = calcularArbol(listarNodosVisibles(), listarEnlacesVisibles()).hijosGlobal;
        // cantidadColapsados cuenta los equipos ocultos solo porque su grupo está colapsado.
        let cantidadColapsados = 0;
        gruposColapsados[modoCapaActual].forEach(identificadorGrupo => {
            obtenerDescendientes(identificadorGrupo).forEach(identificadorDescendiente => {
                const nodoDescendiente = nodosPorId.get(identificadorDescendiente);
                if (nodoDescendiente && !nodoDescendiente.hidden) {
                    nodoDescendiente.hidden = true;
                    cantidadColapsados++;
                }
            });
        });
        enlacesVisibles = listarEnlacesVisibles();

        // La selección no sobrevive a un cambio de equipos visibles.
        if (identificadorSeleccionado !== null && nodosPorId.get(identificadorSeleccionado)?.hidden !== false) {
            identificadorSeleccionado = null;
            mapaTopologia.classList.remove("mapa-con-seleccion");
            nodosTopologia.forEach(nodo => nodo.classList.remove("nodo-seleccionado", "nodo-vecino"));
        }

        const dispusoCapaCompleta = asegurarDisposicion(enlacesVisibles);
        construirEnlaces(enlacesVisibles);
        if (identificadorSeleccionado !== null) {
            seleccionarNodo(identificadorSeleccionado);
        } else {
            actualizarGeometria();
        }
        actualizarControlesGrupo();
        actualizarResumen(enlacesVisibles.length, cantidadSinConexion, cantidadColapsados);
        return dispusoCapaCompleta;
    };

    /**
     * Cambia la perspectiva de red; el modo determina qué tarjetas pueden verse.
     * @param {string} modoSolicitado El valor física, switching o acceso limita la vista sin alterar los datos.
     */
    const seleccionarCapa = modoSolicitado => {
        // modoValido evita activar una vista desconocida si el HTML recibe un valor inesperado.
        const modoValido = ["fisica", "switching", "acceso"].includes(modoSolicitado) ? modoSolicitado : "fisica";
        modoCapaActual = modoValido;
        mapaTopologia.dataset.capaActual = modoValido;
        botonesCapa.forEach(boton => {
            // estaActiva evita marcar simultáneamente dos perspectivas.
            const estaActiva = boton.dataset.seleccionarCapa === modoCapaActual;
            boton.classList.toggle("activa", estaActiva);
            boton.setAttribute("aria-pressed", String(estaActiva));
        });
        // La disposición es independiente por capa; al volver a una capa ya vista se conservan sus posiciones.
        const yaEstabaDispuesta = capaDispuesta[modoCapaActual];
        const dispusoCapaCompleta = actualizarFiltros();
        if (dispusoCapaCompleta || !yaEstabaDispuesta) {
            vistaInicial();
        }
    };

    botonesCapa.forEach(boton => {
        // cada boton cambia solo la perspectiva visual y conserva los filtros de búsqueda y área.
        boton.addEventListener("click", () => seleccionarCapa(boton.dataset.seleccionarCapa || "fisica"));
    });
    buscarTopologia?.addEventListener("input", actualizarFiltros);
    filtrarArea?.addEventListener("change", actualizarFiltros);
    casillaSoloConectados?.addEventListener("change", () => {
        actualizarFiltros();
        vistaInicial();
    });

    // observadorTarjetas actualiza tamaño y anclas cuando una tarjeta cambia de alto o ancho (por ejemplo, al elegir un puerto).
    const observadorTarjetas = new ResizeObserver(entradas => {
        // tarjetasCambiadas reúne las tarjetas visibles con tamaño nuevo.
        const tarjetasCambiadas = [];
        entradas.forEach(entrada => {
            const nodo = entrada.target;
            if (nodo.hidden || nodo.offsetWidth === 0) return;
            const tamanoPrevio = tamanosNodos.get(nodo);
            if (tamanoPrevio && tamanoPrevio.ancho === nodo.offsetWidth && tamanoPrevio.alto === nodo.offsetHeight) return;
            tamanosNodos.set(nodo, { ancho: nodo.offsetWidth, alto: nodo.offsetHeight });
            tarjetasCambiadas.push(nodo);
        });
        tarjetasCambiadas.forEach(nodo => {
            (registrosPorNodo.get(nodo.dataset.identificadorDispositivo) || []).forEach(registro => {
                registro.anclaOrigen = medirAncla(registro.nodoOrigen, registro.puertoOrigen);
                registro.anclaDestino = medirAncla(registro.nodoDestino, registro.puertoDestino);
            });
        });
        if (tarjetasCambiadas.length > 0) actualizarGeometria(tarjetasCambiadas);
    });
    nodosTopologia.forEach(nodo => {
        nodo.classList.toggle("nodo-compacto", !nodo.querySelector(".panel-frontal-switch"));
        observadorTarjetas.observe(nodo);
    });

    // Al imprimir, el mapa se encaja en la zona de la hoja A4 horizontal y luego se restaura la vista.
    window.addEventListener("beforeprint", () => {
        vistaAntesDeImprimir = { ...vista };
        // En papel se permite un zoom muy bajo para que el mapa completo entre en la hoja.
        ajustarVista(0.02, anchoA4HorizontalPx, altoMapaImpresionPx, true);
    });
    window.addEventListener("afterprint", () => {
        if (vistaAntesDeImprimir) {
            Object.assign(vista, vistaAntesDeImprimir);
            vistaAntesDeImprimir = null;
            aplicarVista();
        }
    });

    leerPosicionesGuardadas();
    actualizarControlMaximizar();
    // Primera disposición: se calcula con las tarjetas ya medidas y luego se encaja la vista completa.
    seleccionarCapa("fisica");
    window.requestAnimationFrame(() => vistaInicial());
})();
