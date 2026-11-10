# Cotejo de tablas incrustadas en DIAGRAMA RED V3.0

**Fuente revisada:** `DIAGRAMA RED V3.0.vsdx`, incluidas sus hojas Excel incrustadas. El usuario confirmó que los datos de dispositivos y conexiones del V3 están revisados. Este registro sustituye las notas previas que marcaban genéricamente las tablas del rack como “a verificar”.

## Criterio de capas de la aplicación

- **Capa 1 · Física:** patcheras, recorridos de cable y terminaciones físicas. El libro `Excel2` registra una patchera y sus salidas; por sí sola, una fila de patchera no identifica necesariamente el puerto destino del switch. No inferir ese extremo.
- **Capa 2 · Switching:** una relación cuyo origen y destino son switches. La conexión entre esos equipos se considera confirmada si está escrita en una tabla. Si ambas tablas se identifican de forma única, la web muestra ambos puertos. Si solo una tabla identifica el puerto, la conexión entre los equipos sigue confirmada, pero el puerto remoto queda sin número.
- **Capa 3 · Acceso:** PC, impresora, servidor, interfaz de administración u otro equipo final conectado a un puerto de switch. Se muestra el puerto local del switch. No se pide ni se inventa un “puerto remoto” de la tarjeta de red del PC.
- “Confirmado” describe la procedencia documental y la confirmación del usuario; no representa una prueba de conectividad en vivo.
- El nombre o etiqueta “principal” identifica el switch principal del área/topología cuando así aparece. Un switch señalado como no principal puede enlazar un grupo de equipos al principal; no significa que sea un modelo o fabricante.
- Las correcciones confirmadas por el usuario prevalecen sobre el texto conflictivo de una tabla: ProCurve `10.0.1.1`, 3Com `192.168.1.24`, switch principal de Mesa de Entradas Aruba 1930 `192.168.1.1`, y R770 P2 es LAN `192.168.0.246` (P1/iDRAC `192.168.0.247`).

## Revisión libro por libro

