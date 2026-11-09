using Microsoft.OpenApi;
using Products.API.DTOs;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Products.API;

/// <summary>
/// Ajusta el OpenAPI de los requests de producto a la verdad del runtime.
/// El [Range] del precio declara como tope decimal.MaxValue exacto (ese valor
/// en sí es un precio válido, lo fija el test de Newman "Línea exacta en
/// decimal.MaxValue es válida"). Al serializar, ese tope se convierte al
/// double más cercano que es 7.9228162514264338e28, ligeramente MAYOR que
/// decimal.MaxValue: Schemathesis prueba el maximum declarado y ese JSON ya
/// no entra en decimal, así que el request "válido" moría en la
/// deserialización con PRD-002 (ST202). Acá se declara como maximum el double
/// más grande que sí entra en decimal. Sobredesclar el schema es inocuo:
/// Schemathesis solo exige aceptar datos que el schema acepta.
/// </summary>
public class ProductsSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// Double más grande que, serializado como JSON, cabe en decimal
    /// (7.9228162514246738e28, escrito con sus dígitos exactos; Microsoft
    /// .OpenApi 2.x guarda los números como string).
    /// </summary>
    public const string PrecioMaximoDeclarado = "79228162514246740000000000000";

    /// <summary>
    /// Al menos un carácter no vacío: [Required] trata "solo espacios" como
    /// ausente (IsNullOrWhiteSpace) y el schema solo decía minLength
    /// (Schemathesis mandaba "   " como nombre/categoría válidos → 400
    /// PRD-002, ST202).
    /// </summary>
    public const string NonBlankPattern =
        @"[\s\S]*[^\u0000-\u0020\u007f-\u009f\u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000\ufeff][\s\S]*";

    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(CreateProductRequest) && context.Type != typeof(UpdateProductRequest))
        {
            return;
        }

        if (schema.Properties is null)
        {
            return;
        }

        if (schema.Properties.TryGetValue("precio", out var precio)
            && precio is OpenApiSchema precioSchema)
        {
            precioSchema.Maximum = PrecioMaximoDeclarado;
        }

        foreach (var field in new[] { "nombre", "categoria" })
        {
            if (schema.Properties.TryGetValue(field, out var prop)
                && prop is OpenApiSchema propSchema)
            {
                propSchema.Pattern = NonBlankPattern;
            }
        }
    }
}
