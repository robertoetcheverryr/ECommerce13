namespace Orders.API.Exceptions;

/// <summary>
/// Contiene los códigos y mensajes de error de Orders API.
/// </summary>
public static class ErrorCodes
{
    // Los textos largos que muestra la guía para cada código están marcados como
    // "sugerido"; acá se usan las formas cortas de la tabla de códigos de error.

    /// <summary>
    /// Código de orden no encontrada.
    /// </summary>
    public const string ORD_001 = "ORD-001";

    /// <summary>
    /// Mensaje de orden no encontrada.
    /// </summary>
    public const string ORD_001_Message = "Orden no encontrada.";

    /// <summary>
    /// Código de datos de orden inválidos.
    /// </summary>
    public const string ORD_002 = "ORD-002";

    /// <summary>
    /// Mensaje de datos de orden inválidos.
    /// </summary>
    public const string ORD_002_Message = "Los datos de la orden son inválidos.";

    /// <summary>
    /// Código de usuario no encontrado. Su validación está pendiente.
    /// </summary>
    public const string ORD_003 = "ORD-003";

    /// <summary>
    /// Mensaje de usuario no encontrado.
    /// </summary>
    public const string ORD_003_Message = "Usuario no encontrado.";

    /// <summary>
    /// Código de producto no encontrado.
    /// </summary>
    public const string ORD_004 = "ORD-004";

    /// <summary>
    /// Mensaje de producto no encontrado.
    /// </summary>
    public const string ORD_004_Message = "Producto no encontrado.";

    /// <summary>
    /// Código de stock insuficiente.
    /// </summary>
    public const string ORD_005 = "ORD-005";

    /// <summary>
    /// Mensaje de stock insuficiente.
    /// </summary>
    public const string ORD_005_Message = "Stock insuficiente.";

    /// <summary>
    /// Código de transición de estado inválida.
    /// </summary>
    public const string ORD_006 = "ORD-006";

    /// <summary>
    /// Mensaje de transición de estado inválida.
    /// </summary>
    public const string ORD_006_Message = "Transición de estado inválida.";

    /// <summary>
    /// Código de error interno.
    /// </summary>
    public const string ORD_007 = "ORD-007";

    /// <summary>
    /// Mensaje de error interno.
    /// </summary>
    public const string ORD_007_Message = "Error interno al procesar la orden.";
}