| Libro incrustado | Página / contenido reconocido | Relación y capa | Resultado del cotejo |
|---|---|---|---|
| `Excel` | Esquema de conexión a Internet, ProCurve 29100I | ProCurve hacia Agrimensura HP, Sumarios, B-P10, Régimen, Valuación R1/R2, Asesoría y Cartografía; switching (Capa 2) cuando el destino es switch | El diagrama enumera uplinks por puerto. B-P10 se conserva como rótulo hasta resolver su ficha sin ambigüedad. |
| `Excel1` | Dell R770 | P1/iDRAC y P2/LAN son endpoints de servidor (Capa 3); P3 libre y P4 no configurado | P1 = `.247`; P2 = `.246` por corrección del usuario. |
| `Excel2` | Patchera 1, salidas | Panel y cableado (Capa 1); las salidas 9–16 identifican switches de área, también Sala P2 en salida 2 | Importar y mostrar el extremo de patchera documentado; no inventar el puerto de switch del otro extremo si no figura. |
| `Excel3` | HP 5500, tabla RJ45 de rack | Servidores/gestión (Capa 3); ProCurve `.1.1` y HP 1920S H `.217` son uplinks (Capa 2) | Se mantienen los estados explícitos libre/desconocido. |
| `Excel4` | HP 5500, tabla SFP | Interfaces SFP de rack; filas listadas como libres | Importar con etiqueta `SFP n` para distinguirlas de la interfaz RJ45 que pudiera compartir número. La hoja no especifica si son jaulas combo; no se deduce su comportamiento. |
| `Excel5` | HP 1920 (E), tabla principal de rack | Switches destino: S4148U, Aruba Mesa de Entradas principal, Cartografía, Valuación R1/R2, Sistemas y HP 1920S (H): Capa 2. pfSense, servidores e iDRAC: Capa 3 | La fila de puerto 24 hacia HP 1920S (H) es cotejable con `Excel7`. El registro tiene dos destinos documentados al S4148U (P1 y P23); su extremo remoto en `Excel8` no permite asignar ambos puertos sin suponer. |
| `Excel6` | HP 1920 (E), tabla SFP | Uplinks a Secretaría, Agrimensura HP e Inmuebles (Capa 2); otras posiciones enumeradas libres | Cotejos recíprocos exactos resumidos debajo. Importar SFP como etiqueta tipada separada de RJ45. |
| `Excel7` | HP 1920S (H), tabla RJ45 | UPS, Cisco, VirtualXP44 y R920 iDRAC: Capa 3; HP 1920 (E) y HP 5500: Capa 2 | Puerto 22 al HP 1920 (E) y puerto 24 al HP 5500 se emparejan por las tablas opuestas. |
| `Excel8` | Dell S4148U, tabla SFP+ | Sumarios y HP 1920 (E): Capa 2; R770 P2: Capa 3; puertos ocupados sin destino conservan destino desconocido | P24 a HP 1920 (E) es relación confirmada a nivel de equipos. Excel5 tiene dos filas hacia S4148U y por eso no se asigna P24 a una en particular. La fila del R770 P2 usa `.246`. Estados “sin SFP” se conservan literalmente. |
| `Excel9` | Tabla resumen de switches por área/IP | Inventario, sin puertos para crear enlaces | Se aplican las correcciones confirmadas de ProCurve, 3Com y Mesa de Entradas. Régimen y Mesa de Entradas principal se registran inaccesibles por falta de credenciales, sin alterar su existencia/conexiones documentadas. |
| `Excel10` | Switch de Agrimensura HP | PCs, incluyendo AGRIMENSURA15: Capa 3; 3Com: Capa 2 | AGRIMENSURA15 es equipo de usuario/PC; mostrarlo desde el puerto local del switch. El puerto 15 hacia 3Com coteja con `Excel12` puerto 24. |
| `Excel11` | Tabla SFP inserta en página de Agrimensura HP, encabezada “Switch HP1920” | Puerto SFP 28 hacia HP 1920 (E): Capa 2 | Se asigna a Agrimensura HP por la página y por la reciprocidad con `Excel6` SFP 27 hacia Agrimensura .23; el encabezado distinto se conserva en la referencia fuente. |
| `Excel12` | Agrimensura 3Com | PCs/impresoras: Capa 3; puerto 24 al switch HP Agrimensura: Capa 2 | Reciproca `Excel10` puerto 15 → 3Com `.24`. |
| `Excel13` | Asuntos Jurídicos | PCs/impresoras: Capa 3; puerto 24 hacia S4148U: Capa 2 | La relación al S4148U aparece en un solo extremo. |
| `Excel14` | Cartografía | PCs: Capa 3; puerto 24 a HP 1920 (E): Capa 2 | Recíproca con `Excel5` puerto 13 → Cartografía `.51`. |
| `Excel15` | Inmuebles principal | Usuarios: Capa 3; puerto 24 hacia switch secundario de 8 puertos: Capa 2 | La conexión entre switches consta en el principal. |
| `Excel16` | Switch secundario de 8 puertos de Inmuebles | Equipos en puertos 1–6; 7–8 libres | No aparece una fila de retorno al principal en esta tabla: no asignar puerto remoto. |
| `Excel17` | Tabla SFP inserta en Inmuebles, encabezada HP1910 | SFP 28 a HP 1920 (E): Capa 2 | Se asigna al switch Inmuebles por la página y por la reciprocidad con `Excel6` SFP 28 → Inmuebles .26. |
| `Excel18` | Mesa de Entradas no principal | MENTRADA04 y MENTRADA02: Capa 3; puerto 24 al Aruba principal: Capa 2 | El principal es Aruba 1930 `192.168.1.1`; inaccesibilidad administrativa no invalida el enlace documentado. |
| `Excel19` | Puerta de Agrimensura | Equipos finales: Capa 3; ProCurve y Aruba principal: Capa 2 | Usar ProCurve `10.0.1.1` según corrección del usuario (no el octeto erróneo de la tabla). |
| `Excel20` | Regularización Dominial | Usuarios/equipos finales: Capa 3; puerto 24 a HP 1920 (E): Capa 2 | Enlace al HP 1920 (E) cotejable con `Excel5` cuando la identidad del área coincide. |
| `Excel21` | Secretaría | PCs, equipos Trimble e impresora: Capa 3; puerto 24 libre | Las filas de equipo final no requieren puerto NIC remoto. |
| `Excel22` | Tabla SFP de Secretaría | SFP 26 a HP 1920 (E): Capa 2 | Recíproca con `Excel6` SFP 26 → Secretaría SFP 28. |
| `Excel23` | Sistemas | PCs/servicios: Capa 3; puerto 24 al HP 1920 (E): Capa 2 | El enlace de switch coteja con `Excel5` puerto 21 → Sistemas `.216`. Se conservan sin resolver las filas con más de una IP, no escoger una por inferencia. |
| `Excel24` | Valuación R0 | Usuarios: Capa 3; puerto 24 a “Switch Agrimensura” `.24`: Capa 2 | Enlace de una sola dirección en esta tabla; el destino se conserva como rotulado aunque otras filas de Agrimensura apunten a otro switch. |
| `Excel25` | Valuación R1 | Usuarios: Capa 3; puertos 23 a R2 y 24 a HP 1920 (E): Capa 2 | Puerto 23 recíproco con `Excel26` puerto 14; puerto 24 con `Excel5` puerto 19. |
| `Excel26` | Valuación R2 | Usuarios: Capa 3; puerto 14 a R1, 23 a R0 y 24 a HP 1920 (E): Capa 2 | Puerto 14 recíproco con `Excel25` puerto 23. Puerto 24 coteja con `Excel5` puerto 20. La referencia R0 queda de una sola dirección, dado que su puerto 24 apunta a 3Com. |
| `Excel27` | Reserva 1, RJ45 | Puertos 1–24 libres | Capacidad libre documentada; no se crean enlaces. |
| `Excel28` | Reserva, SFP; encabezado HP1910 | Posiciones listadas libres | Conservar encabezado como referencia; no asignar modelo al switch solo por el título de la tabla. |
| `Excel29` | Reserva 2, RJ45 | Puertos 1–24 libres | Capacidad libre documentada; no se crean enlaces. |
| `Excel30` | Reserva 2, SFP | Posiciones listadas libres | Mantener separadas las interfaces SFP; no inferir módulo instalado. |

