# Colección de Postman

Demo de Products.API, Users.API, Cart.API, Notifications.API y Orders.API: casos felices, todos los códigos de error de la sección 4 que hoy se pueden provocar, el Correlation ID y los errores del framework (404/405/415). Orders tiene una carpeta acotada: casos felices y un request por código demostrable; ORD-003 está pendiente y ORD-007 se muestra aparte con Products apagado. Products sigue usando Orders para PRD-004 (y PRD-005 manual).

| Archivo | Contenido |
|---------|-----------|
| `ECommerce13.postman_collection.json` | Colección principal (v2.1). Se corre entera y en orden. |
| `ECommerce13.manual.postman_collection.json` | CRT-005 y ORD-007 con Products apagado; PRD-005 con Orders apagado. Se corre aparte y a mano, por carpeta. |
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
3. Abrir la colección **ECommerce13 - Demo Products, Users, Cart, Notifications y Orders** → **Run** → **Run ECommerce13...**, o ejecutar los requests uno por uno en orden.

## Correr desde la terminal (Newman)

Desde la raíz del repo, con Node.js instalado:

```powershell
npx newman run docs/postman/ECommerce13.postman_collection.json -e docs/postman/ECommerce13.local.postman_environment.json
```

Se puede correr varias veces seguidas sobre la misma base: cada corrida usa un `runId` nuevo en los nombres de producto y en los emails, y la carpeta de limpieza borra lo que creó (los usuarios, las notificaciones y las órdenes canceladas quedan, porque esas APIs no tienen endpoint de baja). La orden de Orders se cancela al terminar su carpeta para liberar B; la de PRD-004 se cancela en la limpieza antes de borrar C. No se crean productos adicionales para Orders.

## Carpetas

| Carpeta | Qué muestra |
|---------|-------------|
| 0 Health | `/health`, `/health/ready` y `/health/live` de Products, Users, Orders, Cart y Notifications. |
| 1 Products | Alta, filtros por `categoria` y `nombre`, consulta, modificación. PRD-001, PRD-002, PRD-003 y PRD-004 (crea el producto C, una orden Pendiente en Orders y intenta borrarlo). |
| 2 Users | A la vista, la historia de la demo: registro y login; USR-001; USR-002 por apellido faltante; después, 3 × 401 USR-003 y recién el cuarto login devuelve 403 USR-004 (también con la contraseña correcta). En `Casos extra`: USR-002 por email inválido y login sin password, USR-003 por email inexistente (sin header, Users genera un GUID), dos fallos y un login correcto que reinician el contador (con el usuario de la demo, que no queda bloqueado), y rutas/métodos inválidos que devuelven 404/405 con `errorCode` vacío. |
| 3 Cart | A la vista, la historia de la demo: CRT-001 sin carrito, agregar (crea y acumula), CRT-003 en POST (total acumulado) sin modificar el carrito, CRT-004 con cero, CRT-002, actualizar (reemplaza) y CRT-003 en PUT (cantidad nueva), quitar y vaciar. En `Casos extra`, que corre con el carrito ya vaciado: CRT-001 en PUT/DELETE sin carrito y sobre un item ya quitado, CRT-002 en PUT y con GUID vacío, CRT-004 negativo, campos faltantes, ids inválidos y 404/405/415 propios de Cart. |
| 4 Correlation ID | El id enviado vuelve en el header y en el body de los errores; sin header se genera un GUID; Cart lo reenvía a Products. |
| 5 Errores del framework | Ruta inexistente (404), método no permitido (405) y `Content-Type` no soportado (415), con `errorCode` vacío. |
| 6 Notifications | A la vista: NTF-003 (usuario sin notificaciones), envío Email, SMS y Push (mensaje de 500 caracteres), listado y NTF-002 por tipo no reconocido. En `Casos extra`: NTF-002 por tipo faltante, mensaje faltante/en blanco/de 501 caracteres, `UsuarioId` faltante/vacío/no GUID, JSON mal formado y body ausente; GUID de correlación generado y 404/405/415 propios de Notifications. Al final, debajo de `Casos extra`, un request verifica que esas validaciones fallidas no agregaron notificaciones. |
| 7 Orders | 10 requests: crear una orden Pendiente con el producto B, obtenerla, listar y filtrar por `usuarioId`; un caso por ORD-001, ORD-002 (items vacíos), ORD-004, ORD-005 y ORD-006; cancelar la orden (Pendiente → Cancelada) para liberar B. |
| 8 Limpieza | Vacía el carrito, cancela la orden de PRD-004 y borra los productos creados (C, A y B). |

ORD-003 sigue pendiente: Users.API no tiene GET por id para validar `UsuarioId`. No se agrega un request que finja ese error; hoy cualquier GUID no vacío pasa esa validación. ORD-007 se provoca con Products apagado, sin modificar la base ni simular respuestas. Las capturas de Orders incluyen la vista general y los seis códigos demostrables: ORD-001, ORD-002, ORD-004, ORD-005, ORD-006 y ORD-007.

