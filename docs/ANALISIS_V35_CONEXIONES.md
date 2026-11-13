# Análisis de conexiones — DIAGRAMA RED V3.5

**Fuente:** `DIAGRAMA RED V3.5.vsdx`, recibido el 12 de noviembre de 2026.  
**Alcance:** revisión de páginas, relaciones del paquete Visio y libros Excel incrustados para actualizar el inventario/topología. El archivo fuente no se modificó.

## Lectura de las tablas

El paquete contiene 22 páginas de Visio y 31 libros de Excel incrustados (el libro sin sufijo más Excel1–Excel30). Las relaciones internas del VSDX vinculan los libros con sus páginas. El número de libro puede cambiar entre versiones; para reconciliar un registro se contrastaron la página, la identidad del equipo (incluida IP/MAC cuando aparece), el puerto local y la fila.

| Página de Visio | Libros incrustados | Qué documentan y cómo se interpretan |
|---|---|---|
| RACK 1 | — | Esquema general del rack; no tiene libro Excel propio. |
| ESQUEMA CONEXIÓN INTERNET | Excel, Excel1, Excel2 | Tabla de puertos del Dell R770, salidas de la patchera y esquema de interconexión. Excel1 indica P1/iDRAC `192.168.0.247`, P2/LAN `192.168.0.246`, P3 libre y P4 no configurado. La tabla de patchera enumera salidas hacia switches, pero no da un puerto remoto único en cada switch. |
| Switch HP 5500 (D) RACK1 | Excel3, Excel4 | Tabla de servicios/servidores y estados de jaulas SFP. Los puertos que nombran switches pertenecen a switching; los servicios finales quedan en acceso. |
| Switch HP 1920 (E) RACK1 | Excel5, Excel6 | Tabla de dispositivos de rack y conexiones a otros switches; tabla separada de SFP. Excel5 puerto 16 identifica ahora `IDRAC R770`, con la misma IP `192.168.0.247`. |
| Switch HP 1920S (H) RACK1 | Excel7 | Dispositivos de rack y enlaces que terminan en switches. |
| DELL S4148U (I) | Excel8 | Estados de puertos/jaulas del S4148U y destinos descritos. La fila 22 contiene una contradicción del puerto 21 detallada abajo. |
| SWITCHES AREAS | Excel9 | Inventario de switches de área y sus IP de administración. Incluye una IP discrepante para Agrimensura 3Com. |
| SWITCH AGRIMENSURA HP | Excel10, Excel11 | Puertos de acceso y tabla SFP del switch. |
| SWITCH AGRIMENSURA 3COM | Excel12 | Puertos de acceso y dispositivos finales de Agrimensura. |
| ASUNTOS JURIDICOS | Excel13 | Puertos de acceso y dispositivos finales. |
| CARTOGRAFIA | Excel14 | Puertos de acceso, además del uplink hacia switching. |
| INMUEBLES | Excel15, Excel16, Excel17 | Switch de área, switch secundario de 8 puertos y tabla auxiliar de SFP. Los renglones 17, 18 y 22 de la tabla de 8 puertos exceden la capacidad declarada y no se asignan a ese equipo. |
| MESA DE ENTRADA (No principal) | Excel18 | Puertos de usuario y puerto 24 hacia Mesa de Entrada Principal. |
| PUERTA AGRIMENSURA | Excel19 | Puertos de acceso y uplinks documentados. La IP del ProCurve se conserva como `10.0.1.1`, confirmada por el usuario. |
| NOTIFICADORES | Excel20 | Tabla de puertos. La página muestra IP `192.168.1.40` y MAC `B8:AF:67:9A:C5:9E`, coincidentes con el switch que el inventario denomina Regularización Dominial (RD). |
| SECRETARIA | Excel21, Excel22 | Puertos de acceso y tabla auxiliar de SFP. |
| SISTEMAS | Excel23 | Puertos de acceso y equipos de usuario. Las celdas con más de una IP se mantienen como aparecen en la tabla. |
| VALUACION R0 | Excel24 | Puertos de acceso y uplink. El puerto 24 apunta ahora a Valuación R2 (`192.168.1.32`). |
| VALUACION R1 | Excel25 | Puertos de acceso. El puerto 12 ahora identifica Valuacion37 (`192.168.0.198`). |
| VALUACION R2 | Excel26 | Puertos de acceso y uplinks documentados. |
| RESERVA | Excel27, Excel28 | Tablas de reserva; no agregan destinos de usuarios que deban inventariarse como activos. |
| RESERVA2 | Excel29, Excel30 | Tablas de reserva; no agregan destinos de usuarios que deban inventariarse como activos. |

