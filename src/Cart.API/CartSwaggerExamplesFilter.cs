using System.Text.Json;
using System.Text.Json.Nodes;
using Cart.API.Controllers;
using Cart.API.DTOs;
using Cart.API.Exceptions;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cart.API;

/// <summary>
/// Adjunta ejemplos de request y response del contrato a cada operación del carrito.
/// </summary>
public class CartSwaggerExamplesFilter : IOperationFilter
{
    private static readonly Guid SampleUserId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");
    private static readonly Guid SampleProductId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    private static readonly string SampleCorrelationId = SampleProductId.ToString();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly CartResponse SampleCart = new()
    {
        UsuarioId = SampleUserId,
        FechaActualizacion = new DateTime(2024, 3, 10, 10, 45, 0, DateTimeKind.Utc),
        Items =
        [
            new CartItemResponse { ProductoId = SampleProductId, Cantidad = 2 }
        ]
    };

    private static readonly AddCartItemRequest SampleAddRequest = new()
    {
        ProductoId = SampleProductId,
        Cantidad = 2
    };

    private static readonly UpdateCartItemRequest SampleUpdateRequest = new()
    {
        Cantidad = 4
    };

    /// <summary>
    /// Agrega los ejemplos del contrato a la operación documentada.
    /// </summary>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var cartPath = $"/api/cart/{SampleUserId}";
        var itemPath = $"{cartPath}/items/{SampleProductId}";

        switch (context.MethodInfo.Name)
        {
            case nameof(CartController.Get):
                SetResponseExample(operation, "200", ToJson(SampleCart));
                SetResponseExample(operation, "404", ToJson(NotFound(cartPath, ErrorCodes.CRT_001, ErrorCodes.CRT_001_Message)));
                SetResponseExample(operation, "500", ToJson(InternalError(cartPath)));
                break;

            case nameof(CartController.AddItem):
                SetRequestExample(operation, ToJson(SampleAddRequest));
                SetResponseExample(operation, "200", ToJson(SampleCart));
                SetResponseExample(operation, "400", ToJson(BadRequest($"{cartPath}/items")));
                SetResponseExample(operation, "404", ToJson(NotFound($"{cartPath}/items", ErrorCodes.CRT_002, ErrorCodes.CRT_002_Message)));
                SetResponseExample(operation, "422", ToJson(Stock($"{cartPath}/items")));
                SetResponseExample(operation, "500", ToJson(InternalError($"{cartPath}/items")));
                break;

            case nameof(CartController.UpdateItem):
                SetRequestExample(operation, ToJson(SampleUpdateRequest));
                SetResponseExample(operation, "200", ToJson(SampleCart));
                SetResponseExample(operation, "400", ToJson(BadRequest(itemPath)));
                SetResponseExample(operation, "404", ToJson(NotFound(itemPath, ErrorCodes.CRT_001, ErrorCodes.CRT_001_Message)));
                SetResponseExample(operation, "422", ToJson(Stock(itemPath)));
                SetResponseExample(operation, "500", ToJson(InternalError(itemPath)));
                break;

            case nameof(CartController.RemoveItem):
                SetResponseExample(operation, "404", ToJson(NotFound(itemPath, ErrorCodes.CRT_001, ErrorCodes.CRT_001_Message)));
                SetResponseExample(operation, "500", ToJson(InternalError(itemPath)));
                break;

            case nameof(CartController.Clear):
                SetResponseExample(operation, "404", ToJson(NotFound(cartPath, ErrorCodes.CRT_001, ErrorCodes.CRT_001_Message)));
                SetResponseExample(operation, "500", ToJson(InternalError(cartPath)));
                break;
        }
    }

    private static ErrorResponse NotFound(string instance, string errorCode, string errorMessage) => new()
    {
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        Title = "Not Found",
        Status = 404,
        Detail = "El recurso solicitado no fue encontrado.",
        Instance = instance,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        CorrelationId = SampleCorrelationId
    };

    private static ErrorResponse BadRequest(string instance) => new()
    {
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        Title = "Bad Request",
        Status = 400,
        Detail = "Los datos enviados no son válidos.",
        Instance = instance,
        ErrorCode = ErrorCodes.CRT_004,
        ErrorMessage = ErrorCodes.CRT_004_Message,
        CorrelationId = SampleCorrelationId
    };

    private static ErrorResponse Stock(string instance) => new()
    {
        Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
        Title = "Unprocessable Entity",
        Status = 422,
        Detail = ErrorCodes.CRT_003_Detail,
        Instance = instance,
        ErrorCode = ErrorCodes.CRT_003,
        ErrorMessage = "Stock insuficiente. Disponible: 1, solicitado: 5.",
        CorrelationId = SampleCorrelationId
    };

    private static ErrorResponse InternalError(string instance) => new()
    {
        Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        Title = "Internal Server Error",
        Status = 500,
        Detail = "Ocurrió un error inesperado.",
        Instance = instance,
        ErrorCode = ErrorCodes.CRT_005,
        ErrorMessage = ErrorCodes.CRT_005_Message,
        CorrelationId = SampleCorrelationId
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
