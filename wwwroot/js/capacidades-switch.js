(() => {
    "use strict";

    // selectoresTipo encuentra formularios de inventario; cambiar el selector altera qué tipo activa capacidades.
    const selectoresTipo = document.querySelectorAll('select[name="TipoDispositivo"]');

    /**
     * Muestra o desactiva los contadores de puertos según el tipo seleccionado.
     */
    const configurarFormulario = (selectorTipo) => {
        // formularioInventario delimita los campos que controla este selector.
        const formularioInventario = selectorTipo.closest("form");
        // grupoCapacidades contiene los contadores exclusivos de switches.
        const grupoCapacidades = formularioInventario?.querySelector("[data-capacidades-switch]");
        if (!grupoCapacidades) {
            return;
        }

        // actualizarCapacidades sincroniza visibilidad y envío de los campos condicionales.
        const actualizarCapacidades = () => {
            const esSwitch = selectorTipo.value === "Switch";
            grupoCapacidades.hidden = !esSwitch;
            grupoCapacidades.querySelectorAll("input").forEach(entrada => {
                entrada.disabled = !esSwitch;
            });
        };

        selectorTipo.addEventListener("change", actualizarCapacidades);
        actualizarCapacidades();
    };

    selectoresTipo.forEach(configurarFormulario);
})();
