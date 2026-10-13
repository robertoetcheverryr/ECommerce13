using System.ComponentModel.DataAnnotations;
using Users.API.Services;

namespace Users.API.DTOs;

/// <summary>
/// Valida la contraseña delegando en <see cref="Password"/>.
/// </summary>
public sealed class ValidPasswordAttribute : ValidationAttribute
{
    public ValidPasswordAttribute()
        : base("La contraseña no es válida.")
    {
    }

    /// <inheritdoc />
    public override bool IsValid(object? value)
        => value is string text && Password.IsValid(text);
}
