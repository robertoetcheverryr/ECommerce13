namespace Cart.API.Exceptions;

/// <summary>
/// Catálogo de códigos de error y mensajes del microservicio Cart.
/// </summary>
public static class ErrorCodes
{
    public const string CRT_001 = "CRT-001";
    public const string CRT_001_Message = "Carrito no encontrado.";

    public const string CRT_002 = "CRT-002";
    public const string CRT_002_Message = "Producto no encontrado.";

    public const string CRT_003 = "CRT-003";
    public const string CRT_003_Message = "Stock insuficiente para agregar al carrito.";
    public const string CRT_003_Detail = "No se puede procesar la solicitud.";

    public const string CRT_004 = "CRT-004";
    public const string CRT_004_Message = "Cantidad inválida.";

    public const string CRT_005 = "CRT-005";
    public const string CRT_005_Message = "Error interno al procesar el carrito.";

    // Not in the catalog: 404 for an id that is not a GUID where the spec lists no code
    // (userId on POST, productId on DELETE item). errorCode goes empty.
    public const string InvalidUserId_Message = "El userId no es un GUID válido.";
    public const string InvalidProductId_Message = "El productId no es un GUID válido.";
}
