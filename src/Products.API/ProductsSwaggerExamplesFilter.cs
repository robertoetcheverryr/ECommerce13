using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Products.API.Controllers;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Products.API;

// 5.1 asks for XML comments + examples following 4. XML <example> on Models/DTOs
// already gives schema examples. This IOperationFilter is here so Swagger UI
// can show a full request/response body per HTTP status (200 vs 404/PRD-001 vs
// 409/PRD-003), which schema examples cannot do.
//
// Bodies are built from Product/DTOs + ErrorCodes, then serialized. XML <example>
// tags cannot be composed into a full JSON document at runtime. type/title/detail
// for 404/400/500 still match the IExceptionHandlers (no shared catalog for those).
/// <summary>
/// Attach request/response examples from the contracts to each Products operation.
/// </summary>
public class ProductsSwaggerExamplesFilter : IOperationFilter
{
    private static readonly Guid SampleProductId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Product SampleProduct = new()
    {
        Id = SampleProductId,
        Nombre = "Notebook Dell XPS 15",
        Descripcion = "Laptop 15 pulgadas, 32GB RAM",
        Precio = 1500.00m,
        Stock = 10,
        Categoria = "Electrónica",
        FechaCreacion = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
    };

    private static readonly CreateProductRequest SampleCreateRequest = new()
    {
        Nombre = SampleProduct.Nombre,
        Descripcion = SampleProduct.Descripcion,
        Precio = SampleProduct.Precio,
        Stock = SampleProduct.Stock,
        Categoria = SampleProduct.Categoria
    };

    private static readonly UpdateProductRequest SampleUpdateRequest = new()
    {
        Nombre = SampleProduct.Nombre,
        Descripcion = "Laptop 15 pulgadas, 64GB RAM",
        Precio = 1750.00m,
        Stock = 8,
        Categoria = SampleProduct.Categoria
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var productPath = $"/api/products/{SampleProductId}";

        switch (context.MethodInfo.Name)
        {
            case nameof(ProductsController.GetAll):
                SetResponseExample(operation, "200", ToJson(new[] { SampleProduct }));
                SetResponseExample(operation, "500", ToJson(InternalError("/api/products")));
                break;

            case nameof(ProductsController.GetById):
                SetResponseExample(operation, "200", ToJson(SampleProduct));
                SetResponseExample(operation, "404", ToJson(NotFound("/api/products/99")));
                SetResponseExample(operation, "500", ToJson(InternalError(productPath)));
                break;

            case nameof(ProductsController.Create):
                SetRequestExample(operation, ToJson(SampleCreateRequest));
                SetResponseExample(operation, "201", ToJson(SampleProduct));
                SetResponseExample(operation, "400", ToJson(BadRequest("/api/products")));
                SetResponseExample(operation, "409", ToJson(Conflict(
                    "/api/products",
                    ErrorCodes.PRD_003,
                    string.Format(ErrorCodes.PRD_003_Message, SampleProduct.Categoria),
                    ErrorCodes.PRD_003_Detail)));
                SetResponseExample(operation, "500", ToJson(InternalError("/api/products")));
                break;

            case nameof(ProductsController.Update):
                var updated = new Product
                {
                    Id = SampleProduct.Id,
                    Nombre = SampleUpdateRequest.Nombre,
                    Descripcion = SampleUpdateRequest.Descripcion,
                    Precio = SampleUpdateRequest.Precio,
                    Stock = SampleUpdateRequest.Stock,
                    Categoria = SampleUpdateRequest.Categoria,
                    FechaCreacion = SampleProduct.FechaCreacion
                };
                SetRequestExample(operation, ToJson(SampleUpdateRequest));
                SetResponseExample(operation, "200", ToJson(updated));
                SetResponseExample(operation, "400", ToJson(BadRequest(productPath)));
                SetResponseExample(operation, "404", ToJson(NotFound(productPath)));
                SetResponseExample(operation, "500", ToJson(InternalError(productPath)));
                break;

            case nameof(ProductsController.Delete):
                SetResponseExample(operation, "404", ToJson(NotFound(productPath)));
                SetResponseExample(operation, "409", ToJson(Conflict(
                    productPath,
                    ErrorCodes.PRD_004,
                    ErrorCodes.PRD_004_Message,
                    ErrorCodes.PRD_004_Detail)));
                SetResponseExample(operation, "500", ToJson(InternalError(productPath)));
                break;
            default:
                if (context.MethodInfo.DeclaringType == typeof(ProductsController))
                {
                    throw new InvalidOperationException(
                        $"No Swagger examples defined for {context.MethodInfo.Name}.");
                }
                break;
        }
    }

    private static ErrorResponse NotFound(string instance) => new()
    {
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        Title = "Not Found",
        Status = 404,
        Detail = "El recurso solicitado no fue encontrado.",
        Instance = instance,
        ErrorCode = ErrorCodes.PRD_001,
        ErrorMessage = ErrorCodes.PRD_001_Message
    };

    private static ErrorResponse BadRequest(string instance) => new()
    {
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        Title = "Bad Request",
        Status = 400,
        Detail = "Los datos enviados no son válidos.",
        Instance = instance,
        ErrorCode = ErrorCodes.PRD_002,
        ErrorMessage = "El nombre es obligatorio.; El precio debe ser mayor a 0."
    };

    private static ErrorResponse Conflict(
        string instance,
        string errorCode,
        string errorMessage,
        string detail) => new()
    {
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.9",
        Title = "Conflict",
        Status = 409,
        Detail = detail,
        Instance = instance,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };

    private static ErrorResponse InternalError(string instance) => new()
    {
        Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        Title = "Internal Server Error",
        Status = 500,
        Detail = "Ocurrió un error inesperado.",
        Instance = instance,
        ErrorCode = ErrorCodes.PRD_005,
        ErrorMessage = ErrorCodes.PRD_005_Message
    };

    private static string ToJson(object value) => JsonSerializer.Serialize(value, JsonOptions);

    private static void SetRequestExample(OpenApiOperation operation, string json)
    {
        if (operation.RequestBody?.Content is null)
            return;

        if (!operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
            return;

        AttachExample(mediaType, json);
    }

    private static void SetResponseExample(OpenApiOperation operation, string statusCode, string json)
    {
        if (operation.Responses is null ||
            !operation.Responses.TryGetValue(statusCode, out var response))
            return;

        // Do not assign response.Content: on modern Microsoft.OpenApi versions that property has no setter.
        if (response.Content is null)
            return;

        if (!response.Content.TryGetValue("application/json", out var mediaType))
        {
            mediaType = new OpenApiMediaType();
            response.Content["application/json"] = mediaType;
        }

        AttachExample(mediaType, json);
    }

    private static void AttachExample(OpenApiMediaType mediaType, string json)
    {
        var node = JsonNode.Parse(json);

        if (mediaType.Examples is not null)
        {
            mediaType.Examples["default"] = new OpenApiExample { Value = node };
            return;
        }

        mediaType.Example = node;
    }
}