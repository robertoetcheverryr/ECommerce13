namespace Users.API.ExceptionHandlers;

/// <summary>
/// Sección "ErrorHandling" de appsettings.
/// Spec 5.2: el nivel de detalle del error depende del entorno.
/// </summary>
public class ErrorHandlingOptions
{
    public const string SectionName = "ErrorHandling";

    /// <summary>
    /// true en Development: detail lleva exception.Message. Nunca el stack trace.
    /// </summary>
    public bool IncludeExceptionDetails { get; set; }
}
