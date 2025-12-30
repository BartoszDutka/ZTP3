using ShoppingCart.Domain.Entities;

namespace ShoppingCart.Domain.Interfaces
{
    /// <summary>
    /// Interface for Order module operations.
    /// This provides clear separation between Cart and Order modules.
    /// Cart module communicates with Order module only through this interface.
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Creates a new order from cart items.
        /// This is the only way Cart module communicates with Order module.
        /// </summary>
        Task<Guid> CreateOrderFromCartAsync(Guid cartId, string userId, List<CartItem> cartItems, CancellationToken cancellationToken = default);
    }
}
