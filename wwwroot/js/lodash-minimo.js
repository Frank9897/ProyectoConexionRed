// lodash-minimo aporta solo memoize y throttle, las dos funciones que cytoscape-edgehandles espera encontrar en window._.
// Debe cargarse antes que cytoscape-edgehandles.js; si ya existe una biblioteca lodash completa no se reemplaza.
(() => {
    "use strict";
    window._ = window._ || {};

    // memoize guarda el resultado por argumento; edgehandles lo usa con un solo argumento (un nodo).
    window._.memoize = window._.memoize || ((funcion) => {
        const memoria = new Map();
        return (argumento) => {
            if (!memoria.has(argumento)) {
                memoria.set(argumento, funcion(argumento));
            }
            return memoria.get(argumento);
        };
    });

    // throttle limita la frecuencia de llamadas a una por intervalo, ejecutando la primera enseguida.
    window._.throttle = window._.throttle || ((funcion, espera) => {
        let ultimaEjecucion = 0;
        return (...argumentos) => {
            const ahora = Date.now();
            if (ahora - ultimaEjecucion >= espera) {
                ultimaEjecucion = ahora;
                funcion(...argumentos);
            }
        };
    });
})();
