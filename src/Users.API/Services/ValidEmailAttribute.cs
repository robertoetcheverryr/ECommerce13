using System.ComponentModel.DataAnnotations;
using Users.API.Services;

namespace Users.API.DTOs;

/// <summary>
/// Valida el formato de email delegando en <see cref="Email"/>.
/// </summary>
public sealed class ValidEmailAttribute : ValidationAttribute
{
    public ValidEmailAttribute()
        : base("El email no tiene un formato válido.")
    {
    }

    /// <inheritdoc />
    public override bool IsValid(object? value)
        => value is string text && Email.IsValid(text);
}
