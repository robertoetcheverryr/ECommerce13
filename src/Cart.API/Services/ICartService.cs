namespace Cart.API.Services;
using Cart.API.Exceptions;
using CartModel = global::Cart.API.Models.Cart;

/// <summary>
/// Contrato del servicio de carrito.
/// Contiene la lógica de negocio y el acceso a la base SQLite.
/// </summary>
public interface ICartService
{
    /// <summary>
    /// Obtiene el carrito de un usuario.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario dueño del carrito.</param>
    /// <returns>El carrito si existe.</returns>
    /// <exception cref="NotFoundException">Cuando el usuario no tiene carrito (CRT-001).</exception>
    CartModel Get(Guid usuarioId);

    /// <summary>
    /// Agrega un producto al carrito. Crea el carrito si no existe.
    /// La cantidad se suma si el producto ya estaba.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario dueño del carrito.</param>
    /// <param name="productoId">Identificador del producto.</param>
    /// <param name="cantidad">Cantidad a agregar. Debe ser mayor a 0.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El carrito actualizado.</returns>
    /// <exception cref="ValidationException">Cuando la cantidad no es mayor a 0 (CRT-004).</exception>
    /// <exception cref="NotFoundException">Cuando el producto no existe en Products (CRT-002).</exception>
    /// <exception cref="BusinessRuleException">Cuando la cantidad supera el stock (CRT-003).</exception>
    Task<CartModel> AddItem(Guid usuarioId, Guid productoId, int cantidad, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda el carrito y reemplaza sus ítems.
    /// </summary>
    /// <param name="cart">Carrito a persistir.</param>
    /// <returns>El carrito guardado, con la fecha de actualización asignada.</returns>
    CartModel Save(CartModel cart);

    /// <summary>
    /// Elimina el carrito y sus ítems.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario dueño del carrito.</param>
    void Delete(Guid usuarioId);
}
