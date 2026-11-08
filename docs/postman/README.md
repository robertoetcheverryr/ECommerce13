# Colección de Postman

Demo de Products.API, Users.API, Cart.API y Notifications.API: casos felices, todos los códigos de error de la sección 4 que hoy se pueden provocar, el Correlation ID y los errores del framework (404/405/415). Orders.API todavía no tiene carpeta propia; Products lo usa para mostrar PRD-004 (y PRD-005 en la colección manual).

| Archivo | Contenido |
|---------|-----------|
| `ECommerce13.postman_collection.json` | Colección principal (v2.1). Se corre entera y en orden. |
| `ECommerce13.manual.postman_collection.json` | CRT-005 con Products apagado y PRD-005 con Orders apagado. Se corre aparte y a mano, por carpeta. |
| `ECommerce13.local.postman_environment.json` | Environment `ECommerce13 local` con las URLs y el `correlationId` por defecto. |

## Antes de correrla

Levantar Products, Users, Orders, Cart y Notifications. Products antes que Cart y Orders (ver "Ejecutar un microservicio" en el [README principal](../../README.md)):

```powershell
dotnet run --project src/Products.API
dotnet run --project src/Users.API
dotnet run --project src/Orders.API
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

Se puede correr varias veces seguidas sobre la misma base: cada corrida usa un `runId` nuevo en los nombres de producto y en los emails, y la carpeta de limpieza borra lo que creó (los usuarios y las notificaciones quedan, porque Users.API y Notifications.API no tienen endpoint de baja). La orden que se crea para PRD-004 se cancela en la limpieza para poder borrar el producto C.

## Carpetas

| Carpeta | Qué muestra |
|---------|-------------|
| 0 Health | `/health`, `/health/ready` y `/health/live` de Products, Users, Orders, Cart y Notifications. |
| 1 Products | Alta, filtros por `categoria` y `nombre`, consulta, modificación. PRD-001, PRD-002, PRD-003 y PRD-004 (crea el producto C, una orden Pendiente en Orders y intenta borrarlo). |
| 2 Users | Registro y login; USR-001; USR-002 por apellido faltante, email inválido y login sin password; USR-003 por email inexistente. Dos fallos y un login correcto reinician el contador; después, 3 × 401 USR-003 y recién el cuarto login devuelve 403 USR-004 (también con la contraseña correcta). Sin header, Users genera un GUID; rutas/métodos inválidos devuelven 404/405 con `errorCode` vacío. |
| 3 Cart | 29 requests: agregar (crea y acumula), actualizar (reemplaza), quitar y vaciar. CRT-001 también en PUT/DELETE sin carrito y al operar sobre un item ya quitado; CRT-002 en POST/PUT; CRT-003 en POST (total acumulado) y PUT (cantidad nueva), sin modificar el carrito al fallar; CRT-004 con cero y negativo. Campos faltantes, ids inválidos, GUID de correlación generado y 404/405/415 propios de Cart. |
| 4 Correlation ID | El id enviado vuelve en el header y en el body de los errores; sin header se genera un GUID; Cart lo reenvía a Products. |
| 5 Errores del framework | Ruta inexistente (404), método no permitido (405) y `Content-Type` no soportado (415), con `errorCode` vacío. |
| 6 Notifications | Envío Email y SMS, listado del usuario. NTF-003 (usuario sin notificaciones), NTF-002 (tipo no reconocido y mensaje faltante). |
| 7 Limpieza | Vacía el carrito, cancela la orden de PRD-004 y borra los productos creados (C, A y B). |

NTF-001 no aparece: Notifications.API todavía no valida el `UsuarioId` contra Users.API (pendiente), así que hoy un `UsuarioId` inexistente igual responde 201. NTF-004 tampoco aparece porque no se puede provocar desde afuera (lo cubre `GlobalExceptionTests`).

USR-005 está implementado: el login devuelve 403 si `Activo == false` e `IntentosFallidos < 3`. No se incluye en la demo HTTP porque Users no tiene un endpoint de bloqueo manual; lo cubren `UsersEndpointsTests` y `UserServicePersistenceTests` con `UserService.MarkManuallyBlocked`. USR-006 (500) necesita una falla interna de servicio o persistencia; lo cubre `UnhandledExceptionTests`, sin romper la base de la demo. Las capturas de Users muestran los cuatro códigos que se pueden provocar con sus endpoints públicos (USR-001 a USR-004).

En Cart, un body sin `cantidad` devuelve 400 con `errorCode` vacío (no CRT-004: ese código es para un valor menor o igual a cero). El POST con `userId` no GUID y el DELETE de item con `productId` no GUID devuelven 404 con `errorCode` vacío; no se les asigna un código de otro caso del catálogo. El PUT sobre un producto que existe en Products pero ya no está en el carrito devuelve CRT-001.

Los requests del carrito y de notificaciones usan el `userId` que devuelve el registro de la carpeta 2, así que las carpetas se corren en orden.

## Tests

Cada request verifica el status y el `errorCode` esperados. Los errores de las carpetas Users y Cart, y el CRT-005 manual, también verifican el `errorMessage` exacto. A nivel colección se verifica además que:

- la respuesta trae el header `X-Correlation-Id` y es igual al enviado;
- toda respuesta 4xx/5xx tiene `errorCode`, un `errorMessage` no vacío y un `correlationId` igual al header.

Si el request no trae su propio `X-Correlation-Id`, la colección manda el valor de `correlationId` del environment (`demo-defensa`). Para buscarlo en los logs:

```powershell
Select-String -Path src/Products.API/logs/*.json -Pattern "demo-defensa"
```

El request **4 Correlation ID → Cart lo propaga a Products** manda `demo-cart-<runId>`. Ese valor aparece en `src/Cart.API/logs/carts-AAAAMMDD.json` y en `src/Products.API/logs/products-AAAAMMDD.json`.

## CRT-005 y PRD-005 (manual)

Las dos carpetas de `ECommerce13.manual.postman_collection.json` piden apagar un servicio distinto: no se corren juntas.

### CRT-005 (Products apagado)

1. Dejar Cart.API levantado y apagar Products.API (Ctrl+C en su terminal).
2. Correr **ECommerce13 - Manual** → **Manual - Products apagado** → **Agregar al carrito con Products apagado**.
3. Respuesta esperada: `500` con `errorCode` `CRT-005` y `errorMessage` "Error interno al procesar el carrito.". El error completo queda en el log de Cart.

```powershell
npx newman run docs/postman/ECommerce13.manual.postman_collection.json -e docs/postman/ECommerce13.local.postman_environment.json --folder "Manual - Products apagado"
```

Después volver a levantar Products.API.

### PRD-005 (Orders apagado)

1. Dejar Products.API levantado y apagar Orders.API (Ctrl+C en su terminal).
2. Correr **ECommerce13 - Manual** → **Manual - Orders apagado**: primero crea un producto (el alta no consulta Orders) y después intenta borrarlo.
3. Respuesta esperada: `500` con `errorCode` `PRD-005` y `errorMessage` "Error interno al procesar el producto.". El error completo queda en el log de Products. El producto no se borra.

```powershell
npx newman run docs/postman/ECommerce13.manual.postman_collection.json -e docs/postman/ECommerce13.local.postman_environment.json --folder "Manual - Orders apagado"
```

Después volver a levantar Orders.API.
