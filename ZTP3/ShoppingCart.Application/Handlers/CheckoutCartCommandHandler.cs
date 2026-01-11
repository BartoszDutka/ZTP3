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
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);
                    if (cart == null || cart.IsCheckedOut || !cart.Items.Any())
                        return null;

                    Guid orderId = await _orderService.CreateOrderFromCartAsync(
                        cart.Id, 
                        cart.UserId, 
                        cart.Items, 
                        cancellationToken);

                    cart.IsCheckedOut = true;
                    cart.UpdatedAt = DateTime.UtcNow;
                    await _cartRepository.UpdateAsync(cart, cancellationToken);

                    return orderId;
                }
                catch (ConcurrencyException)
                {
                    if (attempt >= MaxRetries - 1)
                        throw;
                    await Task.Delay(100, cancellationToken);
                }
            }

            return null;
        }
    }
}
