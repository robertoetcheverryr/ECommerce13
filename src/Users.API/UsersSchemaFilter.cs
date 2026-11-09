using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Users.API.DTOs;

namespace Users.API;

/// <summary>
/// Declara en el OpenAPI el formato de email que [ValidEmail] ya exige en
/// runtime. Sin pattern, el schema acepta cualquier string y Schemathesis
/// mandaba emails gigantes o con caracteres de control como datos "válidos"
/// que el runtime rechazaba con USR-002 (ST202). El patrón es un subconjunto
/// práctico de lo que acepta System.Net.Mail.MailAddress: todo lo que entra
/// acá, entra en la validación real. Es declaración pura de schema: la
/// autoridad sigue siendo [ValidEmail]: la validación de Users junta los
/// errores del ModelState en el errorMessage, y un atributo duplicaría el
/// texto exacto que Newman compara, así que el patrón va solo en el schema.
/// </summary>
public class UsersSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// Patrón OpenAPI para el campo email de los requests de Users. Calibrado
    /// contra System.Net.Mail.MailAddress (la autoridad de [ValidEmail]),
    /// que rechaza: punto inicial o puntos consecutivos en el local-part, y
    /// dominio con label vacía. Todo lo que este patrón genera, MailAddress
    /// lo acepta (ver matriz en scripts/probe_email_matrix.sh).
    /// </summary>
    public const string EmailPattern =
        "^[a-zA-Z0-9_%+-](?:\\.?[a-zA-Z0-9_%+-]){0,62}@[a-zA-Z0-9-]{1,63}(?:\\.[a-zA-Z0-9-]{1,63})*\\.[a-zA-Z]{2,6}$";

    /// <summary>
    /// Al menos un carácter no vacío: misma regla que [Required] y
    /// Password.IsValid (IsNullOrWhiteSpace) en runtime, que el schema
    /// declaraba solo como minLength (Schemathesis mandaba "\n" como
    /// nombre válido → 400 USR-002, ST202). La clase de espacios cubre las
    /// de .NET y Python y el patrón sobrevive el anclado de MVC (ver
    /// UpdateOrderStatusRequest para la explicación completa).
    /// </summary>
    public const string NonBlankPattern =
        @"[\s\S]*[^\u0000-\u0020\u007f-\u009f\u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000\ufeff][\s\S]*";

    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(RegisterUserRequest) && context.Type != typeof(LoginRequest))
        {
            return;
        }

        if (schema.Properties is null)
        {
            return;
        }

        Apply(schema, "email", EmailPattern);
        Apply(schema, "password", NonBlankPattern);
        if (context.Type == typeof(RegisterUserRequest))
        {
            Apply(schema, "nombre", NonBlankPattern);
            Apply(schema, "apellido", NonBlankPattern);
        }
    }

    private static void Apply(IOpenApiSchema schema, string property, string pattern)
    {
        if (schema.Properties.TryGetValue(property, out var prop)
            && prop is OpenApiSchema propSchema)
        {
            propSchema.Pattern = pattern;
        }
    }
}
