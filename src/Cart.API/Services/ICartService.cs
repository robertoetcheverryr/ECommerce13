namespace Cart.API.Services;
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
    /// <returns>El carrito si existe; null si no hay fila.</returns>
    CartModel? Get(Guid usuarioId);

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
