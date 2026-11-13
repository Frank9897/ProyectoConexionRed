// filtroBusqueda conecta la búsqueda y los filtros con las tarjetas de switches documentadas.
document.addEventListener("DOMContentLoaded", iniciarFiltrosPuertos);

// iniciarFiltrosPuertos filtra tarjetas locales sin consultar ni modificar el inventario.
function iniciarFiltrosPuertos() {
    // campoBusqueda aporta el texto para localizar nombre, área, modelo, IP o destino.
    const campoBusqueda = document.getElementById("buscar-switch-puertos");
    // filtroEstado limita las tarjetas según sus contadores documentales.
    const filtroEstado = document.getElementById("estado-switch-puertos");
    // tarjetasSwitch contiene las fichas ya renderizadas desde SQLite en el servidor.
    const tarjetasSwitch = Array.from(document.querySelectorAll(".switch-puertos-card"));
    // contadorVisible comunica cuántas tarjetas pasan los filtros.
    const contadorVisible = document.getElementById("contador-switch-puertos");
    // avisoSinResultados se muestra cuando el filtro no encuentra coincidencias.
    const avisoSinResultados = document.getElementById("sin-resultados-switch-puertos");

    if (!campoBusqueda || !filtroEstado || !contadorVisible || !avisoSinResultados) {
        return;
    }

    // actualizarResultados aplica texto y estado a las tarjetas existentes.
    const actualizarResultados = () => {
        // textoBusqueda normaliza tildes y mayúsculas para facilitar la búsqueda.
        const textoBusqueda = campoBusqueda.value.trim().toLocaleLowerCase("es").normalize("NFD").replace(/[\u0300-\u036f]/g, "");
        // estadoElegido conserva la categoría escogida en el selector.
        const estadoElegido = filtroEstado.value;
        // cantidadVisible contabiliza las tarjetas que siguen disponibles en la vista.
        let cantidadVisible = 0;

        tarjetasSwitch.forEach((tarjetaSwitch) => {
            // textoTarjeta incluye el contenido visible de nombre, IP y destinos.
            const textoTarjeta = tarjetaSwitch.textContent.toLocaleLowerCase("es").normalize("NFD").replace(/[\u0300-\u036f]/g, "");
            // cantidadOcupada identifica interfaces con conexión documentada.
            const cantidadOcupada = Number(tarjetaSwitch.dataset.puertosOcupados || 0);
            // cantidadDesconocida identifica puertos cuyo uso o vigencia falta confirmar.
            const cantidadDesconocida = Number(tarjetaSwitch.dataset.puertosDesconocidos || 0);
            // coincideTexto exige que la ficha contenga el texto buscado.
            const coincideTexto = textoTarjeta.includes(textoBusqueda);
            // coincideEstado aplica la opción seleccionada sin inferir una conexión nueva.
            const coincideEstado =
                estadoElegido === "" ||
                (estadoElegido === "con-conexiones" && cantidadOcupada > 0) ||
                (estadoElegido === "pendientes" && cantidadDesconocida > 0) ||
                (estadoElegido === "solo-confirmados" && cantidadDesconocida === 0) ||
                (estadoElegido === "sin-conexiones" && cantidadOcupada === 0);
            // mostrarTarjeta combina las dos condiciones para esta ficha.
            const mostrarTarjeta = coincideTexto && coincideEstado;

            tarjetaSwitch.hidden = !mostrarTarjeta;
            if (mostrarTarjeta) {
                cantidadVisible += 1;
            }
        });

        contadorVisible.textContent = `Mostrando ${cantidadVisible} de ${tarjetasSwitch.length} switches`;
        avisoSinResultados.hidden = cantidadVisible > 0;
    };

    campoBusqueda.addEventListener("input", actualizarResultados);
    filtroEstado.addEventListener("change", actualizarResultados);
    actualizarResultados();
}
