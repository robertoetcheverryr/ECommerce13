namespace Users.API.Exceptions;

/// <summary>
/// Catálogo de códigos de error y mensajes del microservicio Users.
/// </summary>
public static class ErrorCodes
{
    public const string USR_001 = "USR-001";
    public const string USR_001_Message = "El email '{0}' ya está registrado.";
    public const string USR_001_Detail = "Ya existe un recurso con esos datos.";

    public const string USR_002 = "USR-002";
    public const string USR_002_Message = "Los datos del usuario son inválidos.";

    public const string USR_003 = "USR-003";
    public const string USR_003_Message = "Credenciales incorrectas.";
    public const string USR_003_Detail = "Las credenciales no son válidas.";

    public const string USR_004 = "USR-004";
    public const string USR_004_Message = "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.";
    public const string USR_004_Detail = "El acceso está prohibido.";

    public const string USR_005 = "USR-005";
    public const string USR_005_Message = "Su cuenta fue suspendida por razones de seguridad. Contacte a soporte.";
    public const string USR_005_Detail = "El acceso está prohibido.";

    public const string USR_006 = "USR-006";
    public const string USR_006_Message = "Error interno al procesar el usuario.";
}
