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

### 4. Paquete OpenAPI (si hiciera falta)

```powershell
$projects = @("Products.API", "Users.API", "Orders.API", "Cart.API", "Notifications.API")
foreach ($p in $projects) {
    dotnet add "src/$p" package Microsoft.AspNetCore.OpenApi
}
```

### 5. Compilar

```powershell
dotnet build
```

Los puertos configurados son:

- Products.API → http://localhost:5001
- Users.API → http://localhost:5002
- Orders.API → http://localhost:5003
- Cart.API → http://localhost:5004
- Notifications.API → http://localhost:5005

## Estado actual

- **Commit 0**: Estructura inicial de la solución y proyectos creados.
- Próximo paso: Asistir a clase y entender pasos proximos esperados.

## Cómo ejecutar

La ejecución local de los microservicios será documentada en próximos commits.

## Tecnologías previstas

- .NET 10
- ASP.NET Core Web API
- Swagger / OpenAPI
- Serilog
- Health Checks
- IExceptionHandler
