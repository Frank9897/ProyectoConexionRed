// iconos-equipo asocia cada tipo de dispositivo con su ícono (carpeta wwwroot/images/iconos) y su color de acento.
// Lo usan el editor del mapa y el alta de equipos; un tipo desconocido recibe el ícono genérico.
(() => {
    "use strict";

    // archivos relaciona el valor del select «Tipo de dispositivo» con el nombre del archivo SVG sin extensión.
    const archivos = {
        "Switch": "switch", "Router": "router", "Firewall": "firewall", "Servidor": "servidor", "Puesto de usuario": "pc",
        "Impresora": "impresora", "UPS": "ups", "Reloj": "reloj", "Cámara IP": "camara-ip", "Punto de acceso": "punto-acceso",
        "Módem": "modem", "Almacenamiento": "almacenamiento", "Equipo de red": "otro", "Otro": "otro"
    };

    // colores coinciden con el acento de cada ícono, para que el borde del equipo en el mapa y su ícono combinen.
    const colores = {
        "Switch": "#2f6f9f", "Router": "#7a5ea8", "Firewall": "#b4533c", "Servidor": "#3f7f5f", "Puesto de usuario": "#6b7685",
        "Impresora": "#8a7a3c", "UPS": "#a0662a", "Reloj": "#5b6f86", "Cámara IP": "#405c8a", "Punto de acceso": "#2d8a8a",
        "Módem": "#b07a2b", "Almacenamiento": "#4a6a8a"
    };

    window.IconosEquipo = {
        /** Nombre del archivo de ícono (sin extensión) de un tipo de dispositivo. */
        archivo: (tipo) => archivos[tipo] ?? "otro",
        /** Ruta completa del ícono dado el prefijo de la carpeta de íconos. */
        ruta: (base, tipo) => `${base}${archivos[tipo] ?? "otro"}.svg`,
        /** Color de acento del tipo. */
        color: (tipo) => colores[tipo] ?? "#8591a3",
        /** Tipos que tienen ícono propio, en el orden en que se ofrecen al crear un equipo. */
        tipos: ["Puesto de usuario", "Switch", "Router", "Firewall", "Servidor", "Impresora", "UPS", "Reloj", "Cámara IP", "Punto de acceso", "Módem", "Almacenamiento", "Otro"]
    };
})();
