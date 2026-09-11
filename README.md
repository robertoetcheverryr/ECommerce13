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
Hoy solo Products.API implementa el catálogo; el resto queda documentado para cuando existan esos servicios.

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
| **USR-002** | 400 | Los datos del usuario son inválidos. | POST /register inválido |
| **USR-003** | 401 | Credenciales incorrectas. | POST /login email o password no coinciden |
| **USR-004** | 403 | Usuario bloqueado por demasiados intentos fallidos. | 3+ intentos fallidos |
| **USR-005** | 403 | Usuario bloqueado por detección de fraude. | Bloqueo manual |
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
    - Persistencia in-memory (`List<Product>`). Mientras esperamos la Lib de la catedra.
    - Validaciones con Data Annotations (PRD-002)
    - `ErrorCodes` + excepciones de dominio (`NotFound`, `Validation`, `BusinessRule` con `Detail`, `Global`)
    - `IExceptionHandler`s registrados en orden de especificidad
    - PRD-003 solo en POST. Ya que no esta en la spec, PUT no revalida unicidad nombre+categoría
    - PRD-004 vía `IActiveOrdersChecker` + `NoOpActiveOrdersChecker`. Los tests inyectan un checker que siempre devuelve true
    - Health checks básicos: `/health`, `/health/ready`, `/health/live`
    - Swagger: XML comments, `[ProducesResponseType]` con los status del contrato (incluye 500/PRD-005)
    - Ejemplos de request/response por status: `ProductsSwaggerExamplesFilter` (`IOperationFilter`)
    - Tests E2E (xUnit + WebApplicationFactory + FluentAssertions)
    - Serilog: consola + JSON, request log, Warning/Error con errorCode, Endpoint y CorrelationId en cada evento del request
    - Correlation ID (TODO outbound): header `X-Correlation-Id`, campo `correlationId` en errores, propiedad en logs
- **Users.API**
  - POST /api/users/register and POST /api/users/login stubs. 
  - Tests para 201/200 y que la password no se devuelve.
  - TODO: el resto de Users
- Orders / Cart / Notifications: solo el esqueleto
- Pendiente TP: Correlation ID outbound + resto de servicios, Users, Orders, Cart, Notifications, Healthchecks completos

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

## Tecnologías previstas

- .NET 10
- ASP.NET Core Web API
- Swagger / OpenAPI (Swashbuckle)
- Serilog
- Health Checks
- IExceptionHandler
