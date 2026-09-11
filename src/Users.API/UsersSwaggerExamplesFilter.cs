using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Users.API.Controllers;
using Users.API.DTOs;
using Users.API.Exceptions;

namespace Users.API;

/// <summary>
/// Attach request/response examples from the contracts to each Users operation.
/// </summary>
public class UsersSwaggerExamplesFilter : IOperationFilter
{
    private static readonly Guid SampleUserId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");
    private static readonly string SampleCorrelationId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    private const string SampleEmail = "maria@email.com";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly RegisterUserRequest SampleRegisterRequest = new()
    {
        Nombre = "María",
        Apellido = "González",
        Email = SampleEmail,
        Password = "MiPassword123!"
    };

    private static readonly LoginRequest SampleLoginRequest = new()
    {
        Email = SampleEmail,
        Password = "MiPassword123!"
    };

    private static readonly UserResponse SampleRegisteredUser = new()
    {
        Id = SampleUserId,
        Nombre = "María",
        Apellido = "González",
        Email = SampleEmail,
        FechaRegistro = new DateTime(2024, 3, 10, 9, 0, 0, DateTimeKind.Utc),
        Activo = true
    };

    private static readonly LoginResponse SampleLoggedInUser = new()
    {
        Id = SampleUserId,
        Nombre = "María",
        Apellido = "González",
        Email = SampleEmail
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        switch (context.MethodInfo.Name)
        {
            case nameof(UsersController.Register):
                SetRequestExample(operation, ToJson(SampleRegisterRequest));
                SetResponseExample(operation, "201", ToJson(SampleRegisteredUser));
                SetResponseExample(operation, "400", ToJson(BadRequest("/api/users/register")));
                SetResponseExample(operation, "409", ToJson(Conflict(
                    "/api/users/register",
                    ErrorCodes.USR_001,
                    string.Format(ErrorCodes.USR_001_Message, SampleEmail),
                    ErrorCodes.USR_001_Detail)));
                SetResponseExample(operation, "500", ToJson(InternalError("/api/users/register")));
                break;

            case nameof(UsersController.Login):
                SetRequestExample(operation, ToJson(SampleLoginRequest));
                SetResponseExample(operation, "200", ToJson(SampleLoggedInUser));
                SetResponseExample(operation, "400", ToJson(BadRequest("/api/users/login")));
                SetResponseExample(operation, "401", ToJson(Unauthorized("/api/users/login")));
                SetResponseExample(operation, "403", ToJson(Forbidden(
                    "/api/users/login",
                    ErrorCodes.USR_004,
                    ErrorCodes.USR_004_Message)));
                SetResponseExample(operation, "500", ToJson(InternalError("/api/users/login")));
                break;

            default:
                if (context.MethodInfo.DeclaringType == typeof(UsersController))
                {
                    throw new InvalidOperationException(
                        $"No Swagger examples defined for {context.MethodInfo.Name}.");
                }
                break;
        }
    }

    private static ErrorResponse BadRequest(string instance) => new()
    {
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        Title = "Bad Request",
        Status = 400,
        Detail = "Los datos enviados no son válidos.",
        Instance = instance,
        ErrorCode = ErrorCodes.USR_002,
        ErrorMessage = ErrorCodes.USR_002_Message,
        CorrelationId = SampleCorrelationId
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
        ErrorMessage = errorMessage,
        CorrelationId = SampleCorrelationId
    };

    private static ErrorResponse Unauthorized(string instance) => new()
    {
        Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
        Title = "Unauthorized",
        Status = 401,
        Detail = ErrorCodes.USR_003_Detail,
        Instance = instance,
        ErrorCode = ErrorCodes.USR_003,
        ErrorMessage = ErrorCodes.USR_003_Message,
        CorrelationId = SampleCorrelationId
    };

    private static ErrorResponse Forbidden(
        string instance,
        string errorCode,
        string errorMessage) => new()
    {
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
        Title = "Forbidden",
        Status = 403,
        Detail = ErrorCodes.USR_004_Detail,
        Instance = instance,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        CorrelationId = SampleCorrelationId
    };

    private static ErrorResponse InternalError(string instance) => new()
    {
        Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        Title = "Internal Server Error",
        Status = 500,
        Detail = "Ocurrió un error inesperado.",
        Instance = instance,
        ErrorCode = ErrorCodes.USR_006,
        ErrorMessage = ErrorCodes.USR_006_Message,
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
