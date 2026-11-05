# Colección de Postman

Demo de Products.API, Users.API, Cart.API y Notifications.API: casos felices, todos los códigos de error de la sección 4 que hoy se pueden provocar, el Correlation ID y los errores del framework (404/405/415).

| Archivo | Contenido |
|---------|-----------|
| `ECommerce13.postman_collection.json` | Colección principal (v2.1). Se corre entera y en orden. |
| `ECommerce13.manual.postman_collection.json` | CRT-005 con Products apagado. Se corre aparte y a mano. |
| `ECommerce13.local.postman_environment.json` | Environment `ECommerce13 local` con las URLs y el `correlationId` por defecto. |

## Antes de correrla

Levantar los cuatro servicios, Products antes que Cart (ver "Ejecutar un microservicio" en el [README principal](../../README.md)):

```powershell
dotnet run --project src/Products.API
dotnet run --project src/Users.API
dotnet run --project src/Cart.API
dotnet run --project src/Notifications.API
```

## Importar en Postman

1. **Import** → arrastrar los tres archivos `.json` de esta carpeta.
2. Arriba a la derecha, elegir el environment **ECommerce13 local**.
3. Abrir la colección **ECommerce13 - Demo Products, Users, Cart y Notifications** → **Run** → **Run ECommerce13...**, o ejecutar los requests uno por uno en orden.

## Correr desde la terminal (Newman)

Desde la raíz del repo, con Node.js instalado:

```powershell
npx newman run docs/postman/ECommerce13.postman_collection.json -e docs/postman/ECommerce13.local.postman_environment.json
```

Se puede correr varias veces seguidas sobre la misma base: cada corrida usa un `runId` nuevo en los nombres de producto y en los emails, y la carpeta de limpieza borra lo que creó (los usuarios y las notificaciones quedan, porque Users.API y Notifications.API no tienen endpoint de baja).

## Carpetas

| Carpeta | Qué muestra |
|---------|-------------|
| 0 Health | `/health`, `/health/ready` y `/health/live` de los cuatro servicios. |
| 1 Products | Alta, filtros por `categoria` y `nombre`, consulta, modificación. PRD-001, PRD-002, PRD-003. |
| 2 Users | Registro y login. USR-001, USR-002 y el bloqueo: 3 × 401 USR-003 y después 403 USR-004 (también con la contraseña correcta). |
| 3 Cart | Agregar, actualizar, quitar y vaciar. CRT-001, CRT-002, CRT-003 (en `POST` la cantidad se suma a la que ya había), CRT-004. |
| 4 Correlation ID | El id enviado vuelve en el header y en el body de los errores; sin header se genera un GUID; Cart lo reenvía a Products. |
| 5 Errores del framework | Ruta inexistente (404), método no permitido (405) y `Content-Type` no soportado (415), con `errorCode` vacío. |
| 6 Notifications | Envío Email y SMS, listado del usuario. NTF-003 (usuario sin notificaciones), NTF-002 (tipo no reconocido y mensaje faltante). |
| 7 Limpieza | Vacía el carrito y borra los productos creados. |

PRD-004 no aparece: no hay órdenes todavía, así que borrar un producto siempre responde 204.

NTF-001 no aparece: Notifications.API todavía no valida el `UsuarioId` contra Users.API (pendiente), así que hoy un `UsuarioId` inexistente igual responde 201. NTF-004 tampoco aparece porque no se puede provocar desde afuera (lo cubre `GlobalExceptionTests`).

Los requests del carrito y de notificaciones usan el `userId` que devuelve el registro de la carpeta 2, así que las carpetas se corren en orden.

## Tests

Cada request verifica el status y el `errorCode` esperados. A nivel colección se verifica además que:

- la respuesta trae el header `X-Correlation-Id` y es igual al enviado;
- toda respuesta 4xx/5xx tiene `errorCode`, un `errorMessage` no vacío y un `correlationId` igual al header.

Si el request no trae su propio `X-Correlation-Id`, la colección manda el valor de `correlationId` del environment (`demo-defensa`). Para buscarlo en los logs:

```powershell
Select-String -Path src/Products.API/logs/*.json -Pattern "demo-defensa"
```

El request **4 Correlation ID → Cart lo propaga a Products** manda `demo-cart-<runId>`. Ese valor aparece en `src/Cart.API/logs/carts-AAAAMMDD.json` y en `src/Products.API/logs/products-AAAAMMDD.json`.

## CRT-005 (manual)

1. Dejar Cart.API levantado y apagar Products.API (Ctrl+C en su terminal).
2. Correr **ECommerce13 - Manual - Products apagado** → **Agregar al carrito con Products apagado**.
3. Respuesta esperada: `500` con `errorCode` `CRT-005` y `errorMessage` "Error interno al procesar el carrito.". El error completo queda en el log de Cart.

Con Newman:

```powershell
npx newman run docs/postman/ECommerce13.manual.postman_collection.json -e docs/postman/ECommerce13.local.postman_environment.json
```

Después volver a levantar Products.API.
