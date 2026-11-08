# E-Commerce - Arquitectura de Microservicios

![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![Build](https://github.com/robertoetcheverryr/ECommerce13/actions/workflows/tests.yml/badge.svg) 
![License](https://img.shields.io/badge/license-MIT-blue)
![Status](https://img.shields.io/badge/status-in%20progress-yellow)

**Trabajo Práctico**  
Materia: Arquitectura y Diseño de Software  
Tecnología: C# / .NET 10  
Modalidad: Grupal

---

## Descripción

Sistema de E-Commerce basado en arquitectura de microservicios.  
Cada funcionalidad se expone como una REST API independiente.

## Microservicios

| Servicio              | Puerto | Descripción                              |
|-----------------------|--------|------------------------------------------|
| **Products.API**      | 5001   | Gestión de productos                     |
| **Users.API**         | 5002   | Registro y autenticación de usuarios     |
| **Orders.API**        | 5003   | Creación y gestión de órdenes            |
| **Cart.API**          | 5004   | Carrito de compras                       |
| **Notifications.API** | 5005   | Envío y consulta de notificaciones       |

## Arquitectura

Diagrama de los cinco servicios (Products, Users, Orders, Cart y Notifications). Cada uno tiene su propia base SQLite y ninguno lee la base de otro.

```mermaid
flowchart LR
    Cliente([Cliente<br/>Swagger UI / Postman])

    subgraph Servicios
        P[Products.API<br/>:5001]
        U[Users.API<br/>:5002]
        O[Orders.API<br/>:5003]
        C[Cart.API<br/>:5004]
        N[Notifications.API<br/>:5005]
    end

    PDB[(products.db)]
    UDB[(users.db)]
    ODB[(orders.db)]
    CDB[(carts.db)]
    NDB[(notifications.db)]

    Cliente -->|HTTP + X-Correlation-Id| P
    Cliente -->|HTTP + X-Correlation-Id| U
    Cliente -->|HTTP + X-Correlation-Id| O
    Cliente -->|HTTP + X-Correlation-Id| C
    Cliente -->|HTTP + X-Correlation-Id| N

    P --> PDB
    U --> UDB
    O --> ODB
    C --> CDB
    N --> NDB

    C -->|"GET /api/products/{id}<br/>reenvía X-Correlation-Id"| P
    O -->|"GET /api/products/{id}<br/>reenvía X-Correlation-Id"| P
    P -->|"GET /api/orders<br/>reenvía X-Correlation-Id"| O
    O -.->|"pendiente: validar UsuarioId"| U
    N -.->|"pendiente: validar UsuarioId"| U
```

Versión ASCII:

```text
                                    ┌──────────────────────────────┐
                                    │            Cliente           │
                                    │     Swagger UI / Postman     │
                                    └──────────────┬───────────────┘
                                                   │ HTTP + X-Correlation-Id
     ┌──────────────┬───────────────┬──────────────┴───┬───────────────┬────────────────┐
     ▼              ▼               ▼                  ▼               ▼
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌───────────────────┐
│Products  │  │  Users   │  │  Orders  │  │   Cart   │  │ Notifications.API │
│  :5001   │  │  :5002   │  │  :5003   │  │  :5004   │  │      :5005        │
└────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────────┬──────────┘
     │  ▲          │      ▲      │  ▲          │                 │
     ▼  │          ▼      ┆      ▼  │          ▼                 ▼
(products.db) (users.db)  ┆ (orders.db)   (carts.db)   (notifications.db)
     ▲  │                 ┆      │  │          │
     │  └──── GET /api/orders ───┘  │          │
     │       PRD-004, reenvía X-Correlation-Id
     └──── GET /api/products/{id} ──┴──────────┘
          Cart y Orders; reenvían X-Correlation-Id

     Orders         ┄┄ pendiente: validar UsuarioId (ORD-003) ┄┄▶ Users
     Notifications  ┄┄ pendiente: validar UsuarioId (NTF-001) ┄┄▶ Users
```

### Llamadas entre servicios

| Origen | Destino | Endpoint | Cuándo | Si falla |
|----------|----------|----------------------------|------------------------------------------------|-------------------------------------------------------------------------------------------|
| Cart.API | Products.API | `GET /api/products/{id}` | En cada `POST` y `PUT` de items, para validar que el producto existe y que alcanza el stock | 404 de Products → **CRT-002**. Stock insuficiente → **CRT-003**. Products caído o cualquier otro error → **CRT-005** |
| Orders.API | Products.API | `GET /api/products/{id}` | En cada `POST` de una orden, para validar que cada producto existe y que alcanza el stock. El Total y el PrecioUnitario salen del precio de Products, no de lo que mande el cliente | 404 de Products → **ORD-004**. Stock insuficiente → **ORD-005** |
| Products.API | Orders.API | `GET /api/orders` | En cada `DELETE` de un producto, para ver si hay órdenes Pendiente o Confirmada | Si hay órdenes activas → **PRD-004**. Si Orders no responde → **PRD-005** (mejor fallar que asumir que no hay órdenes) |
| Orders.API | Users.API | Pendiente | En `POST /api/orders`, para validar que el `UsuarioId` existe | La spec (4.3) pide **ORD-003** cuando el `UsuarioId` no existe en Users API. Todavía no está implementado: hoy se crea la orden para cualquier Guid no vacío (un `Guid.Empty` es ORD-002) |
| Notifications.API | Users.API | Pendiente | En `POST /api/notifications/send`, para validar que el `UsuarioId` existe | La spec (4.5) pide **NTF-001** cuando el `UsuarioId` no existe en Users API. Todavía no está implementado: hoy se registra la notificación para cualquier Guid no vacío |

Cart y Orders copian el `X-Correlation-Id` del request entrante en la llamada a Products, y Products lo copia en la llamada a Orders, así el mismo id aparece en los logs de los servicios que participan. La URL de Products sale de `Products:BaseUrl` en Cart (`appsettings.json`, por defecto `http://localhost:5001`) y de `Services:ProductsApi:BaseUrl` en Orders. La URL de Orders sale de `Orders:BaseUrl` en Products (por defecto `http://localhost:5003`).

Error de consistencia en la spec: NTF-001 (y ORD-003) piden validar el usuario contra Users API, pero Users API (4.2) solo expone `register` y `login`, no hay endpoint para consultar un usuario por id.

### Servicios

**Products.API (5001)** - Catálogo de productos sobre `products.db`.
Alta, baja, modificación y consulta, con filtros por `categoria` y por `nombre` (búsqueda parcial).
No permite dos productos con el mismo nombre en la misma categoría (PRD-003).
Cart y Orders lo consultan para validar productos y stock. Al borrar un producto, Products consulta Orders para no eliminar uno con órdenes activas (PRD-004).

**Users.API (5002)** - Registro y login sobre `users.db`.
El password se guarda con hash y nunca vuelve en las respuestas.
Después de 3 logins fallidos el usuario queda bloqueado (USR-004).
No llama a otros servicios.

**Orders.API (5003)** - Creación y gestión de órdenes sobre `orders.db`.
`GET` lista (filtro opcional `usuarioId`), `GET` por id, `POST` y `PUT` del estado.
Antes de crear consulta a Products.API por cada producto y el stock; el Total y el PrecioUnitario salen del precio de Products.
Si el producto no existe responde ORD-004; si no hay stock, ORD-005.
Todavía no llama a Users.API para validar el `UsuarioId` (ORD-003 pendiente).

**Cart.API (5004)** - Carrito por usuario sobre `carts.db`.
El `POST` de un item crea el carrito si no existe y suma la cantidad a la que ya había; el `PUT` la reemplaza.
Antes de guardar consulta a Products.API por el producto y el stock.
Si Products no responde, devuelve CRT-005 y deja el carrito como estaba.

**Notifications.API (5005)** - Registro y envío simulado de notificaciones sobre `notifications.db`.
`POST /api/notifications/send` guarda la notificación con `Estado` "Enviada" y `FechaEnvio` en UTC. `Tipo` tiene que ser Email, Push o SMS.
`GET /api/notifications/{userId}` lista las notificaciones del usuario; si no tiene ninguna responde NTF-003.
Todavía no llama a Users.API para validar el `UsuarioId` (NTF-001 pendiente).

## Códigos de error

Todas las respuestas 4xx/5xx usan Problem Details más `errorCode` y `errorMessage`.  
Las 5 APIs (Products, Users, Orders, Cart y Notifications) implementan el catálogo, con las dos únicas excepciones documentadas: ORD-003 y NTF-001, que necesitan un endpoint de Users para validar el `UsuarioId` que todavía no existe (ver los puntos pendientes de cada sección).

### Products.API

| errorCode | HTTP | errorMessage | Cuándo |
|-----------|------|--------------|--------|
| **PRD-001** | 404 | Producto no encontrado. | GET/PUT/DELETE con ID inexistente |
| **PRD-002** | 400 | Los datos del producto son inválidos. | POST/PUT con campos faltantes o formato incorrecto |
| **PRD-003** | 409 | Ya existe un producto con ese nombre en la categoría '{0}'. | POST duplicado nombre+categoría |
| **PRD-004** | 409 | El producto tiene órdenes activas y no puede eliminarse. | DELETE con órdenes Pendiente o Confirmada |
| **PRD-005** | 500 | Error interno al procesar el producto. | Error inesperado |

### Users.API

| errorCode | HTTP | errorMessage | Cuándo |
|-----------|------|--------------|--------|
| **USR-001** | 409 | El email ya está registrado. | POST /register con email existente |
| **USR-002** | 400 | Los datos del usuario son inválidos. | POST /register o /login inválido |
| **USR-003** | 401 | Credenciales incorrectas. | POST /login email inexistente o password no coincide |
| **USR-004** | 403 | Usuario bloqueado por demasiados intentos fallidos. | POST /login con Activo false e IntentosFallidos >= 3. El tercer fallo sigue siendo USR-003 |
| **USR-005** | 403 | Usuario bloqueado por detección de fraude. | POST /login con Activo false e IntentosFallidos < 3 |
| **USR-006** | 500 | Error interno al procesar el usuario. | Error inesperado |

### Orders.API

| errorCode | HTTP | errorMessage | Cuándo |
|-----------|------|--------------|--------|
| **ORD-001** | 404 | Orden no encontrada. | GET/PUT con ID inexistente |
| **ORD-002** | 400 | Los datos de la orden son inválidos. | POST con campos faltantes (JsonRequired), `usuarioId` Guid.Empty, `cantidad` <= 0 o items vacíos |
| **ORD-003** | 404 | Usuario no encontrado al crear la orden. | UsuarioId no existe en Users. Pendiente: ver sección Orders |
| **ORD-004** | 404 | Producto no encontrado al crear la orden. | ProductoId no existe en Products |
| **ORD-005** | 422 | Stock insuficiente para uno o más productos. | Cantidad > stock |
| **ORD-006** | 409 | El estado de la orden no puede ser modificado. | Transición de estado inválida |
| **ORD-007** | 500 | Error interno al procesar la orden. | Error inesperado |

### Cart.API

| errorCode | HTTP | errorMessage | Cuándo |
|-----------|------|--------------|--------|
| **CRT-001** | 404 | Carrito no encontrado. | userId sin carrito activo |
| **CRT-002** | 404 | Producto no encontrado. | ProductoId no existe en Products |
| **CRT-003** | 422 | Stock insuficiente para agregar al carrito. | Cantidad > stock |
| **CRT-004** | 400 | Cantidad inválida. | Cantidad ≤ 0 |
| **CRT-005** | 500 | Error interno al procesar el carrito. | Error inesperado |

### Notifications.API

| errorCode | HTTP | errorMessage | Cuándo |
|-----------|------|--------------|--------|
| **NTF-001** | 404 | Usuario no encontrado. | UsuarioId no existe en Users. Pendiente: ver sección Notifications |
| **NTF-002** | 400 | Los datos de la notificación son inválidos. | Campos faltantes o tipo no reconocido |
| **NTF-003** | 404 | No se encontraron notificaciones para el usuario. | userId sin notificaciones |
| **NTF-004** | 500 | Error interno al procesar la notificación. | Error inesperado |

## Estructura del proyecto

```
ECommerce13/
├── src/
│   ├── Products.API/
│   ├── Users.API/
│   ├── Orders.API/
│   ├── Cart.API/
│   └── Notifications.API/
├── tests/
│   ├── Products.API.Tests/
│   ├── Users.API.Tests/
│   ├── Cart.API.Tests/
│   └── Notifications.API.Tests/
├── docs/
└── README.md
```

Cada microservicio sigue la siguiente estructura interna:

```
Xxx.API/
├── Controllers/
├── Models/
├── DTOs/
├── Services/
├── Exceptions/
├── ExceptionHandlers/
├── logs/
└── Program.cs
```

## Cómo levantar el entorno de desarrollo

### 1. SDK de .NET 10

Verificar que el SDK esté instalado y visible:

```powershell
dotnet --list-sdks
dotnet --info
```

Debe aparecer la versión `10.0.x`. Si no aparece, instalar desde:  
https://dotnet.microsoft.com/download/dotnet/10.0

### 2. Feed de NuGet

Si `dotnet nuget list source` no muestra ningún origen, agregarlo:

```powershell
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

Verificar:

```powershell
dotnet nuget list source
```

### 3. Restaurar paquetes

Desde la raíz de la solución:

```powershell
dotnet restore
```

### 4. Compilar

```powershell
dotnet build
dotnet test
```

### 5. Ejecutar un microservicio

```powershell
dotnet run --project src/Products.API
dotnet run --project src/Users.API
dotnet run --project src/Orders.API
dotnet run --project src/Cart.API
dotnet run --project src/Notifications.API
```

Puertos configurados:

- Products.API → http://localhost:5001
- Users.API → http://localhost:5002
- Orders.API → http://localhost:5003
- Cart.API → http://localhost:5004
- Notifications.API → http://localhost:5005

Orden de arranque: levantar **Products.API antes que Cart.API y Orders.API**. Cart arranca igual sin Products, pero los `POST` y `PUT` de items necesitan consultarlo (si no, responden CRT-005). Orders arranca igual sin Products, pero el `POST` de una orden lo consulta para validar producto y stock. Products arranca igual sin Orders, pero el `DELETE` de un producto lo consulta (si no, responde PRD-005). Users.API y Notifications.API son independientes (Orders y Notifications todavía no consultan a Users).

Logs: Serilog escribe en consola y en un archivo JSON por día, relativo a la carpeta desde donde se ejecuta el servicio. Con los comandos de arriba (desde la raíz del repo):

- Products.API → `src/Products.API/logs/products-AAAAMMDD.json`
- Users.API → `src/Users.API/logs/users-AAAAMMDD.json`
- Orders.API → `src/Orders.API/logs/orders-api-AAAAMMDD.json`
- Cart.API → `src/Cart.API/logs/carts-AAAAMMDD.json`
- Notifications.API → `src/Notifications.API/logs/notifications-api-AAAAMMDD.json`

Para la demo hay una colección de Postman con los casos felices y los errores de Products, Users, Cart y Notifications. Orders todavía no tiene carpeta propia; Products lo usa para mostrar PRD-004: ver [`docs/postman/`](docs/postman/README.md).

### 6. Swagger UI

Con el microservicio corriendo, abrir en el navegador:

- Products: http://localhost:5001/swagger
- Users: http://localhost:5002/swagger
- Orders: http://localhost:5003/swagger
- Cart: http://localhost:5004/swagger
- Notifications: http://localhost:5005/swagger

Desde ahí se pueden probar los endpoints interactivos.

### 7. Tests

Trato de agregar la mayor cantidad posible de tests.
De automatizarlos para evitar olvidos. 
Y de seguir dentro de lo posible una metodologia TDD.

```powershell
dotnet test
```

## Estado actual

- Estructura de la solución + 5 microservicios + tests + CI (GitHub Actions)
- **Products.API**
    - Endpoints 4.1: GET lista (`?categoria=`, `?nombre=` parcial), GET by id, POST, PUT, DELETE
    - Persistencia en SQLite con Dapper (`Microsoft.Data.Sqlite`). Al iniciar, `DatabaseInitializer` crea la tabla `products` si no existe, con las columnas del producto: Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion. Archivo `products.db` (`ConnectionStrings:DefaultConnection`). El patrón de base lo tomamos del ejemplo de la cátedra: [toDoList-2026, branch `feature/completo`](https://github.com/cai-uba/toDoList-2026/tree/feature/completo) (SQLite + Dapper, `CREATE TABLE IF NOT EXISTS` al startup).
    - Validaciones con Data Annotations (PRD-002)
    - `ErrorCodes` + excepciones de dominio (`NotFound`, `Validation`, `BusinessRule` con `Detail`, `Global`)
    - `IExceptionHandler`s registrados en orden de especificidad
    - 404 de ruta inexistente, 405 y 415: `UseStatusCodePages` + `StatusCodeErrorWriter` devuelven el mismo `ErrorResponse` con `correlationId`. La sección 4 no tiene código para estos casos, así que `errorCode` va vacío. `SuppressMapClientErrors` evita que `[ApiController]` arme su propio ProblemDetails en el 415
    - PRD-003 solo en POST. Ya que no esta en la spec, PUT no revalida unicidad nombre+categoría
    - PRD-004 vía `IActiveOrdersChecker` implementada por `ActiveOrdersClient`: GET Orders `/api/orders` (puerto 5003, `Orders:BaseUrl`) y busca el `productoId` en los items de órdenes Pendiente o Confirmada. Si Orders no responde, el DELETE es PRD-005: mejor fallar que asumir que no hay órdenes. Los tests inyectan un fake: `NeverActiveOrdersChecker` por defecto (`ProductsApiFactory`) y `AlwaysActiveOrdersChecker` en los casos que esperan el 409; el camino HTTP real lo cubre `ActiveOrdersClientTests` contra un Orders falso servido en Kestrel
    - Health checks 5.4: `/health` (ambas sondas), `/health/ready` (tabla `products`), `/health/live` (proceso). JSON `{ status }`
    - Swagger: XML comments, `[ProducesResponseType]` con los status del contrato (incluye 500/PRD-005)
    - Ejemplos de request/response por status: `ProductsSwaggerExamplesFilter` (`IOperationFilter`)
    - Tests E2E (xUnit + WebApplicationFactory + FluentAssertions)
    - Serilog: consola + JSON, request log, Warning/Error con errorCode, Endpoint y CorrelationId en cada evento del request
    - Correlation ID inbound y outbound: header `X-Correlation-Id`, campo `correlationId` en errores, propiedad en logs. `CorrelationIdHandler` lo copia en la llamada a Orders para PRD-004
- **Users.API**
    - Endpoints 4.2: POST /api/users/register, POST /api/users/login. No hay GET ni lock/unlock de admin
    - Persistencia en SQLite con Dapper (`Microsoft.Data.Sqlite`). Al iniciar, `DatabaseInitializer` crea la tabla `users` si no existe, con las columnas del usuario: Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro, Activo, IntentosFallidos. Archivo `users.db` (`ConnectionStrings:DefaultConnection`). El email se guarda en minúsculas. La unicidad sigue en el servicio, no hay índice unique, así un archivo que nos dejen carga igual.
    - Validaciones con Data Annotations (USR-002). Email y password delegan en `Services/Email` y `Services/Password`
    - `ErrorCodes` + excepciones de dominio (`NotFound`, `Validation`, `BusinessRule` con `Detail`, `Global`)
    - `IExceptionHandler`s registrados en orden de especificidad
    - 404 de ruta inexistente, 405 y 415: `UseStatusCodePages` + `StatusCodeErrorWriter` devuelven el mismo `ErrorResponse` con `correlationId`. La sección 4 no tiene código para estos casos, así que `errorCode` va vacío. `SuppressMapClientErrors` evita que `[ApiController]` arme su propio ProblemDetails en el 415
    - USR-003 no distingue email inexistente de password incorrecta para evitar dar informacion a un atacante
    - USR-004: al tercer fallo consecutivo `Activo` pasa a false y ese request sigue siendo USR-003. El login siguiente es 403. Un login correcto antes resetea `IntentosFallidos`. El contador se escribe antes de tirar la excepción, si no un restart se olvida el bloqueo
    - USR-005: `Activo == false` con `IntentosFallidos < 3`. No hay endpoint para marcarlo; los tests usan `UserService.MarkManuallyBlocked`
    - PasswordHash nunca sale en register ni login
    - Health checks 5.4: `/health` (ambos chequeos), `/health/ready` (tabla `users`), `/health/live` (proceso). JSON `{ status }`
    - Swagger: XML comments, `[ProducesResponseType]`, `UsersSwaggerExamplesFilter`. El 403 del login tiene dos ejemplos nombrados, `usr-004` y `usr-005`, sin `example` suelto: OpenAPI no permite `example` y `examples` juntos en el mismo media type
    - Tests E2E (xUnit + WebApplicationFactory + FluentAssertions). `UsersApiFactory` apunta a un sqlite temporal, no a `users.db` en la carpeta de output
    - Serilog: consola + JSON, request log, Warning/Error con errorCode, Endpoint y CorrelationId en cada evento del request
    - Correlation ID inbound: header `X-Correlation-Id`, campo `correlationId` en errores, propiedad en logs. Users no llama a nadie, no hay outbound
- **Orders.API**
    - Endpoints 4.3: GET /api/orders (`?usuarioId=` opcional), GET /api/orders/{id}, POST, PUT /api/orders/{id}/status
    - Persistencia en SQLite con Dapper (`Microsoft.Data.Sqlite`). Al iniciar, `DatabaseInitializer` crea `orders` y `order_items` si no existen. Archivo `orders.db` (`ConnectionStrings:DefaultConnection`). Guids, totales y fechas se guardan como TEXT y Dapper no materializa Guid desde una columna texto: las queries leen en clases Row con campos `string` y mapean a mano con `Guid.Parse` / `decimal.Parse`, igual que Products
    - `ErrorCodes` (ORD-001 a ORD-007) + `OrderException` que ya trae el status code. Un único `OrdersExceptionHandler` los escribe (el pattern de un handler por tipo de excepción de Products/Users/Cart no estaba en el codigo original de la rama y no lo cambiamos)
    - ORD-006 maquina de estados: `HashSet` con las 5 transiciones validas del doc. Mismo estado o estado inexistente es 409 parejo. PUT con estado vacio es 400 ORD-002
    - ORD-003 pendiente: falta definir el endpoint interno de Users para validar UsuarioId, hay un comentario en `CreateAsync` marcando donde va
    - 404 de ruta inexistente, 405 y 415: `UseStatusCodePages` + `StatusCodeErrorWriter` devuelven el mismo `ErrorResponse` con `correlationId`. La sección 4 no tiene código para estos casos, así que `errorCode` va vacío.
    - Validación de POST en dos capas, el patrón de Cart: los DTOs exigen `usuarioId`, `items`, `productoId` y `cantidad` con `[JsonRequired]` (si falta un campo en un Guid no nullable toma Guid.Empty), y `CreateAsync` rechaza además valores inválidos: `usuarioId` Guid.Empty explícito, `cantidad` <= 0 e items vacíos, todos 400 ORD-002
    - Llamada a Products via `IProductsClient` en `Services:ProductsApi:BaseUrl` (http://localhost:5001): 404 ORD-004 si el producto no existe, 422 ORD-005 con nombre/disponible/solicitado en el mensaje si no hay stock. El Total y el PrecioUnitario se calculan con el precio de Products, no con lo que mande el cliente
    - Health checks 5.4: `/health` (ambas sondas), `/health/ready` (tablas `orders` y `order_items`), `/health/live` (proceso). JSON `{ status }`
    - Swagger: XML comments, `[ProducesResponseType]`, ejemplos con `IExamplesProvider` + `[SwaggerRequestExample]`/`[SwaggerResponseExample]` (`Swashbuckle.AspNetCore.Filters`), el pattern de Notifications y no el OperationFilter de Products/Cart
    - Serilog: consola + JSON con Service=Orders.API. Request log con middleware propio (`Request iniciado/finalizado` + DuracionMs), calcado de Notifications
    - Correlation ID inbound y outbound: header `X-Correlation-Id`, campo `correlationId` en errores, propiedad en logs. `CorrelationIdHandler` lo copia en el GET a Products
    - Sin tests E2E, decisión de esta pasada
- **Cart.API**
    - Endpoints 4.4: GET /api/cart/{userId}, POST /api/cart/{userId}/items, PUT /api/cart/{userId}/items/{productId}, DELETE item, DELETE carrito
    - Persistencia en SQLite con Dapper (`Microsoft.Data.Sqlite`). Al iniciar, `DatabaseInitializer` crea `carts` y `cart_items` si no existen. `UsuarioId` es la PK: el Apéndice A no da un Id al carrito. `cart_items` guarda ProductoId y Cantidad; `UsuarioId` ahí es solo la clave del dueño. Archivo `carts.db` (`ConnectionStrings:DefaultConnection`)
    - POST crea el carrito si no existe y suma la cantidad si el producto ya estaba. PUT reemplaza la cantidad. Stock se valida contra ese total
    - Stock vía `IProductCatalog` / `ProductCatalogClient`: GET Products `/api/products/{id}` en `Products:BaseUrl` (http://localhost:5001). CRT-002 es un 404 de Products. CRT-003 es cantidad mayor al stock
    - Los tests de Cart no levantan Products. Reemplazan `IProductCatalog`. 
    - 404 de ruta inexistente, 405 y 415: `UseStatusCodePages` + `StatusCodeErrorWriter` devuelven el mismo `ErrorResponse` con `correlationId`. La sección 4 no tiene código para estos casos, así que `errorCode` va vacío. `SuppressMapClientErrors` evita que `[ApiController]` arme su propio ProblemDetails en el 415
    - Health checks 5.4: `/health` (ambas sondas), `/health/ready` (tablas `carts` y `cart_items`), `/health/live` (proceso). JSON `{ status }`
    - Swagger: XML comments, `[ProducesResponseType]`, `CartSwaggerExamplesFilter`. Donde un status tiene más de un caso van ejemplos nombrados, sin `example` suelto: 400 de POST y PUT (`crt-004`, `body-invalido`), 404 de POST (`crt-002`, `userid-no-guid`), de PUT (`crt-001`, `crt-002`) y de DELETE item (`crt-001`, `productid-no-guid`)
    - Tests E2E (xUnit + WebApplicationFactory + FluentAssertions). `CartApiFactory` apunta a un sqlite temporal, no a `carts.db` en la carpeta de output
    - Serilog: consola + JSON, request log, Warning/Error con errorCode, Endpoint y CorrelationId en cada evento del request
    - Correlation ID inbound y outbound: header `X-Correlation-Id`, campo `correlationId` en errores, propiedad en logs. `CorrelationIdHandler` lo copia en la llamada a Products
- **Notifications.API**
    - Endpoints 4.5: POST /api/notifications/send (201), GET /api/notifications/{userId} (404 NTF-003 si el usuario no tiene notificaciones)
    - Persistencia en SQLite con Dapper (`Microsoft.Data.Sqlite`). Al iniciar, `DatabaseInitializer` crea la tabla `notifications` si no existe. Archivo `notifications.db` (`ConnectionStrings:DefaultConnection`). El envio se simula: se guarda con Estado `Enviada` y FechaEnvio UTC
    - `ErrorCodes` (NTF-002, NTF-003, NTF-004) + excepciones de dominio (`NotFound`, `Validation`, `BusinessRule`, `Global`) con sus `IExceptionHandler`s
    - NTF-001 pendiente: como ORD-003 en Orders, falta el endpoint de Users para validar que el `UsuarioId` exista; el comentario en `NotificationService.SendNotification` marca donde va. Hoy un userId inexistente se guarda y responde 201
    - 404 de ruta inexistente, 405 y 415: `UseStatusCodePages` + `StatusCodeErrorWriter` devuelven el mismo `ErrorResponse` con `correlationId`. La sección 4 no tiene código para estos casos, así que `errorCode` va vacío.
    - Health checks 5.4: `/health` (ambas sondas), `/health/ready` (tabla `notifications`), `/health/live` (proceso). JSON `{ status }`
    - Swagger: XML comments, `[ProducesResponseType]`, ejemplos con `IExamplesProvider` + `[SwaggerRequestExample]`/`[SwaggerResponseExample]`. Orders copia este pattern
    - Serilog: consola + JSON con Service=Notifications.API. Request log con middleware propio (`Request iniciado/finalizado` + DuracionMs)
    - Correlation ID inbound: header `X-Correlation-Id`, campo `correlationId` en errores, propiedad en logs. Notifications no llama a nadie, no hay outbound
    - Tests E2E (xUnit + WebApplicationFactory + FluentAssertions): los errores de contrato NTF-003/NTF-004 con su `correlationId` en header y body, y el manejo del header inbound (lo genera si falta, lo trimmea si viene con espacios)

## Swagger / OpenAPI

Detalle que me dio dolor de cabeza:
Swashbuckle.AspNetCore **10.2.3** trae Microsoft.OpenApi **2.7.5**.
En varios lados de internet asignan `response.Content["application/json"].Example = ...`:
https://stackoverflow.com/questions/67860252/how-to-use-dependency-injection-with-swaggeroperationfilter
Eso no compila: `Content` y `Example` en las interfaces son get-only.
La guia de migracion a Swashbuckle v10 dice usar el tipo concreto y escribir ahi:
https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/docs/migrating-to-v10.md 

## Capturas de Swagger

Las capturas de Swagger UI con las respuestas de error (`errorCode` y `errorMessage`) están en [`docs/capturas/`](docs/capturas/). Todavía no hay capturas de Orders.API.

## Logging

El TP pide estos usos de log:

- **Request HTTP** - un evento al iniciar (`HTTP {método} {path} started`,
  middleware del Correlation ID en Products, Users y Cart; Orders y Notifications
  usan middleware propio con `Request iniciado/finalizado`) y otro al terminar
  (`UseSerilogRequestLogging`, o el cierre del middleware propio): método, path, status y duración.
- **Reglas de negocio** - `LogWarning`
- **Excepción no contemplada** - `LogError`
- **Todo evento logeado debe incluir el endpoint que lo genero**
- **Correlation ID** en cada evento del request

## Correlation ID

Implementado en las 5 APIs. Inbound en todas; outbound en Cart.API y Orders.API (hacia Products) y en Products.API (hacia Orders, PRD-004). Header: `X-Correlation-Id`.

- Si el cliente manda un valor no vacío (después de trim), se reutiliza. No tiene que ser un Guid.
- Si falta o viene en blanco, se genera un Guid.
- El mismo valor sale en el header de respuesta, en los logs (`CorrelationId`) y en el campo `correlationId` de cualquier error 4xx/5xx.
- Probar: `GET http://localhost:5001/api/products` o `POST http://localhost:5002/api/users/login` con y sin el header. Un 401 o un 404 también debe repetir el id en el body.

Cart.API y Orders.API copian el header en `GET /api/products/{id}` con `CorrelationIdHandler`, y Products.API en `GET /api/orders` con el suyo, para PRD-004. Users y Notifications no llaman a nadie.

## Health Checks

Spec 5.4, en Products.API, Users.API, Cart.API, Orders.API y Notifications.API.

- `GET /health` corre los dos chequeos y devuelve el peor estado.
- `GET /health/ready` solo el de SQLite (tag `ready`).
- `GET /health/live` solo el del proceso (tag `live`).
- Body: `{ "status": "Healthy" | "Degraded" | "Unhealthy" }`.
- Unhealthy sale con 503. Healthy y Degraded con 200.

La prueba de ready abre `ConnectionStrings:DefaultConnection` y busca la tabla del servicio: `products` en `products.db`, `users` en `users.db`, `carts` y `cart_items` en `carts.db`, `orders` y `order_items` en `orders.db`, `notifications` en `notifications.db`.
Healthy significa que ese archivo ya tenía la tabla, o que el startup acaba de crearla.
Si el archivo abre pero no tiene esa tabla, el chequeo devuelve Unhealthy. Si no se puede abrir, también.
Aclaración: con el proceso corriendo, SQLite reutiliza conexiones del pool sobre el archivo ya abierto; si se borra o renombra el `.db` en caliente, ready puede seguir diciendo Healthy un rato. El Unhealthy está garantizado en el arranque en frío (directorio roto o tabla inexistente), que es lo que importa al desplegar.
Un directorio que no existe no tira abajo el host: el startup atrapa el error y ready queda Unhealthy. Live sigue contestando.
No hay realmente razon para Degraded en este projecto, por lo chico.

Probar: `GET http://localhost:5001/health` y `GET http://localhost:5002/health`, más `/health/ready` y `/health/live` en cada uno.

## Desvíos justificados del TP

Decisiones donde la implementación se aparta de una lectura literal de la guía, cada una con su nota en el código:

- **ORD-003 y NTF-001 no se disparan.** La guía pide 404 si el `UsuarioId` no existe en Users, pero no define el endpoint interno de Users para consultarlo, y leer la base de otro microservicio rompe el silo. El comentario en `OrdersService.CreateAsync` y en `NotificationService.SendNotification` marca exactamente dónde va la llamada cuando el endpoint exista.
- **`errorCode` vacío en los 404 de ruta inexistente, 405 y 415.** La sección 3 del TP exige el contrato en todo 4xx, pero la sección 4 no define ningún código de catálogo para errores del framework; tomar uno prestado diría otra cosa. Escriben estos casos `StatusCodeErrorWriter` en las cinco APIs.
- **`cantidad <= 0` y `usuarioId` vacío responden ORD-002.** El Apéndice A pide `Cantidad` "mayor a 0" y el catálogo de Orders no tiene un código propio tipo CRT-004, así que el valor inválido cae en "datos inválidos". El Guid.Empty además no es un usuario a buscar en Users: es un dato inválido, por eso es ORD-002 y no ORD-003.
- **El PUT de estado devuelve la orden completa, no el body de ejemplo de la guía.** El ejemplo muestra `fechaActualizacion`, un campo que el Apéndice A define solo para el carrito, no para la orden: el ejemplo contradice al propio UML.
- **Los `errorMessage` usan las formas cortas de la tabla.** Los textos largos que ilustra la guía están marcados "sugerido".

## Tecnologías previstas

- .NET 10
- ASP.NET Core Web API
- Swagger / OpenAPI (Swashbuckle)
- Serilog
- Health Checks
- IExceptionHandler
- SQLite (`Microsoft.Data.Sqlite`) y Dapper. Patrón de persistencia tomado del ejemplo de la cátedra: [toDoList-2026, branch `feature/completo`](https://github.com/cai-uba/toDoList-2026/tree/feature/completo)
