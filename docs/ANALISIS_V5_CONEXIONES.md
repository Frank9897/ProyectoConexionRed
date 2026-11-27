# Análisis de conexiones: DIAGRAMA RED V5.0

**Fuente:** `DIAGRAMA RED V5.0.vsdx` (22 páginas y 33 libros Excel incrustados, sin cifrar), recibido el 25 de noviembre de 2026. Se comparó página por página y fila por fila contra el V4.0 y contra el catálogo que usa la aplicación. El archivo fuente no se modificó.

## Qué cambió respecto del V4

Los textos y rótulos de los dibujos (rack y esquema de internet) son idénticos al V4. Cambian solo las tablas Excel, en 30 filas más una tabla nueva:

| Página o tabla | Cambio en el V5 |
|---|---|
| Asuntos Jurídicos, puerto 8 | De `Agrimensura600` (192.168.0.151) a **Desconocido** |
| Cartografía, puerto 24 | El destino pasa a llamarse `Switch HP 1920 (E)` (192.168.1.115) |
| Notificadores, puerto 24 | De `HP 1920 (E)` a **`DELL S4148U-ON` (192.168.1.44)** |
| Dell S4148U-ON, puerto 2 | De `SUMARIOS` a **`SWITCH NOTIFICADORES` (192.168.1.40)** |
| Dell S4148U-ON, puerto 3 | De `OCUPADO` a **`SWITCH ASUNTOS JURIDICOS` (192.168.1.30)** |
| Dell S4148U-ON, puerto 5 | De `OCUPADO` a **`SWITCH REGIMEN` (192.168.1.17)** |
| Dell S4148U-ON, puerto 12 | De `OCUPADO` a **`SIN SFP`** |
| HP 1920 (E), puerto 23 | El puerto remoto pasa de `LAN` a **`MGMT p1 atrás`** |
| Puerta Agrimensura, puerto 18 | De `Agrimensura21` a **`Switch no administrable`** |
| Puerta Agrimensura, puertos 19 y 24 | Pasan a **LIBRE** (antes `Agrimensura23` y el Aruba de Mesa de Entradas) |
| **Tabla nueva** de Puerta Agrimensura | Switch no administrable de 4 puertos: 1 `Agrimensura23` (192.168.0.37), 2 `Agrimensura21` (192.168.0.124), 3 y 4 libres |
| SWITCHES AREAS | La IP 192.168.1.40 es **NOTIFICADORES** (el V4 decía «RD») |

## Qué hace la actualización

`Data/ActualizacionV5.cs` se ejecuta al iniciar, antes del importador de puertos:

1. **Renombra** las fichas `Regularización Dominial` a `Notificadores` (IP 192.168.1.40) y `Switch de área sin identificar (2)` a `Switch Régimen` (IP 192.168.1.17, área Régimen), con evento en el historial. Los catálogos del código usan ya los nombres nuevos.
2. **Agrega** la ficha `Switch no administrable` (4 puertos RJ45, administrable = No, sin IP), a verificar.
3. **Concilia las conexiones una sola vez por base** (marca `conciliacion-visio-v5-2026-11` en `MantenimientosEjecutados`): para cada puerto con tabla en el V5 elimina
   - las **relaciones documentadas** cuyo destino o IP no coinciden con la fila del V5, o que el V5 marca como libre, desconocido o sin SFP;
   - los **enlaces físicos** (incluidos los dibujados en el editor) cuyo extremo con tabla debería estar conectado a otro equipo o a ninguno.
   Cada eliminación queda en el historial con el valor anterior y lo que dice el V5. Después el importador vuelve a crear las relaciones vigentes con los datos del V5.
4. Un destino del V5 que no se puede asociar a una ficha **no** elimina el enlace: se conserva.

Los puertos sin tabla en el V5 (por ejemplo los de PC o impresoras) no se tocan. Como la conciliación es de una sola vez, los enlaces que se dibujen en el editor después no se borran en cada inicio.

## Efecto esperado en una base ya cargada

Se eliminan las 10 relaciones que el V5 contradice (Asuntos Jurídicos 8, Puerta Agrimensura 18, 19 y 24, Notificadores 24, HP 1920 puerto 23, Dell S4148U puertos 2, 3, 5 y 12) y se recrean las que el V5 define (S4148U 2, 3 y 5, Puerta Agrimensura 18 y el HP 1920 puerto 23, más la tabla del switch no administrable). Cartografía 24 se actualiza sin eliminarse porque ambos rótulos resuelven a la misma ficha.
