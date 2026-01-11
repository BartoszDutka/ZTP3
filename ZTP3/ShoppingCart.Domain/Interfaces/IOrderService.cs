using ShoppingCart.Domain.Entities;

namespace ShoppingCart.Domain.Interfaces
{
    public interface IOrderService
    {
        Task<Guid> CreateOrderFromCartAsync(Guid cartId, string userId, List<CartItem> cartItems, CancellationToken cancellationToken = default);
    }
}
