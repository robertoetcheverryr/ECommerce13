namespace Products.API.Exceptions;

/// <summary>
/// Catálogo de códigos de error y mensajes del microservicio Products.
/// </summary>
public static class ErrorCodes
{
    public const string PRD_001 = "PRD-001";
    public const string PRD_001_Message = "Producto no encontrado.";

    public const string PRD_002 = "PRD-002";
    public const string PRD_002_Message = "Los datos del producto son inválidos.";

    public const string PRD_003 = "PRD-003";
    public const string PRD_003_Message = "Ya existe un producto con ese nombre en la categoría '{0}'.";

    public const string PRD_004 = "PRD-004";
    public const string PRD_004_Message = "El producto tiene órdenes activas y no puede eliminarse.";

    public const string PRD_005 = "PRD-005";
    public const string PRD_005_Message = "Error interno al procesar el producto.";
}