NTF-001 no aparece: Notifications.API todavía no valida el `UsuarioId` contra Users.API (pendiente), así que hoy un `UsuarioId` inexistente igual responde 201. NTF-004 (500) requiere una falla interna del servicio o de persistencia: no se fuerza en la demo HTTP ni se rompe la base para mostrarlo. Lo cubre `GlobalExceptionTests` inyectando un servicio que lanza una excepción; `CorrelationIdHeaderTests` también verifica que el header vuelve en ese error. Las capturas de Notifications muestran los dos códigos que hoy se pueden provocar con sus endpoints públicos (NTF-002 y NTF-003).

USR-005 está implementado: el login devuelve 403 si `Activo == false` e `IntentosFallidos < 3`. No se incluye en la demo HTTP porque Users no tiene un endpoint de bloqueo manual; lo cubren `UsersEndpointsTests` y `UserServicePersistenceTests` con `UserService.MarkManuallyBlocked`. USR-006 (500) necesita una falla interna de servicio o persistencia; lo cubre `UnhandledExceptionTests`, sin romper la base de la demo. Las capturas de Users muestran los cuatro códigos que se pueden provocar con sus endpoints públicos (USR-001 a USR-004).

En Cart, un body sin `cantidad` devuelve 400 con `errorCode` vacío (no CRT-004: ese código es para un valor menor o igual a cero). El POST con `userId` no GUID y el DELETE de item con `productId` no GUID devuelven 404 con `errorCode` vacío; no se les asigna un código de otro caso del catálogo. El PUT sobre un producto que existe en Products pero ya no está en el carrito devuelve CRT-001.

En Notifications, el `UsuarioId` faltante o GUID vacío devuelve 400 NTF-002 (no NTF-001). Un `userId` no GUID en el GET no coincide con la ruta y devuelve 404 con `errorCode` vacío (no NTF-003). Los envíos exitosos verifican el id, destinatario, mensaje, tipo, estado `Enviada` y fecha UTC; el listado recupera los tres ids creados y sigue teniendo solo esas tres notificaciones después de las validaciones fallidas.

Users, Cart y Notifications dejan a la vista los requests de la demo y guardan los casos adicionales en la subcarpeta `Casos extra`, que corre después de esos requests; para mostrar la demo alcanza con no abrirla. Los requests del carrito y de notificaciones usan el `userId` que devuelve el registro de la carpeta 2, así que las carpetas se corren en orden.

## Tests

Cada request verifica el status y el `errorCode` esperados. Los errores de las carpetas Users, Cart, Notifications y Orders, y los CRT-005 y ORD-007 manuales, también verifican el `errorMessage` (en ORD-005 el sufijo del nombre del producto cambia por corrida). A nivel colección se verifica además que:

- la respuesta trae el header `X-Correlation-Id` y es igual al enviado;
- toda respuesta 4xx/5xx tiene `errorCode`, un `errorMessage` no vacío y un `correlationId` igual al header.

Si el request no trae su propio `X-Correlation-Id`, la colección manda el valor de `correlationId` del environment (`demo-defensa`). Para buscarlo en los logs:

```powershell
Select-String -Path src/Products.API/logs/*.json -Pattern "demo-defensa"
```

El request **4 Correlation ID → Cart lo propaga a Products** manda `demo-cart-<runId>`. Ese valor aparece en `src/Cart.API/logs/carts-AAAAMMDD.json` y en `src/Products.API/logs/products-AAAAMMDD.json`.

## CRT-005, PRD-005 y ORD-007 (manual)

Las carpetas de `ECommerce13.manual.postman_collection.json` se corren por separado: CRT-005 y ORD-007 necesitan Products apagado (pueden aprovechar el mismo apagado); PRD-005 necesita Orders apagado y Products levantado. No correr toda la colección manual de una vez.

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

### ORD-007 (Products apagado)

1. Dejar Orders.API levantado y apagar Products.API (Ctrl+C en su terminal).
2. Correr **ECommerce13 - Manual** → **Manual - Orders con Products apagado** → **Crear orden con Products apagado**.
3. Respuesta esperada: `500` con `errorCode` `ORD-007` y `errorMessage` "Error interno al procesar la orden.". Con `ErrorHandling:IncludeExceptionDetails=false`, `detail` repite ese texto; en Development (flag `true`) muestra `exception.Message`. Falla la consulta a Products antes de persistir; no queda una orden que limpiar. El error completo queda en `src/Orders.API/logs/orders-api-AAAAMMDD.json`.

```powershell
npx newman run docs/postman/ECommerce13.manual.postman_collection.json -e docs/postman/ECommerce13.local.postman_environment.json --folder "Manual - Orders con Products apagado"
```

Después volver a levantar Products.API.
