using MediatR;
using ShoppingCart.Application.Commands;
using ShoppingCart.Domain.Exceptions;
using ShoppingCart.Domain.Interfaces;

namespace ShoppingCart.Application.Handlers
{
    public class RemoveProductFromCartCommandHandler : IRequestHandler<RemoveProductFromCartCommand, bool>
    {
        private readonly ICartRepository _cartRepository;
        private const int MaxRetries = 3;

        public RemoveProductFromCartCommandHandler(ICartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        public async Task<bool> Handle(RemoveProductFromCartCommand request, CancellationToken cancellationToken)
        {
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);
                    if (cart == null || cart.IsCheckedOut)
                        return false;

                    var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
                    if (item == null)
                        return false;

                    if (request.Quantity.HasValue && request.Quantity.Value > 0)
                    {
                        item.Quantity -= request.Quantity.Value;
                        if (item.Quantity <= 0)
                            cart.Items.Remove(item);
                        else
                            item.ReservationExpiresAt = DateTime.UtcNow.AddMinutes(15);
                    }
                    else
                    {
                        cart.Items.Remove(item);
                    }

                    cart.UpdatedAt = DateTime.UtcNow;
                    cart.LastActivityAt = DateTime.UtcNow;

                    await _cartRepository.UpdateAsync(cart, cancellationToken);
                    return true;
                }
                catch (ConcurrencyException)
                {
                    if (attempt >= MaxRetries - 1)
                        throw;
                    await Task.Delay(100, cancellationToken);
                }
            }

            return false;
        }
    }
}
