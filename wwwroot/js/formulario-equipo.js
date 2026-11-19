(() => {
    "use strict";

    // selectorTipo identifica los formularios de alta y edición de equipos; sin él este script no hace nada.
    const selectorTipo = document.querySelector('select[name="TipoDispositivo"]');
    if (!selectorTipo) {
        return;
    }

    // formulario delimita los campos controlados; baseInventario permite llamar a las acciones de ayuda del mismo controlador.
    const formulario = selectorTipo.closest("form");
    const baseInventario = new URL("./", formulario.action);
    const campo = (nombre) => formulario.querySelector(`[name="${nombre}"]`);
    // identificadorEquipo es nulo en un alta; el servidor lo usa para no tratar la IP propia como duplicada.
    const identificadorEquipo = Number(campo("Identificador")?.value || 0) || null;

    /**
     * Consulta una acción de ayuda del servidor y devuelve su JSON; nada se guarda.
     */
    const pedir = async (accion, parametros) => {
        const direccion = new URL(accion, baseInventario);
        Object.entries(parametros).forEach(([clave, valor]) => {
            if (valor !== null && valor !== undefined && valor !== "") {
                direccion.searchParams.set(clave, valor);
            }
        });
        const respuesta = await fetch(direccion, { headers: { Accept: "application/json" } });
        if (!respuesta.ok) {
            throw new Error(`Error ${respuesta.status}`);
        }
        return respuesta.json();
    };

    /** Evita lanzar una consulta por cada tecla: espera a que el técnico deje de escribir. */
    const conRetraso = (funcion, milisegundos = 350) => {
        let temporizador = 0;
        return (...argumentos) => {
            window.clearTimeout(temporizador);
            temporizador = window.setTimeout(() => funcion(...argumentos), milisegundos);
        };
    };

    const crear = (etiqueta, clase, texto) => {
        const elemento = document.createElement(etiqueta);
        if (clase) elemento.className = clase;
        if (texto) elemento.textContent = texto;
        return elemento;
    };

    /** Muestra mensajes bajo un campo usando textContent, nunca HTML, para no interpretar datos externos. */
    const mostrarAvisos = (contenedor, avisos) => {
        contenedor.replaceChildren(...avisos.map(aviso => {
            const clase = aviso.severidad === "error" ? "aviso-campo-error"
                : aviso.severidad === "aviso" ? "aviso-campo-aviso" : "";
            return crear("p", `aviso-campo ${clase}`.trim(), aviso.mensaje);
        }));
    };

    // ---------- IP: sugerencia de libres y validación en vivo ----------
    const ip = campo("IpAdministracion");
    const mac = campo("DireccionMac");
    const nombre = campo("Nombre");
    const fabricante = campo("Fabricante");
    const modelo = campo("Modelo");
    const administrable = campo("EsAdministrable");

    const avisosIp = crear("div");
    const avisosMac = crear("div");
    const cajaEscaneo = crear("div");
    const cajaTipo = crear("div");
    const listaIpsLibres = crear("datalist");
    listaIpsLibres.id = "ips-libres";

    if (ip) {
        ip.setAttribute("list", listaIpsLibres.id);
        ip.setAttribute("autocomplete", "off");
        const selectorRed = crear("select");
        [["", "Red de la IP"], ["0", "192.168.0.x (usuarios)"], ["1", "192.168.1.x (equipos de red)"]]
            .forEach(([valor, texto]) => {
                const opcion = crear("option", "", texto);
                opcion.value = valor;
                selectorRed.append(opcion);
            });
        const botonSugerir = crear("button", "boton boton-secundario", "Sugerir una IP libre");
        botonSugerir.type = "button";
        const ayuda = crear("div", "ayuda-ip");
        ayuda.append(selectorRed, botonSugerir);
        ip.parentElement.append(ayuda, listaIpsLibres, avisosIp, cajaEscaneo);

        /** Carga las IP libres de la red elegida en la lista desplegable del campo. */
        const cargarLibres = async () => {
            const red = selectorRed.value || (ip.value.trim().startsWith("192.168.1.") ? "1" : "0");
            const libres = await pedir("IpsLibres", { red, actual: ip.value.trim() });
            listaIpsLibres.replaceChildren(...libres.map(valor => {
                const opcion = crear("option");
                opcion.value = valor;
                return opcion;
            }));
            return libres;
        };

        botonSugerir.addEventListener("click", async () => {
            try {
                const libres = await cargarLibres();
                if (libres.length === 0) {
                    mostrarAvisos(avisosIp, [{ severidad: "aviso", mensaje: "No quedan IP libres en esa red." }]);
                    return;
                }
                ip.value = libres[0];
                ip.dispatchEvent(new Event("change", { bubbles: true }));
            } catch {
                mostrarAvisos(avisosIp, [{ severidad: "aviso", mensaje: "No se pudieron consultar las IP libres." }]);
            }
        });
        selectorRed.addEventListener("change", () => cargarLibres().catch(() => { }));
        ip.addEventListener("focus", () => cargarLibres().catch(() => { }), { once: true });

        const validarIp = conRetraso(async () => {
            try {
                mostrarAvisos(avisosIp, await pedir("ValidarIp", { ip: ip.value.trim(), id: identificadorEquipo }));
            } catch {
                avisosIp.replaceChildren();
            }
        });
        ip.addEventListener("input", validarIp);
        ip.addEventListener("change", validarIp);
        if (ip.value.trim()) {
            validarIp();
        }
    }

    // ---------- MAC: formato uniforme ----------
    if (mac) {
        mac.parentElement.append(avisosMac);
        mac.addEventListener("blur", () => {
            const digitos = mac.value.replace(/[^0-9a-f]/gi, "").toUpperCase();
            if (digitos.length === 12) {
                mac.value = digitos.match(/.{2}/g).join(":");
            }
            const valida = /^([0-9A-F]{2}:){5}[0-9A-F]{2}$/.test(mac.value.trim());
            mostrarAvisos(avisosMac, mac.value.trim() && !valida
                ? [{ severidad: "aviso", mensaje: "La MAC debe tener 12 dígitos hexadecimales, por ejemplo AA:BB:CC:DD:EE:FF." }]
                : []);
        });
    }

    // ---------- Escaneo de la LAN y tipo sugerido ----------
    selectorTipo.parentElement.append(cajaTipo);

    /** Completa solo los campos vacíos con lo que vio el escaneo; nunca pisa lo que el técnico escribió. */
    const completarVacios = (datos) => {
        const rellenar = (elemento, valor) => {
            if (elemento && valor && !elemento.value.trim()) {
                elemento.value = valor;
                elemento.dispatchEvent(new Event("change", { bubbles: true }));
            }
        };
        rellenar(ip, datos.ip);
        rellenar(nombre, datos.nombre);
        rellenar(fabricante, datos.fabricante);
        rellenar(mac, datos.mac);
    };

    const consultarEscaneo = conRetraso(async () => {
        try {
            const datos = await pedir("SugerirDesdeEscaneo", {
                ip: ip?.value.trim(), mac: mac?.value.trim(), nombre: nombre?.value.trim(),
                fabricante: fabricante?.value.trim(), modelo: modelo?.value.trim()
            });

            cajaEscaneo.replaceChildren();
            if (datos.encontrado) {
                const partes = [datos.nombre ? `nombre ${datos.nombre}` : "sin nombre resuelto",
                    datos.mac ? `MAC ${datos.mac}` : "sin MAC válida",
                    datos.fabricante ? `fabricante ${datos.fabricante}` : "fabricante no identificado",
                    datos.activo ? "activo al escanear" : "inactivo al escanear"];
                if (datos.otrasIps?.length) partes.push(`también responde en ${datos.otrasIps.join(", ")}`);
                const mensaje = crear("p", "aviso-campo", `El escaneo de la LAN vio este equipo: ${partes.join("; ")}.`);
                const faltan = (ip && !ip.value.trim() && datos.ip) || (nombre && !nombre.value.trim() && datos.nombre)
                    || (fabricante && !fabricante.value.trim() && datos.fabricante) || (mac && !mac.value.trim() && datos.mac);
                if (faltan) {
                    const boton = crear("button", "boton boton-secundario", "Completar campos vacíos");
                    boton.type = "button";
                    boton.addEventListener("click", () => completarVacios(datos));
                    mensaje.append(boton);
                }
                cajaEscaneo.append(mensaje);
            }

            cajaTipo.replaceChildren();
            if (datos.tipoSugerido && datos.tipoSugerido !== selectorTipo.value) {
                const mensaje = crear("p", "aviso-campo", `Sugerencia de tipo: ${datos.tipoSugerido} (${datos.motivoTipo}).`);
                const boton = crear("button", "boton boton-secundario", "Aplicar");
                boton.type = "button";
                boton.addEventListener("click", () => {
                    selectorTipo.value = datos.tipoSugerido;
                    selectorTipo.dispatchEvent(new Event("change", { bubbles: true }));
                    cajaTipo.replaceChildren();
                });
                mensaje.append(boton);
                cajaTipo.append(mensaje);
            }
        } catch {
            cajaEscaneo.replaceChildren();
        }
    }, 500);

    [ip, mac, nombre, fabricante, modelo].forEach(elemento => elemento?.addEventListener("change", consultarEscaneo));
    selectorTipo.addEventListener("change", () => cajaTipo.replaceChildren());

    // ---------- Switch: administrable según la IP ----------
    const notaAdministrable = crear("small", "ayuda-formulario");
    administrable?.parentElement.append(notaAdministrable);
    const sugerirAdministrable = () => {
        if (!administrable || selectorTipo.value !== "Switch" || administrable.value !== "") {
            return;
        }
        const tieneIp = /^(\d{1,3}\.){3}\d{1,3}$/.test(ip?.value.trim() ?? "");
        administrable.value = tieneIp ? "true" : "false";
        notaAdministrable.textContent = tieneIp
            ? "Sugerido porque el switch tiene IP; confírmalo."
            : "Sugerido porque el switch no tiene IP; confírmalo.";
    };
    ip?.addEventListener("change", sugerirAdministrable);
    selectorTipo.addEventListener("change", sugerirAdministrable);
    administrable?.addEventListener("change", () => { notaAdministrable.textContent = ""; });

    // ---------- Capacidades: plantillas y valores habituales ----------
    const grupoCapacidades = formulario.querySelector("[data-capacidades-switch]");
    if (grupoCapacidades) {
        // plantillas fijan cantidades comunes de switches; solo se aplican al elegirlas, nunca solas.
        const plantillas = {
            "8 RJ45": { CantidadPuertosRj45: 8 },
            "24 RJ45 + 4 SFP": { CantidadPuertosRj45: 24, CantidadPuertosSfp: 4 },
            "24 RJ45 + 2 SFP": { CantidadPuertosRj45: 24, CantidadPuertosSfp: 2 },
            "48 RJ45 + 4 SFP": { CantidadPuertosRj45: 48, CantidadPuertosSfp: 4 },
            "48 RJ45 + 4 SFP+": { CantidadPuertosRj45: 48, CantidadPuertosSfpPlus: 4 }
        };
        const selectorPlantilla = crear("select");
        const primera = crear("option", "", "Plantilla de switch (opcional)");
        primera.value = "";
        selectorPlantilla.append(primera);
        Object.keys(plantillas).forEach(clave => selectorPlantilla.append(crear("option", "", clave)));
        const envoltorio = crear("div");
        envoltorio.append(crear("label", "", "Plantilla de puertos"), selectorPlantilla);
        grupoCapacidades.insertBefore(envoltorio, grupoCapacidades.children[1] ?? null);
        selectorPlantilla.addEventListener("change", () => {
            const valores = plantillas[selectorPlantilla.value];
            if (!valores) return;
            Object.entries(valores).forEach(([nombreCampo, valor]) => {
                const entrada = campo(nombreCampo);
                if (entrada) entrada.value = String(valor);
            });
        });

        // listas de valores habituales por conector; el campo sigue aceptando cualquier número.
        const habituales = {
            CantidadPuertosRj45: [8, 12, 16, 24, 48], CantidadPuertosSfp: [0, 2, 4], CantidadPuertosSfpPlus: [0, 2, 4, 6],
            CantidadPuertosQsfp: [0, 2], CantidadPuertosQsfpPlus: [0, 2, 6], CantidadPuertosQsfp28: [0, 2, 6]
        };
        Object.entries(habituales).forEach(([nombreCampo, valores]) => {
            const entrada = campo(nombreCampo);
            if (!entrada) return;
            const lista = crear("datalist");
            lista.id = `habituales-${nombreCampo}`;
            valores.forEach(valor => { const opcion = crear("option"); opcion.value = String(valor); lista.append(opcion); });
            entrada.setAttribute("list", lista.id);
            entrada.parentElement.append(lista);
        });
    }

    sugerirAdministrable();
})();
