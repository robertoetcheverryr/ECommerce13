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
    [Required]
    [ValidGuid]
    public Guid UsuarioId { get; set; }

    /// <summary>
    /// Mensaje que se enviará al usuario.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de notificación: Email, Push o SMS.
    /// </summary>
    [Required]
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
    /// Comprueba si el valor recibido es un GUID válido y no vacío.
    /// </summary>
    /// <param name="value">Valor que se desea validar.</param>
    /// <returns>True si es un GUID válido y no vacío.</returns>
    public override bool IsValid(object? value)
    {
        return value is Guid guid && guid != Guid.Empty;
    }
}