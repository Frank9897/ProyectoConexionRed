(() => {
    "use strict";

    // raiz contiene las direcciones de las acciones del servidor; sin ella la página no es el editor.
    const raiz = document.getElementById("editor-topologia");
    if (!raiz || typeof cytoscape === "undefined") {
        return;
    }

    // La biblioteca de dibujo se registra sola al cargarse después de Cytoscape; solo se registra a mano si no ocurrió.
    if (typeof cytoscapeEdgehandles === "function" && typeof cytoscape("core", "edgehandles") !== "function") {
        cytoscape.use(cytoscapeEdgehandles);
    }

    const urls = {
        datos: raiz.dataset.urlDatos, validar: raiz.dataset.urlValidar, crear: raiz.dataset.urlCrear,
        eliminar: raiz.dataset.urlEliminar, actualizar: raiz.dataset.urlActualizar, posiciones: raiz.dataset.urlPosiciones,
        leerPosiciones: raiz.dataset.urlLeerPosiciones,
        editar: raiz.dataset.urlEditar
    };
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";
    const colador = new Intl.Collator("es", { numeric: true, sensitivity: "base" });
    const porId = (id) => document.getElementById(id);
    const estadoPagina = porId("mapa-estado");

    // colores por tipo de dispositivo; un tipo desconocido usa el gris neutro.
    const coloresTipo = {
        "Switch": "#2f6f9f", "Router": "#7a5ea8", "Firewall": "#b4533c", "Servidor": "#3f7f5f",
        "Puesto de usuario": "#6b7685", "Impresora": "#8a7a3c", "UPS": "#a0662a"
    };
    const colorTipo = (tipo) => coloresTipo[tipo] ?? "#8591a3";
    const velocidadTexto = (mbps) => mbps >= 1000 ? `${Number((mbps / 1000).toFixed(1))} Gbps` : `${mbps} Mbps`;

    const crear = (etiqueta, clase, texto) => {
        const elemento = document.createElement(etiqueta);
        if (clase) elemento.className = clase;
        if (texto !== undefined) elemento.textContent = texto;
        return elemento;
    };

    const avisar = (texto) => { estadoPagina.textContent = texto; };

    /** Pide JSON al servidor; lanza un error con el primer mensaje cuando la respuesta no es correcta. */
    const pedirJson = async (url, parametros) => {
        const direccion = new URL(url, window.location.origin);
        Object.entries(parametros ?? {}).forEach(([clave, valor]) => direccion.searchParams.set(clave, valor));
        const respuesta = await fetch(direccion, { headers: { Accept: "application/json" } });
        if (!respuesta.ok) throw new Error(`Error ${respuesta.status} al consultar el servidor.`);
        return respuesta.json();
    };

    /** Envía un formulario POST con el token antifalsificación; devuelve JSON o lanza los errores del servidor. */
    const enviar = async (url, campos) => {
        const datos = new FormData();
        datos.set("__RequestVerificationToken", token);
        Object.entries(campos).forEach(([clave, valor]) => datos.set(clave, valor));
        const respuesta = await fetch(url, { method: "POST", body: datos, headers: { Accept: "application/json" } });
        let cuerpo = null;
        try { cuerpo = await respuesta.json(); } catch { /* respuesta sin JSON */ }
        if (!respuesta.ok) {
            const mensajes = cuerpo?.errores ?? [`Error ${respuesta.status} al guardar.`];
            const error = new Error(mensajes.join(" "));
            error.mensajes = mensajes;
            throw error;
        }
        return cuerpo;
    };

    // ---------- Cytoscape ----------
    const estilo = [
        {
            selector: "node",
            style: {
                width: 44, height: 44, shape: "round-rectangle", "background-color": "#ffffff",
                "background-image": "data(icono)", "background-fit": "contain", "background-clip": "none",
                "border-width": 3, "border-color": "data(color)", label: "data(nombre)", "font-size": 10,
                "text-valign": "bottom", "text-margin-y": 4, "text-wrap": "ellipsis", "text-max-width": 96,
                color: "#26303f", "min-zoomed-font-size": 6
            }
        },
        { selector: "node[tipo = 'Switch']", style: { width: 56, height: 40 } },
        { selector: "node[?noAdministrable]", style: { "border-style": "dashed" } },
        { selector: "node:selected", style: { "border-color": "#d9822b", "border-width": 5 } },
        { selector: "node.coincidencia", style: { "border-color": "#d9822b", "border-width": 5 } },
        { selector: "node.atenuado", style: { opacity: 0.25 } },
        { selector: ".oculto", style: { display: "none" } },
        {
            selector: "edge",
            style: {
                width: 2, "line-color": "#46556b", "curve-style": "bezier", label: "data(etiqueta)", "font-size": 8,
                "text-background-color": "#ffffff", "text-background-opacity": 0.85, "text-background-padding": 2,
                "min-zoomed-font-size": 9, "text-rotation": "autorotate", color: "#26303f"
            }
        },
        { selector: "edge.sin-etiquetas", style: { label: "" } },
        { selector: "edge[?parcial]", style: { "line-style": "dotted", "line-color": "#9aa5b4", width: 1.6 } },
        { selector: "edge[estadoStp = 'Reenvío']", style: { "line-color": "#2f9f6f" } },
        { selector: "edge[estadoStp = 'Bloqueado']", style: { "line-style": "dashed", "line-color": "#b4533c" } },
        { selector: "edge[estadoStp = 'Redundante']", style: { "line-style": "dashed", "line-color": "#d9822b" } },
        { selector: "edge.modo-datos[estadoDatos = 'Confirmado']", style: { "line-color": "#2f9f6f" } },
        { selector: "edge.modo-datos[estadoDatos = 'Pendiente de revisión']", style: { "line-color": "#8591a3" } },
        { selector: "edge.modo-datos[estadoDatos = 'A verificar']", style: { "line-color": "#d9a42b" } },
        { selector: "edge.modo-datos[estadoDatos = 'Desactualizado']", style: { "line-color": "#b4533c" } },
        { selector: "edge:selected", style: { width: 4, "line-color": "#d9822b" } },
        { selector: "edge.camino", style: { width: 5, "line-color": "#7a2fb4", "z-index": 10 } },
        { selector: "node.camino", style: { "border-color": "#7a2fb4", "border-width": 5 } },
        { selector: "edge.ciclo", style: { "underlay-color": "#d9822b", "underlay-opacity": 0.25, "underlay-padding": 4 } },
        { selector: ".eh-handle", style: { "background-color": "#d9822b", width: 14, height: 14, shape: "ellipse", "overlay-opacity": 0, "border-width": 10, "border-opacity": 0, label: "" } },
        { selector: ".eh-hover", style: { "background-color": "#b86a1d" } },
        { selector: ".eh-source", style: { "border-width": 4, "border-color": "#d9822b" } },
        { selector: ".eh-target", style: { "border-width": 4, "border-color": "#2f9f6f" } },
        { selector: ".eh-preview, .eh-ghost-edge", style: { "background-color": "#d9822b", "line-color": "#d9822b", "target-arrow-color": "#d9822b", "source-arrow-color": "#d9822b", label: "" } },
        { selector: ".eh-ghost-edge.eh-preview-active", style: { opacity: 0 } }
    ];

    const cy = cytoscape({
        container: porId("lienzo-mapa"), elements: [], style: estilo, wheelSensitivity: 0.25,
        minZoom: 0.1, maxZoom: 3, boxSelectionEnabled: true
    });

    // asistente de dibujo: arrastrar desde el punto naranja de un equipo (o en modo conectar, desde el propio equipo) hasta otro.
    const asistente = typeof cy.edgehandles === "function"
        ? cy.edgehandles({
            canConnect: (origen, destino) => !origen.same(destino),
            edgeParams: () => ({}), hoverDelay: 150, snap: true, snapThreshold: 40, snapFrequency: 15,
            noEdgeEventsInDraw: true, disableBrowserGestures: true
        })
        : null;

    // datosActuales conserva la última respuesta del servidor para armar el diálogo y el panel de selección.
    let datosActuales = { nodos: [], aristas: [], posiciones: {}, alertas: [] };
    const esNodoReal = (nodo) => !nodo.hasClass("eh-handle");
    const nodosReales = () => cy.nodes().filter(esNodoReal);
    const aristasReales = () => cy.edges().filter((arista) => arista.data("real") === true);

    // ---------- Vistas: qué equipos se muestran ----------
    // vistaActual decide qué se ve: "switches" (solo switches y sus conexiones, por defecto), "foco" (un switch con sus equipos
    // y los switches relacionados) o "todo" (inventario completo, solo si el técnico lo pide).
    let vistaActual = "switches";
    const selectorVista = porId("mapa-vista");
    const selectorFoco = porId("mapa-foco");
    const etiquetaFoco = porId("mapa-etiqueta-foco");
    // conPosicion marca los equipos que ya tienen una posición válida en la disposición de la vista actual.
    const conPosicion = new Set();
    let primeraCarga = true;

    const esSwitch = (nodo) => nodo.data("tipo") === "Switch";
    const esAristaReal = (arista) => arista.data("real") === true;
    const vecinosReales = (nodo) => nodo.connectedEdges().filter(esAristaReal).connectedNodes().filter((vecino) => vecino.id() !== nodo.id());
    const visiblesReales = () => nodosReales().filter((nodo) => !nodo.hasClass("oculto"));
    // La disposición de cada vista se guarda por separado: la del foco depende del switch elegido.
    const claveVista = () => vistaActual === "switches" ? "switches" : vistaActual === "todo" ? "todo" : `foco-${selectorFoco.value}`;

    /** Devuelve el switch en foco: el elegido o, si no hay, el que tiene más conexiones documentadas. */
    const elegirFoco = () => {
        const elegido = selectorFoco.value ? cy.getElementById(selectorFoco.value) : cy.collection();
        if (elegido.nonempty()) return elegido[0];
        const mejor = nodosReales().filter(esSwitch).toArray()
            .sort((p, q) => vecinosReales(q).length - vecinosReales(p).length)[0];
        if (mejor) selectorFoco.value = mejor.id();
        return mejor ?? null;
    };

    /** Conjunto de equipos que corresponden a la vista; nulo significa todos. */
    const equiposDeLaVista = () => {
        if (vistaActual === "todo") return null;
        const ids = new Set();
        if (vistaActual === "switches") {
            nodosReales().filter(esSwitch).forEach((nodo) => ids.add(nodo.id()));
            return ids;
        }
        const foco = elegirFoco();
        if (foco) {
            ids.add(foco.id());
            vecinosReales(foco).forEach((vecino) => ids.add(vecino.id()));
        }
        return ids;
    };

    // ---------- Carga y reconciliación ----------
    const cargar = async () => {
        avisar("Cargando el mapa documentado.");
        datosActuales = await pedirJson(urls.datos);
        const { nodos, aristas } = datosActuales;

        cy.batch(() => {
            const idsNodos = new Set(nodos.map((nodo) => String(nodo.id)));
            nodosReales().forEach((nodo) => { if (!idsNodos.has(nodo.id())) { conPosicion.delete(nodo.id()); nodo.remove(); } });
            aristasReales().remove();

            nodos.forEach((nodo) => {
                const id = String(nodo.id);
                const datosNodo = {
                    id, nombre: nodo.nombre, tipo: nodo.tipo, ip: nodo.ip, area: nodo.area, administrable: nodo.administrable,
                    noAdministrable: nodo.administrable === false,
                    icono: nodo.icono, color: colorTipo(nodo.tipo), puertos: nodo.puertos, notasSinEquipo: nodo.notasSinEquipo
                };
                const existente = cy.getElementById(id);
                if (existente.nonempty()) existente.data(datosNodo);
                else cy.add({ group: "nodes", data: datosNodo, position: { x: 0, y: 0 }, classes: "oculto" });
            });

            aristas.forEach((arista) => {
                const origen = String(arista.source);
                const destino = String(arista.target);
                if (cy.getElementById(origen).empty() || cy.getElementById(destino).empty()) return;
                const etiqueta = arista.etiquetaDestino ? `${arista.etiquetaOrigen} ↔ ${arista.etiquetaDestino}` : arista.etiquetaOrigen;
                cy.add({ group: "edges", data: { ...arista, id: arista.id, source: origen, target: destino, etiqueta, real: true } });
            });
        });

        llenarFiltros(nodos);
        pintarAlertas(datosActuales.alertas);
        if (primeraCarga) {
            primeraCarga = false;
            await cambiarVista();
        } else {
            aplicarFiltros();
        }
        const cantidadSwitches = nodos.filter((nodo) => nodo.tipo === "Switch").length;
        avisar(`${nodos.length} equipos en el inventario (${cantidadSwitches} switches) y ${aristas.length} conexiones documentadas.`);
    };

    /** Cambia lo que se muestra: carga la disposición guardada de esa vista y encaja el mapa en pantalla. */
    const cambiarVista = async () => {
        vistaActual = selectorVista.value;
        etiquetaFoco.hidden = vistaActual !== "foco";
        if (vistaActual === "foco") elegirFoco();
        conPosicion.clear();
        try {
            const guardadas = await pedirJson(urls.leerPosiciones, { vista: claveVista() });
            cy.batch(() => Object.entries(guardadas).forEach(([id, posicion]) => {
                const nodo = cy.getElementById(id);
                if (nodo.nonempty()) { nodo.position(posicion); conPosicion.add(id); }
            }));
        } catch {
            // Sin posiciones guardadas se calculan al mostrar la vista.
        }
        aplicarFiltros({ encajar: true });
    };

    // ---------- Filtros y búsqueda ----------
    const llenarFiltros = (nodos) => {
        const rellenar = (selector, valores, valorActual) => {
            const opciones = [...new Set(valores.filter(Boolean))].sort(colador.compare);
            const primera = selector.options[0];
            selector.replaceChildren(primera, ...opciones.map((valor) => {
                const opcion = crear("option", "", valor);
                opcion.value = valor;
                return opcion;
            }));
            selector.value = opciones.includes(valorActual) ? valorActual : "";
        };
        rellenar(porId("mapa-area"), nodos.map((nodo) => nodo.area), porId("mapa-area").value);
        rellenar(porId("mapa-tipo"), nodos.map((nodo) => nodo.tipo), porId("mapa-tipo").value);

        // El selector de foco ofrece solo switches; conserva la elección si sigue existiendo.
        const actual = selectorFoco.value;
        const switches = nodos.filter((nodo) => nodo.tipo === "Switch").sort((p, q) => colador.compare(p.nombre, q.nombre));
        selectorFoco.replaceChildren(...switches.map((nodo) => {
            const opcion = crear("option", "", nodo.nombre);
            opcion.value = String(nodo.id);
            return opcion;
        }));
        if (switches.some((nodo) => String(nodo.id) === actual)) {
            selectorFoco.value = actual;
        } else if (switches.length > 0) {
            // Sin una elección previa se propone el switch con más conexiones documentadas.
            const conteo = new Map();
            datosActuales.aristas.forEach((arista) => [String(arista.source), String(arista.target)]
                .forEach((id) => conteo.set(id, (conteo.get(id) ?? 0) + 1)));
            const mejor = [...switches].sort((p, q) => (conteo.get(String(q.id)) ?? 0) - (conteo.get(String(p.id)) ?? 0))[0];
            selectorFoco.value = String(mejor.id);
        }
    };

    /** Muestra u oculta equipos según la vista, el área, el tipo y la búsqueda, y ubica los que aún no tienen posición. */
    const aplicarFiltros = ({ encajar = false } = {}) => {
        const area = porId("mapa-area").value;
        const tipo = porId("mapa-tipo").value;
        const texto = porId("mapa-buscar").value.trim().toLowerCase();
        const permitidos = equiposDeLaVista();
        cy.batch(() => {
            nodosReales().forEach((nodo) => {
                // Una búsqueda por texto muestra el equipo aunque no corresponda a la vista, para poder encontrarlo y conectarlo.
                const coincideTexto = texto.length > 0
                    && `${nodo.data("nombre")} ${nodo.data("ip") ?? ""}`.toLowerCase().includes(texto);
                const visible = (permitidos === null || permitidos.has(nodo.id()) || coincideTexto)
                    && (!area || nodo.data("area") === area)
                    && (!tipo || nodo.data("tipo") === tipo);
                nodo.toggleClass("oculto", !visible);
                nodo.toggleClass("coincidencia", coincideTexto && visible);
            });
            cy.edges().toggleClass("sin-etiquetas", !porId("mapa-etiquetas").checked);
            cy.edges().toggleClass("modo-datos", porId("mapa-color").value === "datos");
        });

        ubicarFaltantes();
        const coincidencias = cy.nodes(".coincidencia");
        if (texto.length > 0 && coincidencias.nonempty()) {
            cy.animate({ fit: { eles: coincidencias, padding: 120 }, duration: 250 });
        } else if (encajar && visiblesReales().nonempty()) {
            cy.fit(visiblesReales(), 40);
        }
    };

    ["mapa-area", "mapa-tipo", "mapa-etiquetas", "mapa-color"].forEach((id) =>
        porId(id).addEventListener("change", () => aplicarFiltros()));
    selectorVista.addEventListener("change", () => cambiarVista().catch((error) => avisar(error.message)));
    selectorFoco.addEventListener("change", () => cambiarVista().catch((error) => avisar(error.message)));
    let temporizadorBusqueda = 0;
    porId("mapa-buscar").addEventListener("input", () => {
        window.clearTimeout(temporizadorBusqueda);
        temporizadorBusqueda = window.setTimeout(() => aplicarFiltros(), 250);
    });
    porId("mapa-ajustar").addEventListener("click", () => cy.fit(visiblesReales(), 40));
    porId("mapa-recargar").addEventListener("click", () => cargar().catch((error) => avisar(error.message)));
    porId("mapa-exportar").addEventListener("click", () => {
        // Se exporta todo lo visible (no solo la parte en pantalla) con fondo blanco, apto para documentación impresa.
        const imagen = cy.png({ full: true, scale: 2, bg: "#ffffff" });
        const descarga = crear("a");
        descarga.href = imagen;
        descarga.download = "mapa-de-red.png";
        descarga.click();
        avisar("Imagen exportada.");
    });

    // ---------- Distribución y posiciones ----------
    // Tamaño aproximado de un equipo con su nombre, usado para que la distribución no los superponga.
    const anchoEquipo = 112;
    const altoEquipo = 76;

    /** Distribuye los equipos indicados por niveles: central arriba, switches por filas y equipos finales debajo de su switch. */
    const distribuirNiveles = (nodos) => {
        if (!window.DistribucionRed || nodos.length === 0) return;
        const ids = new Set(nodos.map((nodo) => nodo.id()));
        const entrada = nodos.map((nodo) => ({
            id: nodo.id(), ancho: anchoEquipo, alto: altoEquipo, esSwitch: esSwitch(nodo), nombre: nodo.data("nombre")
        }));
        const conexiones = aristasReales().toArray()
            .filter((arista) => ids.has(arista.source().id()) && ids.has(arista.target().id()))
            .map((arista) => ({ a: arista.source().id(), b: arista.target().id() }));
        const resultado = window.DistribucionRed.distribuirPorNiveles(entrada, conexiones,
            { separacionX: 36, separacionNiveles: 130, separacionHoja: 16, maxColumnasHojas: 6 });
        cy.batch(() => nodos.forEach((nodo) => {
            const posicion = resultado.get(nodo.id());
            if (!posicion) return;
            nodo.position({ x: posicion.x + anchoEquipo / 2, y: posicion.y + altoEquipo / 2 });
            conPosicion.add(nodo.id());
        }));
        guardarPosiciones(nodos).catch((error) => avisar(error.message));
        cy.fit(visiblesReales(), 40);
    };

    /** Ubica un equipo nuevo debajo de uno de sus vecinos ya ubicados; si no tiene vecinos, a la derecha del conjunto. */
    const ubicarCerca = (faltan) => {
        const contador = new Map();
        const ubicados = visiblesReales().filter((nodo) => conPosicion.has(nodo.id()));
        const limiteDerecho = ubicados.nonempty() ? ubicados.boundingBox().x2 : 0;
        const arriba = ubicados.nonempty() ? ubicados.boundingBox().y1 : 0;
        let sueltos = 0;
        faltan.forEach((nodo) => {
            const vecino = vecinosReales(nodo).filter((v) => conPosicion.has(v.id()) && !v.hasClass("oculto"))[0];
            if (vecino) {
                const indice = contador.get(vecino.id()) ?? 0;
                contador.set(vecino.id(), indice + 1);
                nodo.position({ x: vecino.position("x") + ((indice % 6) - 2.5) * (anchoEquipo + 8), y: vecino.position("y") + 150 + Math.floor(indice / 6) * (altoEquipo + 12) });
            } else {
                nodo.position({ x: limiteDerecho + 160, y: arriba + sueltos * (altoEquipo + 12) });
                sueltos++;
            }
            conPosicion.add(nodo.id());
        });
        guardarPosiciones(faltan).catch((error) => avisar(error.message));
    };

    /** Asegura que todo equipo visible tenga posición: distribuye todo si faltan muchos, o ubica los pocos nuevos cerca de sus vecinos. */
    const ubicarFaltantes = () => {
        const visibles = visiblesReales();
        const faltan = visibles.filter((nodo) => !conPosicion.has(nodo.id()));
        if (faltan.empty()) return;
        if (faltan.length > visibles.length * 0.25) distribuirNiveles(visibles.toArray());
        else ubicarCerca(faltan.toArray());
    };

    const redistribuir = () => {
        const tipoDiseno = porId("mapa-diseno").value;
        const visibles = visiblesReales();
        if (visibles.empty()) return;
        if (tipoDiseno === "niveles") {
            distribuirNiveles(visibles.toArray());
            return;
        }
        const elementos = visibles.union(visibles.connectedEdges().filter((arista) => esAristaReal(arista)
            && !arista.source().hasClass("oculto") && !arista.target().hasClass("oculto")));
        const opciones = tipoDiseno === "cose"
            ? { name: "cose", animate: false, fit: true, padding: 30, idealEdgeLength: 110, nodeRepulsion: 12000, gravity: 0.5, numIter: 1500 }
            : { name: tipoDiseno, animate: false, fit: true, padding: 30 };
        elementos.layout(opciones).run();
        visibles.forEach((nodo) => conPosicion.add(nodo.id()));
        guardarPosiciones(visibles.toArray()).catch((error) => avisar(error.message));
    };
    porId("mapa-redistribuir").addEventListener("click", redistribuir);

    /** Guarda en el servidor la posición de los nodos indicados para la vista actual, compartida entre técnicos. */
    const guardarPosiciones = async (nodos) => {
        const lista = nodos.map((nodo) => ({ id: Number(nodo.id()), x: nodo.position("x"), y: nodo.position("y") }));
        if (lista.length === 0) return;
        const vista = claveVista();
        for (let inicio = 0; inicio < lista.length; inicio += 1000) {
            await enviar(urls.posiciones, { posiciones: JSON.stringify(lista.slice(inicio, inicio + 1000)), vista });
        }
        avisar("Disposición guardada.");
    };

    // pendientes agrupa los nodos movidos para enviarlos juntos y no una vez por cada arrastre.
    const pendientes = new Set();
    let temporizadorGuardado = 0;
    cy.on("dragfree", "node", (evento) => {
        if (!esNodoReal(evento.target)) return;
        pendientes.add(evento.target.id());
        cy.nodes(":selected").filter(esNodoReal).forEach((nodo) => pendientes.add(nodo.id()));
        window.clearTimeout(temporizadorGuardado);
        temporizadorGuardado = window.setTimeout(async () => {
            const nodos = [...pendientes].map((id) => cy.getElementById(id));
            pendientes.clear();
            try { await guardarPosiciones(nodos); } catch (error) { avisar(error.message); }
        }, 900);
    });

    /** Muestra un switch con sus equipos y los switches relacionados (vista «foco»). */
    const verEquiposDelSwitch = (nodo) => {
        selectorVista.value = "foco";
        selectorFoco.value = nodo.id();
        return cambiarVista().catch((error) => avisar(error.message));
    };
    cy.on("dbltap", "node", (evento) => { if (esNodoReal(evento.target) && esSwitch(evento.target)) verEquiposDelSwitch(evento.target); });

    // ---------- Alertas ----------
    const pintarAlertas = (alertas) => {
        const contenedor = porId("mapa-alertas");
        cy.edges().removeClass("ciclo");
        if (!alertas || alertas.length === 0) {
            contenedor.replaceChildren(crear("p", "editor-alerta editor-alerta-ok", "Sin ciclos ni conflictos de IP detectados en lo documentado."));
            return;
        }
        contenedor.replaceChildren(...alertas.map((alerta) => {
            const boton = crear("button", `editor-alerta${alerta.documentado ? " editor-alerta-ok" : ""}`, alerta.texto);
            boton.type = "button";
            if (alerta.aristaId) {
                cy.getElementById(alerta.aristaId).addClass("ciclo");
                boton.addEventListener("click", () => {
                    const arista = cy.getElementById(alerta.aristaId);
                    if (arista.empty()) return;
                    cy.elements().unselect();
                    arista.select();
                    cy.animate({ fit: { eles: arista.connectedNodes(), padding: 160 }, duration: 250 });
                });
            }
            return boton;
        }));
    };

    // ---------- Panel de selección ----------
    const dato = (etiqueta, valor) => {
        const parrafo = crear("p", "editor-dato");
        parrafo.append(crear("strong", "", `${etiqueta}: `), document.createTextNode(valor || "Sin dato"));
        return parrafo;
    };

    const mostrarNodo = (nodo) => {
        const panel = porId("mapa-seleccion");
        const puertos = nodo.data("puertos") ?? [];
        const libres = puertos.filter((puerto) => !puerto.enlazado && !puerto.relacionParcial).length;
        const administrable = nodo.data("administrable");
        const acciones = crear("div", "editor-acciones");
        const enlaceFicha = crear("a", "boton boton-secundario", "Abrir ficha");
        enlaceFicha.href = urls.editar.replace("__ID__", encodeURIComponent(nodo.id()));
        acciones.append(enlaceFicha);
        if (esSwitch(nodo)) {
            const verEquipos = crear("button", "boton boton-secundario", "Ver sus equipos");
            verEquipos.type = "button";
            verEquipos.addEventListener("click", () => verEquiposDelSwitch(nodo));
            acciones.append(verEquipos);
        }
        panel.replaceChildren(
            crear("h3", "", nodo.data("nombre")),
            dato("Tipo", nodo.data("tipo")), dato("IP", nodo.data("ip")), dato("Área", nodo.data("area")),
            dato("Administrable", administrable === true ? "Sí" : administrable === false ? "No" : "Sin confirmar"),
            dato("Puertos", `${puertos.length} registrados, ${libres} sin conexión documentada`),
            dato("Relaciones sin equipo asociado", String(nodo.data("notasSinEquipo") ?? 0)),
            acciones, bloqueCamino(nodo));
    };

    /** Permite elegir otro equipo y resaltar el camino documentado más corto hasta él, útil para análisis rápido. */
    const bloqueCamino = (origen) => {
        const contenedor = crear("div", "editor-campos-inline");
        const etiqueta = crear("label", "", "Camino hasta otro equipo");
        etiqueta.htmlFor = "mapa-camino-destino";
        const entrada = crear("input");
        entrada.id = "mapa-camino-destino";
        entrada.setAttribute("list", "mapa-camino-lista");
        entrada.placeholder = "Nombre del equipo destino";
        const lista = crear("datalist");
        lista.id = "mapa-camino-lista";
        nodosReales().filter((nodo) => nodo.id() !== origen.id()).forEach((nodo) => {
            const opcion = crear("option");
            opcion.value = nodo.data("nombre");
            lista.append(opcion);
        });
        const resultado = crear("div");
        const resaltar = crear("button", "boton boton-secundario", "Resaltar camino");
        resaltar.type = "button";
        resaltar.addEventListener("click", () => {
            limpiarCamino();
            const destino = nodosReales().filter((nodo) => nodo.data("nombre").toLowerCase() === entrada.value.trim().toLowerCase());
            if (destino.empty()) { resultado.replaceChildren(crear("p", "editor-camino", "No hay un equipo con ese nombre.")); return; }
            const recorrido = cy.elements().filter((elemento) => esNodoReal(elemento.isNode() ? elemento : elemento.source()))
                .dijkstra({ root: origen, weight: () => 1, directed: false });
            // Cytoscape devuelve una ruta aunque el destino sea inalcanzable; la distancia infinita indica que no hay camino.
            const ruta = recorrido.pathTo(destino[0]);
            if (!Number.isFinite(recorrido.distanceTo(destino[0]))) { resultado.replaceChildren(crear("p", "editor-camino", "No hay un camino documentado entre esos equipos.")); return; }
            ruta.removeClass("oculto").addClass("camino");
            const nombres = ruta.nodes().map((nodo) => nodo.data("nombre"));
            resultado.replaceChildren(crear("p", "editor-camino", `${nombres.length - 1} saltos: ${nombres.join(" → ")}`));
        });
        const limpiar = crear("button", "boton boton-secundario", "Quitar resaltado");
        limpiar.type = "button";
        limpiar.addEventListener("click", () => { limpiarCamino(); resultado.replaceChildren(); });
        const acciones = crear("div", "editor-acciones");
        acciones.append(resaltar, limpiar);
        contenedor.append(etiqueta, entrada, lista, acciones, resultado);
        return contenedor;
    };

    const limpiarCamino = () => cy.elements(".camino").removeClass("camino");

    const mostrarArista = (arista) => {
        const panel = porId("mapa-seleccion");
        const origen = arista.source().data("nombre");
        const destino = arista.target().data("nombre");
        const elementos = [
            crear("h3", "", arista.data("parcial") ? "Relación documentada (parcial)" : "Enlace físico"),
            dato("Origen", `${origen} · ${arista.data("etiquetaOrigen")}`),
            dato("Destino", `${destino}${arista.data("etiquetaDestino") ? ` · ${arista.data("etiquetaDestino")}` : ""}`),
            dato("Medio", arista.data("medio")), dato("Estado de los datos", arista.data("estadoDatos")),
            dato("Fuente", arista.data("fuente"))
        ];

        if (arista.data("parcial")) {
            elementos.push(crear("p", "editor-vacio",
                "Es una nota de un solo extremo. Para registrarla como enlace completo, dibuja la conexión entre estos mismos equipos eligiendo el puerto documentado: se convertirá automáticamente."));
        } else {
            // selectorLista arma un select con etiqueta y el valor actual del enlace.
            const selectorLista = (identificador, titulo, opciones, valor) => {
                const bloque = crear("div", "editor-campos-inline");
                const etiqueta = crear("label", "", titulo);
                etiqueta.htmlFor = identificador;
                const selector = crear("select");
                selector.id = identificador;
                opciones.forEach((opcion) => { const elemento = crear("option", "", opcion); elemento.value = opcion; selector.append(elemento); });
                selector.value = opciones.includes(valor) ? valor : opciones[0];
                bloque.append(etiqueta, selector);
                return { bloque, selector };
            };
            const medio = selectorLista("mapa-medio", "Medio", ["No confirmado", "Cobre RJ45", "Fibra", "DAC", "Otro"], arista.data("medio"));
            const datos = selectorLista("mapa-datos", "Estado de los datos", ["Pendiente de revisión", "Confirmado", "A verificar", "Desactualizado"], arista.data("estadoDatos"));
            const stp = selectorLista("mapa-stp", "Estado STP o RSTP", ["No documentado", "Reenvío", "Bloqueado", "Redundante"], arista.data("estadoStp"));
            const guardar = crear("button", "boton boton-secundario", "Guardar cambios");
            guardar.type = "button";
            guardar.addEventListener("click", async () => {
                try {
                    const respuesta = await enviar(urls.actualizar, {
                        id: arista.data("enlaceId"), medio: medio.selector.value, estadoDatos: datos.selector.value, estadoStp: stp.selector.value
                    });
                    await cargar();
                    avisar(respuesta.cambios > 0 ? "Enlace actualizado." : "No había cambios que guardar.");
                } catch (error) { avisar(error.message); }
            });
            const eliminar = crear("button", "boton boton-secundario", "Eliminar enlace");
            eliminar.type = "button";
            eliminar.addEventListener("click", async () => {
                if (!window.confirm(`Se eliminará el enlace entre ${origen} y ${destino}. Solo cambia la documentación. ¿Continuar?`)) return;
                try {
                    await enviar(urls.eliminar, { id: arista.data("enlaceId") });
                    panelVacio();
                    await cargar();
                    avisar("Enlace eliminado de la documentación.");
                } catch (error) { avisar(error.message); }
            });
            const acciones = crear("div", "editor-acciones");
            acciones.append(guardar, eliminar);
            elementos.push(medio.bloque, datos.bloque, stp.bloque, acciones);
        }
        panel.replaceChildren(...elementos);
    };

    const panelVacio = () => porId("mapa-seleccion").replaceChildren(crear("p", "editor-vacio", "Selecciona un equipo o un enlace."));
    cy.on("tap", "node", (evento) => { if (esNodoReal(evento.target)) mostrarNodo(evento.target); });
    cy.on("tap", "edge", (evento) => { if (evento.target.data("real") === true) mostrarArista(evento.target); });
    cy.on("tap", (evento) => { if (evento.target === cy) panelVacio(); });

    // ---------- Diálogo de nuevo enlace ----------
    const dialogo = porId("dialogo-enlace");
    const avisosDialogo = porId("enlace-avisos");
    const botonGuardarEnlace = porId("enlace-guardar");
    // extremos guarda los equipos y puertos elegidos mientras el diálogo está abierto.
    let extremos = { a: null, b: null, selectorA: null, selectorB: null };

    /** Arma el selector de puerto de un extremo: libres primero, ocupados desactivados y avisos de relación parcial. */
    const bloquePuerto = (nodo, otro, titulo) => {
        const contenedor = crear("div");
        const etiqueta = crear("label", "", `${titulo}: ${nodo.data("nombre")}`);
        contenedor.append(etiqueta);
        const puertos = [...(nodo.data("puertos") ?? [])].sort((p, q) => colador.compare(p.etiquetaVisual, q.etiquetaVisual));
        if (puertos.length === 0) {
            contenedor.append(crear("p", "editor-vacio", `${nodo.data("nombre")} no tiene interfaces documentadas: se creará la interfaz «Red 1».`));
            return { contenedor, selector: null };
        }

        const selector = crear("select");
        etiqueta.htmlFor = `puerto-${titulo.toLowerCase()}`;
        selector.id = etiqueta.htmlFor;
        const marcador = crear("option", "", "Elige un puerto");
        marcador.value = "";
        selector.append(marcador);
        let sugerido = null;
        puertos.forEach((puerto) => {
            let texto = puerto.etiquetaVisual;
            if (puerto.tipo && puerto.tipo !== "No confirmado") texto += ` · ${puerto.tipo}`;
            if (puerto.velocidadMbps) texto += ` · ${velocidadTexto(puerto.velocidadMbps)}`;
            const opcion = crear("option");
            opcion.value = String(puerto.id);
            if (puerto.enlazado) {
                opcion.disabled = true;
                texto += " (ocupado por un enlace)";
            } else if (puerto.relacionParcial) {
                if (puerto.relacionParcial.equipoId === Number(otro.id())) {
                    texto += " (documentado hacia este equipo: se convertirá en enlace completo)";
                    sugerido ??= String(puerto.id);
                } else {
                    opcion.disabled = true;
                    texto += ` (documentado hacia ${puerto.relacionParcial.destino || "otro equipo"})`;
                }
            }
            opcion.textContent = texto;
            selector.append(opcion);
        });
        const libres = puertos.filter((puerto) => !puerto.enlazado && !puerto.relacionParcial);
        selector.value = sugerido ?? (puertos.length === 1 && libres.length === 1 ? String(libres[0].id) : "");
        contenedor.append(selector);
        return { contenedor, selector };
    };

    const valorPuerto = (selector) => (selector ? Number(selector.value || 0) : 0);

    /** Consulta las reglas del servidor y muestra errores, avisos e información; solo habilita Guardar sin errores. */
    const validarDialogo = async () => {
        const { a, b, selectorA, selectorB } = extremos;
        if ((selectorA && !selectorA.value) || (selectorB && !selectorB.value)) {
            avisosDialogo.replaceChildren(crear("p", "aviso-campo", "Elige el puerto de cada extremo para validar la conexión."));
            botonGuardarEnlace.disabled = true;
            return;
        }
        try {
            const hallazgos = await pedirJson(urls.validar, {
                equipoA: a.id(), puertoA: valorPuerto(selectorA), equipoB: b.id(), puertoB: valorPuerto(selectorB)
            });
            avisosDialogo.replaceChildren(...hallazgos.map((hallazgo) => crear("p",
                `aviso-campo ${hallazgo.severidad === "error" ? "aviso-campo-error" : hallazgo.severidad === "aviso" ? "aviso-campo-aviso" : ""}`.trim(),
                hallazgo.mensaje)));
            botonGuardarEnlace.disabled = hallazgos.some((hallazgo) => hallazgo.severidad === "error");
        } catch (error) {
            avisosDialogo.replaceChildren(crear("p", "aviso-campo aviso-campo-error", error.message));
            botonGuardarEnlace.disabled = true;
        }
    };

    const abrirDialogo = (idOrigen, idDestino) => {
        const a = cy.getElementById(idOrigen);
        const b = cy.getElementById(idDestino);
        if (a.empty() || b.empty()) return;
        const bloqueA = bloquePuerto(a, b, "Origen");
        const bloqueB = bloquePuerto(b, a, "Destino");
        extremos = { a, b, selectorA: bloqueA.selector, selectorB: bloqueB.selector };
        porId("enlace-resumen").textContent = `${a.data("nombre")} ↔ ${b.data("nombre")}`;
        porId("enlace-bloque-a").replaceChildren(bloqueA.contenedor);
        porId("enlace-bloque-b").replaceChildren(bloqueB.contenedor);
        [bloqueA.selector, bloqueB.selector].forEach((selector) => selector?.addEventListener("change", validarDialogo));
        porId("enlace-medio").value = "No confirmado";
        porId("enlace-estado-datos").value = "Pendiente de revisión";
        porId("enlace-stp").value = "No documentado";
        avisosDialogo.replaceChildren();
        botonGuardarEnlace.disabled = true;
        dialogo.showModal();
        validarDialogo();
    };

    porId("enlace-cancelar").addEventListener("click", () => dialogo.close());
    botonGuardarEnlace.addEventListener("click", async () => {
        const { a, b, selectorA, selectorB } = extremos;
        botonGuardarEnlace.disabled = true;
        try {
            const respuesta = await enviar(urls.crear, {
                equipoA: a.id(), puertoA: valorPuerto(selectorA), equipoB: b.id(), puertoB: valorPuerto(selectorB),
                medio: porId("enlace-medio").value, estadoDatos: porId("enlace-estado-datos").value, estadoStp: porId("enlace-stp").value
            });
            dialogo.close();
            await cargar();
            const nueva = cy.getElementById(`e${respuesta.enlaceId}`);
            if (nueva.nonempty()) { nueva.select(); mostrarArista(nueva); }
            avisar(respuesta.avisos?.length ? `Enlace guardado. Avisos: ${respuesta.avisos.join(" ")}` : "Enlace guardado.");
        } catch (error) {
            avisosDialogo.replaceChildren(...(error.mensajes ?? [error.message]).map((mensaje) => crear("p", "aviso-campo aviso-campo-error", mensaje)));
            botonGuardarEnlace.disabled = false;
        }
    });

    // Al soltar la línea sobre otro equipo se descarta la arista provisional y se abre el diálogo de puertos.
    cy.on("ehcomplete", (evento, origen, destino, aristaAgregada) => {
        aristaAgregada.remove();
        abrirDialogo(origen.id(), destino.id());
    });

    // Modo conectar: permite dibujar arrastrando desde el propio equipo, útil en pantallas táctiles.
    const botonConectar = porId("mapa-conectar");
    botonConectar.addEventListener("click", () => {
        if (!asistente) { avisar("La biblioteca de dibujo de enlaces no se cargó."); return; }
        const activo = botonConectar.getAttribute("aria-pressed") === "true";
        botonConectar.setAttribute("aria-pressed", String(!activo));
        botonConectar.textContent = activo ? "Modo conectar" : "Modo conectar activado";
        if (activo) asistente.disableDrawMode(); else asistente.enableDrawMode();
    });

    cargar().catch((error) => {
        avisar(error.message);
        porId("mapa-alertas").replaceChildren(crear("p", "editor-vacio", "No se pudieron cargar los datos del mapa."));
    });
})();
