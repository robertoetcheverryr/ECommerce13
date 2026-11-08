using System.Text.RegularExpressions;

namespace Orders.API;

/// <summary>
/// System.Text.Json reporta sus errores en inglés y con nombres de tipos internos
/// (System.Guid, Orders.API.DTOs...). Se reconocen por la clave de ModelState y se
/// reemplazan por textos en espanol. Los mensajes de Data Annotations pasan tal cual.
/// Mismo traductor que ProductsController y CartController.
/// </summary>
internal static class ValidationMessages
{
    internal static IEnumerable<string> ToSpanishMessages(string key, string message)
    {
        // Empty key: there was no body at all (or a literal null)
        if (key == string.Empty)
            return ["El cuerpo de la solicitud es obligatorio."];

        // "$" is the JSON root: either [JsonRequired] fields are missing or the JSON is broken
        if (key == "$")
        {
            const string missingRequired = "missing required properties including:";
            var index = message.IndexOf(missingRequired, StringComparison.Ordinal);
            if (index < 0)
                return ["El cuerpo de la solicitud no es un JSON válido."];

            // The message ends with "including: 'usuarioId', 'items'." keep only the quoted field names,
            // the type name is quoted too but it comes before "including:"
            return Regex.Matches(message[(index + missingRequired.Length)..], "'([^']+)'")
                .Select(match => $"El campo '{match.Groups[1].Value}' es obligatorio.");
        }

        // "$.items[0].productoId": the value sent for that field could not be read (e.g. "abc" for a Guid)
        if (key.StartsWith("$.", StringComparison.Ordinal))
            return [$"El campo '{key[2..]}' tiene un formato inválido."];

        // Any other key is a property validated by Data Annotations, already in Spanish
        return [message];
    }
}