## Capas para presentar las relaciones

- **Capa 1 — física:** patchera, tramos de rack, cable y medio. La tabla de la patchera ubica salidas, pero no siempre identifica ambos puertos terminales ni el recorrido físico completo; no se crea un enlace físico completo por aproximación.
- **Capa 2 — switching:** puerto de switch a otro switch, con el puerto remoto solo cuando una tabla contraparte permite emparejarlo.
- **Capa 3 — acceso:** puerto del switch de área a PC, impresora u otro dispositivo final. No se exige que la tabla identifique un puerto de red del PC para confirmar el puerto del switch.
- Los rótulos `LIBRE`, `DESCONOCIDO`, `SIN SFP`, `SFP LIBRE` y `OCUPADO` son estados; no son nombres de equipos remotos.
- La presencia de una jaula SFP/SFP+ no confirma modelo de transceptor ni medio instalado. Esos detalles siguen como «A verificar» si la tabla no los identifica.

## Cambios incorporados al inventario

La carga se aplica al iniciar la aplicación, es idempotente y deja auditoría en `EventosModificacion` cuando cambia una relación cuya fila fuente ya estaba registrada:

1. **Regularización Dominial / RD, puerto 19:** `Sistemas06` pasa a `Regimen10`; se conserva `192.168.0.197`. Aunque la página ahora se titula «NOTIFICADORES», su IP y MAC coinciden con la ficha RD existente, por lo que se conserva la identidad del equipo y se actualiza el nombre de página de la fuente.
2. **Valuación R0, puerto 24:** pasa de la referencia anterior a Agrimensura 3Com a `Switch Valuación R2`, IP `192.168.1.32`.
3. **Valuación R1, puerto 12:** pasa de desconocido a `Valuacion37`, IP `192.168.0.198`.
4. **HP 1920 (E), puerto 16:** se normaliza la etiqueta remota a `IDRAC R770`; la IP sigue siendo `192.168.0.247`.

No se infiere puerto remoto, medio, módulo óptico ni configuración del equipo a partir de una etiqueta ambigua. Los cambios de valores se registran con su fila y página de origen.

## Discrepancias que quedan visibles

### S4148U puerto 21 y Dell R770

La aclaración previa del usuario identifica S4148U puerto 21 → Dell R770 P2/LAN, `192.168.0.246`, y HP 1920 puerto 16 → iDRAC, `192.168.0.247`. En cambio, V3.5 Excel8 fila 22 escribe `R770` y `192.168.0.247 (IDRAC)` para el puerto 21. Se conserva el enlace previo y se presenta la diferencia en la sección de pendientes de la topología hasta cotejar el puerto físico en ambos extremos.

### Agrimensura 3Com

El resumen «SWITCHES AREAS» de V3.5 escribe `192.168.0.24`; el usuario confirmó `192.168.1.24` y la tabla de puertos también usa `192.168.1.24`. El inventario conserva `192.168.1.24`.

### Nombre de área RD / Notificadores

La página «NOTIFICADORES» y el listado de switches usan nombres distintos, pero la IP y la MAC son idénticas a Regularización Dominial (RD). Se mantiene la ficha de RD y se señala pendiente confirmar el nombre del área.

### Datos aún faltantes

Régimen y Mesa de Entrada Principal siguen sin acceso administrativo por falta de credenciales. En algunos enlaces de rack falta emparejar el puerto remoto; en algunos puertos SFP falta conocer el transceptor y el medio. Estas faltas no invalidan las conexiones de usuario documentadas.
