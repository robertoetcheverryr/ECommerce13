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
- Users / Orders / Cart / Notifications: solo el esqueleto
- Pendiente TP: Serilog, Correlation ID, Users, Orders, Cart, Notifications, Healthchecks completos

## Swagger / OpenAPI

Detalle que me dio dolor de cabeza:
Swashbuckle.AspNetCore **10.2.3** trae Microsoft.OpenApi **2.7.5**.
En varios lados de internet asignan `response.Content["application/json"].Example = ...`:
https://stackoverflow.com/questions/67860252/how-to-use-dependency-injection-with-swaggeroperationfilter
Eso no compila: `Content` y `Example` en las interfaces son get-only.
La guia de migracion a Swashbuckle v10 dice usar el tipo concreto y escribir ahi:
https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/docs/migrating-to-v10.md 

## Tecnologías previstas

- .NET 10
- ASP.NET Core Web API
- Swagger / OpenAPI (Swashbuckle)
- Serilog (TODO)
- Health Checks
- IExceptionHandler