## Pares switch-a-switch cotejados

| Extremo A | Extremo B | Evidencia |
|---|---|---|
| HP 1920 (E) RJ45 24 | HP 1920S (H) RJ45 22 | `Excel5` ↔ `Excel7` |
| HP 5500 RJ45 24 | HP 1920S (H) RJ45 24 | `Excel3` ↔ `Excel7` |
| Agrimensura HP RJ45 15 | Agrimensura 3Com RJ45 24 | `Excel10` ↔ `Excel12` |
| Cartografía RJ45 24 | HP 1920 (E) RJ45 13 | `Excel14` ↔ `Excel5` |
| HP 1920 (E) SFP 26 | Secretaría SFP 26 | `Excel6` ↔ `Excel22` |
| HP 1920 (E) SFP 27 | Agrimensura HP SFP 28 | `Excel6` ↔ `Excel11`; asignación validada por página y nombre/IP |
| HP 1920 (E) SFP 28 | Inmuebles SFP 28 | `Excel6` ↔ `Excel17` |
| Valuación R1 RJ45 23 | Valuación R2 RJ45 14 | `Excel25` ↔ `Excel26` |
| HP 1920 (E) RJ45 19 | Valuación R1 RJ45 24 | `Excel5` ↔ `Excel25` |
| HP 1920 (E) RJ45 20 | Valuación R2 RJ45 24 | `Excel5` ↔ `Excel26` |

Relaciones confirmadas entre equipos pero sin emparejamiento único de puertos incluyen HP 1920 (E) ↔ S4148U (dos entradas desde el HP1920 y una respuesta en S4148U), Inmuebles principal ↔ switch de 8 puertos (sin puerto de retorno) y referencias a switches de una sola tabla. Se muestran como relaciones de Capa 2 con el puerto remoto vacío, no como conexiones dudosas.

## Cambios aplicados a la web

- La topología separa acceso (Capa 3), switching (Capa 2) y físico (Capa 1); el detalle de un switch identifica los endpoints como PC/usuario/servicio y no imprime un puerto remoto ficticio para ellos.
- Las filas parciales del inventario se cotejan por IP/nombre y se emparejan solo cuando existe una única relación recíproca en cada sentido.
- Los enlaces de un solo extremo permanecen visibles como relación confirmada entre equipos; un estilo distinto señala únicamente que no hay contraparte única.
- El importador incorpora las filas SFP documentadas y los puertos de rack aparecen confirmados según la instrucción del usuario.

- Para que Capa 3 dibuje la línea aunque la PC no tenga ficha propia en SQLite, el mapa genera un nodo visual temporal con los campos literales de nombre/IP de la relación. Este nodo no se guarda como dispositivo ni inventa atributos; cuando ya existe una ficha que coincide, se usa la ficha del inventario.
