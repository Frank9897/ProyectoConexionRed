// distribucion-red calcula una distribución jerárquica por niveles para los mapas de topología y el editor.
// Es una función pura: recibe equipos con su tamaño y las conexiones visibles, devuelve posiciones y no toca la página.
// Idea general: el equipo más central queda arriba, cada nivel de switches queda en una fila, las filas se ordenan para
// que las líneas se crucen lo menos posible y los equipos finales (con una sola conexión) se agrupan debajo de su switch.
(function (global) {
    "use strict";

    // Valores por defecto de separación en las mismas unidades que el tamaño de los equipos.
    const porDefecto = {
        separacionX: 40,          // espacio horizontal mínimo entre equipos de la misma fila
        separacionNiveles: 120,   // espacio vertical entre una fila y la siguiente (por donde pasan las líneas)
        separacionHoja: 16,       // espacio entre equipos finales agrupados bajo un switch
        maxColumnasHojas: 6,      // ancho máximo, en equipos, del bloque de equipos finales de un switch
        separacionComponentes: 120,
        anchoMaximoFila: 2400     // ancho a partir del cual se empaquetan los grupos en una nueva fila
    };

    const comparar = new Intl.Collator("es", { numeric: true, sensitivity: "base" }).compare;

    /**
     * Distribuye los equipos en niveles.
     * @param {Array<{id:string,ancho:number,alto:number,esSwitch:boolean,nombre:string}>} nodos Equipos a ubicar.
     * @param {Array<{a:string,b:string}>} enlaces Conexiones visibles entre ellos; los extremos desconocidos se ignoran.
     * @param {object} [opciones] Separaciones y límites; cualquier omitida usa su valor por defecto.
     * @returns {Map<string,{x:number,y:number}>} Esquina superior izquierda de cada equipo; la propiedad raizPrincipal
     *   indica el equipo central del grupo más grande.
     */
    function distribuirPorNiveles(nodos, enlaces, opciones) {
        const cfg = { ...porDefecto, ...(opciones || {}) };
        const resultado = new Map();
        resultado.raizPrincipal = null;
        const porId = new Map(nodos.map((nodo) => [nodo.id, nodo]));
        const vecinos = new Map(nodos.map((nodo) => [nodo.id, new Set()]));
        (enlaces || []).forEach(({ a, b }) => {
            if (a !== b && vecinos.has(a) && vecinos.has(b)) {
                vecinos.get(a).add(b);
                vecinos.get(b).add(a);
            }
        });

        // Grupos conectados (componentes) y equipos aislados.
        const visitados = new Set();
        const componentes = [];
        const aislados = [];
        nodos.forEach((nodo) => {
            if (visitados.has(nodo.id)) return;
            visitados.add(nodo.id);
            if (vecinos.get(nodo.id).size === 0) { aislados.push(nodo); return; }
            const cola = [nodo.id];
            for (let i = 0; i < cola.length; i++) {
                vecinos.get(cola[i]).forEach((vecino) => {
                    if (!visitados.has(vecino)) { visitados.add(vecino); cola.push(vecino); }
                });
            }
            componentes.push(cola);
        });
        componentes.sort((p, q) => q.length - p.length);

        const bloques = componentes.map((ids) => distribuirComponente(ids, porId, vecinos, cfg));
        if (componentes.length > 0) resultado.raizPrincipal = bloques[0].raiz;

        // Empaquetado: el grupo más grande arriba a la izquierda; los demás se acomodan en filas debajo o al costado.
        const anchoFila = Math.max(cfg.anchoMaximoFila, bloques.length ? bloques[0].ancho : 0);
        let cursorX = 0;
        let cursorY = 0;
        let altoFila = 0;
        bloques.forEach((bloque) => {
            if (cursorX > 0 && cursorX + bloque.ancho > anchoFila) {
                cursorX = 0;
                cursorY += altoFila + cfg.separacionComponentes;
                altoFila = 0;
            }
            bloque.posiciones.forEach((posicion, id) => resultado.set(id, { x: cursorX + posicion.x, y: cursorY + posicion.y }));
            cursorX += bloque.ancho + cfg.separacionComponentes;
            altoFila = Math.max(altoFila, bloque.alto);
        });

        // Los equipos sin ninguna conexión visible van al final, en filas.
        if (aislados.length > 0) {
            aislados.sort((p, q) => comparar(p.nombre || "", q.nombre || ""));
            let x = 0;
            let y = bloques.length ? cursorY + altoFila + cfg.separacionComponentes : 0;
            let altoLinea = 0;
            aislados.forEach((nodo) => {
                if (x > 0 && x + nodo.ancho > anchoFila) { x = 0; y += altoLinea + cfg.separacionHoja; altoLinea = 0; }
                resultado.set(nodo.id, { x, y });
                x += nodo.ancho + cfg.separacionHoja;
                altoLinea = Math.max(altoLinea, nodo.alto);
            });
        }
        return resultado;
    }

    /** Distribuye un grupo conectado y devuelve posiciones relativas a su esquina superior izquierda. */
    function distribuirComponente(ids, porId, vecinos, cfg) {
        const enComponente = new Set(ids);
        const grado = (id) => vecinos.get(id).size;

        // Hojas: equipos finales con una sola conexión que cuelgan de un switch (o de un equipo con más conexiones).
        const hojasDe = new Map();
        const esHoja = new Set();
        ids.forEach((id) => {
            const nodo = porId.get(id);
            if (nodo.esSwitch || grado(id) !== 1) return;
            const [dueno] = [...vecinos.get(id)];
            const nodoDueno = porId.get(dueno);
            if (nodoDueno.esSwitch || grado(dueno) > 1) {
                esHoja.add(id);
                if (!hojasDe.has(dueno)) hojasDe.set(dueno, []);
                hojasDe.get(dueno).push(nodo);
            }
        });
        hojasDe.forEach((lista) => lista.sort((p, q) => comparar(p.nombre || "", q.nombre || "")));

        // Esqueleto: todo lo que no es hoja; sobre él se calculan los niveles.
        const esqueleto = ids.filter((id) => !esHoja.has(id));
        const vecinosEsqueleto = new Map(esqueleto.map((id) => [id, [...vecinos.get(id)].filter((v) => enComponente.has(v) && !esHoja.has(v))]));
        const recorrer = (raiz) => {
            const distancia = new Map([[raiz, 0]]);
            const cola = [raiz];
            for (let i = 0; i < cola.length; i++) {
                vecinosEsqueleto.get(cola[i]).forEach((v) => {
                    if (!distancia.has(v)) { distancia.set(v, distancia.get(cola[i]) + 1); cola.push(v); }
                });
            }
            return distancia;
        };

        // Raíz: el equipo más central (menor distancia máxima); a igualdad el de más conexiones y luego un switch.
        let raiz = esqueleto[0];
        let mejor = null;
        esqueleto.forEach((id) => {
            const excentricidad = Math.max(...recorrer(id).values());
            const puntaje = [excentricidad, -grado(id), porId.get(id).esSwitch ? 0 : 1, porId.get(id).nombre || ""];
            if (!mejor || puntaje[0] < mejor[0]
                || (puntaje[0] === mejor[0] && (puntaje[1] < mejor[1]
                    || (puntaje[1] === mejor[1] && (puntaje[2] < mejor[2]
                        || (puntaje[2] === mejor[2] && comparar(puntaje[3], mejor[3]) < 0)))))) {
                mejor = puntaje;
                raiz = id;
            }
        });
        const nivel = recorrer(raiz);
        // Equipos del esqueleto no alcanzados (no debería ocurrir) quedan en el nivel siguiente al último.
        const nivelMaximo = Math.max(...nivel.values());
        esqueleto.forEach((id) => { if (!nivel.has(id)) nivel.set(id, nivelMaximo + 1); });

        const filas = [];
        esqueleto.forEach((id) => {
            const n = nivel.get(id);
            while (filas.length <= n) filas.push([]);
            filas[n].push(id);
        });
        filas.forEach((fila) => fila.sort((p, q) => comparar(porId.get(p).nombre || "", porId.get(q).nombre || "")));

        // Reducción de cruces: barrido por baricentro hacia abajo y hacia arriba, varias veces.
        const indiceEnFila = new Map();
        const reindexar = () => filas.forEach((fila) => fila.forEach((id, i) => indiceEnFila.set(id, i)));
        reindexar();
        const ordenarPorVecinos = (fila, nivelVecino) => {
            const clave = new Map(fila.map((id) => {
                const indices = vecinosEsqueleto.get(id).filter((v) => nivel.get(v) === nivelVecino).map((v) => indiceEnFila.get(v));
                return [id, indices.length ? indices.reduce((s, v) => s + v, 0) / indices.length : indiceEnFila.get(id)];
            }));
            fila.sort((p, q) => (clave.get(p) - clave.get(q)) || comparar(porId.get(p).nombre || "", porId.get(q).nombre || ""));
        };
        for (let pasada = 0; pasada < 6; pasada++) {
            for (let n = 1; n < filas.length; n++) { ordenarPorVecinos(filas[n], n - 1); reindexar(); }
            for (let n = filas.length - 2; n >= 0; n--) { ordenarPorVecinos(filas[n], n + 1); reindexar(); }
        }

        // Medidas de cada equipo: espacio horizontal que ocupa (incluye su bloque de hojas) y altura total.
        const medida = new Map();
        esqueleto.forEach((id) => {
            const nodo = porId.get(id);
            const hojas = hojasDe.get(id) || [];
            if (hojas.length === 0) {
                medida.set(id, { ancho: nodo.ancho, alto: nodo.alto, bloque: null, izq: nodo.ancho / 2, der: nodo.ancho / 2, alLado: false });
                return;
            }
            const columnas = Math.min(cfg.maxColumnasHojas, Math.max(1, Math.ceil(Math.sqrt(hojas.length))));
            const celdaAncho = Math.max(...hojas.map((h) => h.ancho));
            const celdaAlto = Math.max(...hojas.map((h) => h.alto));
            const cantidadFilas = Math.ceil(hojas.length / columnas);
            const bloque = {
                columnas, celdaAncho, celdaAlto,
                ancho: columnas * celdaAncho + (columnas - 1) * cfg.separacionHoja,
                alto: cantidadFilas * celdaAlto + (cantidadFilas - 1) * cfg.separacionHoja
            };
            // Si el switch tiene switches debajo, su bloque de equipos finales va a su derecha para no tapar esas líneas.
            const tieneHijos = vecinosEsqueleto.get(id).some((v) => nivel.get(v) === nivel.get(id) + 1);
            if (tieneHijos) {
                medida.set(id, {
                    ancho: nodo.ancho + cfg.separacionHoja * 2 + bloque.ancho,
                    alto: Math.max(nodo.alto, bloque.alto),
                    bloque, izq: nodo.ancho / 2, der: nodo.ancho / 2 + cfg.separacionHoja * 2 + bloque.ancho, alLado: true
                });
            } else {
                const mitad = Math.max(nodo.ancho, bloque.ancho) / 2;
                medida.set(id, {
                    ancho: Math.max(nodo.ancho, bloque.ancho),
                    alto: nodo.alto + cfg.separacionHoja * 2 + bloque.alto,
                    bloque, izq: mitad, der: mitad, alLado: false
                });
            }
        });

        // Altura de cada fila y posición vertical.
        const yFila = [];
        let y = 0;
        filas.forEach((fila, n) => {
            yFila[n] = y;
            y += Math.max(...fila.map((id) => medida.get(id).alto)) + cfg.separacionNiveles;
        });

        // Posición horizontal: cada equipo busca quedar bajo el promedio de sus conexiones del nivel anterior;
        // luego se resuelven solapes en la fila y se hace una pasada de ajuste hacia arriba y otra hacia abajo.
        const centro = new Map();
        const resolverFila = (fila, deseados) => {
            // separacion entre los centros de dos equipos vecinos: lo que sobresale por la derecha del primero,
            // lo que sobresale por la izquierda del segundo y el espacio mínimo.
            const separacion = (izquierdo, derecho) => medida.get(izquierdo).der + medida.get(derecho).izq + cfg.separacionX;
            const adelante = [];
            fila.forEach((id, i) => {
                const minimo = i === 0 ? -Infinity : adelante[i - 1] + separacion(fila[i - 1], id);
                adelante.push(Math.max(deseados.get(id), minimo));
            });
            const atras = new Array(fila.length);
            for (let i = fila.length - 1; i >= 0; i--) {
                const maximo = i === fila.length - 1 ? Infinity : atras[i + 1] - separacion(fila[i], fila[i + 1]);
                atras[i] = Math.min(deseados.get(fila[i]), maximo);
            }
            const final = [];
            fila.forEach((id, i) => {
                const promedio = (adelante[i] + atras[i]) / 2;
                const minimo = i === 0 ? -Infinity : final[i - 1] + separacion(fila[i - 1], id);
                final.push(Math.max(promedio, minimo));
            });
            fila.forEach((id, i) => centro.set(id, final[i]));
        };
        const deseadosDesde = (fila, nivelVecino, mezcla) => new Map(fila.map((id) => {
            const x = vecinosEsqueleto.get(id).filter((v) => nivel.get(v) === nivelVecino && centro.has(v)).map((v) => centro.get(v));
            const actual = centro.get(id);
            if (x.length === 0) return [id, actual ?? 0];
            const promedio = x.reduce((s, v) => s + v, 0) / x.length;
            return [id, actual === undefined ? promedio : actual * (1 - mezcla) + promedio * mezcla];
        }));
        // Nivel 0: repartido de forma pareja alrededor de cero.
        resolverFila(filas[0], new Map(filas[0].map((id, i) => [id, i * 1])));
        for (let n = 1; n < filas.length; n++) resolverFila(filas[n], deseadosDesde(filas[n], n - 1, 1));
        for (let n = filas.length - 2; n >= 0; n--) resolverFila(filas[n], deseadosDesde(filas[n], n + 1, 0.6));
        for (let n = 1; n < filas.length; n++) resolverFila(filas[n], deseadosDesde(filas[n], n - 1, 0.6));

        // Posiciones finales (esquina superior izquierda) y bloque de hojas bajo cada switch.
        const posiciones = new Map();
        esqueleto.forEach((id) => {
            const nodo = porId.get(id);
            const c = centro.get(id);
            const arriba = yFila[nivel.get(id)];
            posiciones.set(id, { x: c - nodo.ancho / 2, y: arriba });
            const m = medida.get(id);
            if (!m.bloque) return;
            const origenX = m.alLado ? c + nodo.ancho / 2 + cfg.separacionHoja * 2 : c - m.bloque.ancho / 2;
            const origenY = m.alLado ? arriba : arriba + nodo.alto + cfg.separacionHoja * 2;
            (hojasDe.get(id) || []).forEach((hoja, i) => {
                const columna = i % m.bloque.columnas;
                const fila = Math.floor(i / m.bloque.columnas);
                posiciones.set(hoja.id, {
                    x: origenX + columna * (m.bloque.celdaAncho + cfg.separacionHoja) + (m.bloque.celdaAncho - hoja.ancho) / 2,
                    y: origenY + fila * (m.bloque.celdaAlto + cfg.separacionHoja)
                });
            });
        });

        // Normaliza para que el grupo empiece en (0,0) y calcula su tamaño.
        let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
        posiciones.forEach((posicion, id) => {
            const nodo = porId.get(id);
            minX = Math.min(minX, posicion.x); minY = Math.min(minY, posicion.y);
            maxX = Math.max(maxX, posicion.x + nodo.ancho); maxY = Math.max(maxY, posicion.y + nodo.alto);
        });
        posiciones.forEach((posicion) => { posicion.x -= minX; posicion.y -= minY; });
        return { posiciones, ancho: maxX - minX, alto: maxY - minY, raiz };
    }

    global.DistribucionRed = { distribuirPorNiveles };
})(typeof window !== "undefined" ? window : globalThis);
