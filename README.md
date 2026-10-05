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

## Códigos de error

Todas las respuestas 4xx/5xx usan Problem Details más `errorCode` y `errorMessage`.  
Hoy Products.API y Users.API implementan el catálogo; el resto queda documentado para cuando existan esos servicios.

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
| **ORD-002** | 400 | Los datos de la orden son inválidos. | POST inválido o items vacíos |
| **ORD-003** | 404 | Usuario no encontrado al crear la orden. | UsuarioId no existe en Users |
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
| **NTF-001** | 404 | Usuario no encontrado. | UsuarioId no existe en Users |
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
│   └── Products.API.Tests/
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
```

Puertos configurados:

- Products.API → http://localhost:5001
- Users.API → http://localhost:5002
- Orders.API → http://localhost:5003
- Cart.API → http://localhost:5004
- Notifications.API → http://localhost:5005

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
    - PRD-003 solo en POST. Ya que no esta en la spec, PUT no revalida unicidad nombre+categoría
    - PRD-004 vía `IActiveOrdersChecker` + `NoOpActiveOrdersChecker`. Los tests inyectan un checker que siempre devuelve true
    - Health checks 5.4: `/health` (ambas sondas), `/health/ready` (tabla `products`), `/health/live` (proceso). JSON `{ status }`
    - Swagger: XML comments, `[ProducesResponseType]` con los status del contrato (incluye 500/PRD-005)
    - Ejemplos de request/response por status: `ProductsSwaggerExamplesFilter` (`IOperationFilter`)
    - Tests E2E (xUnit + WebApplicationFactory + FluentAssertions)
    - Serilog: consola + JSON, request log, Warning/Error con errorCode, Endpoint y CorrelationId en cada evento del request
    - Correlation ID (TODO outbound): header `X-Correlation-Id`, campo `correlationId` en errores, propiedad en logs
- **Users.API**
    - Endpoints 4.2: POST /api/users/register, POST /api/users/login. No hay GET ni lock/unlock de admin
    - Persistencia in-memory (`List<User>`). Mientras esperamos la Lib de la catedra
    - Validaciones con Data Annotations (USR-002). Email y password delegan en `Services/Email` y `Services/Password`
    - `ErrorCodes` + excepciones de dominio (`NotFound`, `Validation`, `BusinessRule` con `Detail`, `Global`)
    - `IExceptionHandler`s registrados en orden de especificidad
    - USR-003 no distingue email inexistente de password incorrecta para evitar dar informacion a un atacante
    - USR-004: al tercer fallo consecutivo `Activo` pasa a false y ese request sigue siendo USR-003. El login siguiente es 403. Un login correcto antes resetea `IntentosFallidos`
    - USR-005: `Activo == false` con `IntentosFallidos < 3`. No hay endpoint para marcarlo; los tests usan `UserService.MarkManuallyBlocked`
    - PasswordHash nunca sale en register ni login
    - Health checks básicos: `/health`, `/health/ready`, `/health/live`
    - Swagger: XML comments, `[ProducesResponseType]`, `UsersSwaggerExamplesFilter`. El 403 de ejemplo es USR-004; USR-005 va como ejemplo nombrado
    - Tests E2E (xUnit + WebApplicationFactory + FluentAssertions)
    - Serilog: consola + JSON, request log, Warning/Error con errorCode, Endpoint y CorrelationId en cada evento del request
    - Correlation ID inbound (TODO outbound, no hay HttpClient): header `X-Correlation-Id`, campo `correlationId` en errores, propiedad en logs
- Orders / Cart / Notifications: solo el esqueleto
- Pendiente TP: Correlation ID outbound + resto de servicios, Orders, Cart, Notifications, health checks en los otros cuatro

## Swagger / OpenAPI

Detalle que me dio dolor de cabeza:
Swashbuckle.AspNetCore **10.2.3** trae Microsoft.OpenApi **2.7.5**.
En varios lados de internet asignan `response.Content["application/json"].Example = ...`:
https://stackoverflow.com/questions/67860252/how-to-use-dependency-injection-with-swaggeroperationfilter
Eso no compila: `Content` y `Example` en las interfaces son get-only.
La guia de migracion a Swashbuckle v10 dice usar el tipo concreto y escribir ahi:
https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/docs/migrating-to-v10.md 

## Logging

El TP pide estos usos de log:

- **Request HTTP** - un solo evento al completar la request
  (`UseSerilogRequestLogging`): método, path, status y duración.
  No hay un log de inicio y otro de fin; el spec pide esos datos,
  Serilog los emite juntos cuando termina el request.
- **Reglas de negocio** - `LogWarning`
- **Excepción no contemplada** - `LogError`
- **Todo evento logeado debe incluir el endpoint que lo genero**
- **Correlation ID** en cada evento del request

## Correlation ID

Implementado en Products.API (inbound). Header: `X-Correlation-Id`.

- Si el cliente manda un valor no vacío (después de trim), se reutiliza. No tiene que ser un Guid.
- Si falta o viene en blanco, se genera un Guid.
- El mismo valor sale en el header de respuesta, en los logs (`CorrelationId`) y en el campo `correlationId` de cualquier error 4xx/5xx.
- Probar: `GET http://localhost:5001/api/products` con y sin el header. Un 404 también debe repetir el id en el body.

Todavía no está: propagación outbound por `HttpClient` (Products no llama a nadie)

## Health Checks

Spec 5.4, por ahora solo Products.API.

- `GET /health` corre las dos sondas y devuelve el peor estado.
- `GET /health/ready` solo la de SQLite (tabla `products`, tag `ready`).
- `GET /health/live` solo la del proceso (tag `live`).
- Body: `{ "status": "Healthy" | "Degraded" | "Unhealthy" }`.
- Unhealthy sale con 503. Healthy y Degraded con 200.

La prueba de ready abre `ConnectionStrings:DefaultConnection` (`Data Source=products.db`) y busca la tabla `products`.
Healthy significa que ese archivo ya tenía la tabla, o que el startup acaba de crearla.
Si el archivo abre pero no tiene `products`, la sonda devuelve Unhealthy. Si no se puede abrir, también.
No hay realmente razon para Degraded en este projecto, por lo chico.

Probar: `GET http://localhost:5001/health`, `/health/ready` y `/health/live`.

## Tecnologías previstas

- .NET 10
- ASP.NET Core Web API
- Swagger / OpenAPI (Swashbuckle)
- Serilog
- Health Checks
- IExceptionHandler
- SQLite (`Microsoft.Data.Sqlite`) y Dapper. Patrón de persistencia tomado del ejemplo de la cátedra: [toDoList-2026, branch `feature/completo`](https://github.com/cai-uba/toDoList-2026/tree/feature/completo)
