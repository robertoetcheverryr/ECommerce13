using Microsoft.OpenApi;
using Notifications.API.DTOs;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Notifications.API.Swagger;

/// <summary>
/// Declara en el OpenAPI la regla de whitespace que [Required] ya aplica en
/// runtime sobre Mensaje: MVC trata "solo espacios" como ausente
/// (IsNullOrWhiteSpace) pero el schema solo declaraba minLength:1, así que
/// Schemathesis mandaba mensajes de un carácter de control ("\u000b") como
/// datos válidos que el runtime rechazaba con NTF-002 (ST202).
/// Es declaración pura de schema: la autoridad sigue siendo [Required].
/// </summary>
public class NotificationsSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// Al menos un carácter no vacío ("pattern" en OpenAPI busca en cualquier
    /// posición). La clase negada cubre los espacios de .NET y de Python y el
    /// patrón sobrevive el anclado de MVC: ver UpdateOrderStatusRequest.
    /// </summary>
    public const string NonBlankPattern =
        @"[\s\S]*[^\u0009-\u000D\u0020\u0085\u00A0\u1680\u2000-\u200A\u2028\u2029\u202F\u205F\u3000][\s\S]*";

    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(SendNotificationRequest))
        {
            return;
        }

        if (schema.Properties is not null
            && schema.Properties.TryGetValue("mensaje", out var mensaje)
            && mensaje is OpenApiSchema mensajeSchema)
        {
            mensajeSchema.Pattern = NonBlankPattern;
        }
    }
}
