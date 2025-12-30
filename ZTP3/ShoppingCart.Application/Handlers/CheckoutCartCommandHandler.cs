using MediatR;
using ShoppingCart.Application.Commands;
using ShoppingCart.Domain.Entities;
using ShoppingCart.Domain.Exceptions;
using ShoppingCart.Domain.Interfaces;

namespace ShoppingCart.Application.Handlers
{
    public class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, Guid?>
    {
        private readonly ICartRepository _cartRepository;
        private readonly IOrderService _orderService;
        private const int MaxRetries = 3;

        public CheckoutCartCommandHandler(
            ICartRepository cartRepository,
            IOrderService orderService)
        {
            _cartRepository = cartRepository;
            _orderService = orderService;
        }

        public async Task<Guid?> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
        {
            var retryCount = 0;

            while (retryCount < MaxRetries)
            {
                try
                {
                    var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);

                    if (cart == null || cart.IsCheckedOut || !cart.Items.Any())
                    {
                        return null;
                    }

                    // Create order through OrderService interface (module separation)
                    var orderId = await _orderService.CreateOrderFromCartAsync(
                        cart.Id, 
                        cart.UserId, 
                        cart.Items, 
                        cancellationToken);

                    // Mark cart as checked out - this prevents double checkout
                    cart.IsCheckedOut = true;
                    cart.UpdatedAt = DateTime.UtcNow;
                    
                    // Update cart with optimistic locking
                    await _cartRepository.UpdateAsync(cart, cancellationToken);

                    return orderId;
                }
                catch (ConcurrencyException)
                {
                    retryCount++;
                    if (retryCount >= MaxRetries)
                    {
                        throw;
                    }
                    // Small delay before retry
                    await Task.Delay(100 * retryCount, cancellationToken);
                }
            }

            return null;
        }
    }
}
