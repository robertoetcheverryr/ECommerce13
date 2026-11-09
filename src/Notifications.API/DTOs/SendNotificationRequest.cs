using System;
using System.ComponentModel.DataAnnotations;

namespace Notifications.API.DTOs;
/// <summary>
/// Representa los datos necesarios para enviar una notificación.
/// </summary>
public class SendNotificationRequest
{
    /// <summary>
    /// Identificador del usuario destinatario.
    /// </summary>
    [Required(ErrorMessage = "El UsuarioId es obligatorio.")]
    [ValidGuid]
    public Guid UsuarioId { get; set; }

    /// <summary>
    /// Mensaje que se enviará al usuario.
    /// El OpenAPI declara además "pattern": ".*\S.*" (ver
    /// Swagger/NotificationsSchemaFilter): [Required] trata "solo espacios"
    /// como ausente (IsNullOrWhiteSpace) pero el schema solo decía
    /// minLength, y Schemathesis mandaba "\u000b" como dato válido que el
    /// runtime rechazaba con NTF-002. Se declara por SchemaFilter y no por
    /// [RegularExpression] porque MVC ancla la regex y salta la cadena vacía
    /// (rompería "sin mensaje" → 201) y duplicaría el mensaje exacto que
    /// compara Newman.
    /// </summary>
    [Required(ErrorMessage = "El mensaje es obligatorio.")]
    [MaxLength(500, ErrorMessage = "El mensaje no puede superar los 500 caracteres.")]
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de notificación: Email, Push o SMS.
    /// </summary>
    [Required(ErrorMessage = "El tipo es obligatorio.")]
    [RegularExpression(
        "^(Email|Push|SMS)$",
        ErrorMessage = "El tipo debe ser Email, Push o SMS.")]
    public string Tipo { get; set; } = string.Empty;
}

/// <summary>
/// Valida que un identificador GUID no esté vacío.
/// </summary>
public class ValidGuidAttribute : ValidationAttribute
{
    /// <summary>
    /// Define el mensaje de error en español que muestra la validación.
    /// </summary>
    public ValidGuidAttribute()
        : base("El UsuarioId no tiene un formato válido.")
    {
    }

    /// <summary>
    /// Comprueba si el valor recibido es un GUID válido y no vacío.
    /// </summary>
    /// <param name="value">Valor que se desea validar.</param>
    /// <returns>True si es un GUID válido y no vacío.</returns>
    public override bool IsValid(object? value)
    {
        return value is Guid guid && guid != Guid.Empty;
    }
}