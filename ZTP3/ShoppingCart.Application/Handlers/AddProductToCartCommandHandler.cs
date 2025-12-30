using MediatR;
using ShoppingCart.Application.Commands;
using ShoppingCart.Domain.Entities;
using ShoppingCart.Domain.Exceptions;
using ShoppingCart.Domain.Interfaces;

namespace ShoppingCart.Application.Handlers
{
    public class AddProductToCartCommandHandler : IRequestHandler<AddProductToCartCommand, bool>
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductService _productService;
        private const int MaxRetries = 3;

        public AddProductToCartCommandHandler(
            ICartRepository cartRepository,
            IProductService productService)
        {
            _cartRepository = cartRepository;
            _productService = productService;
        }

        public async Task<bool> Handle(AddProductToCartCommand request, CancellationToken cancellationToken)
        {
            var retryCount = 0;

            while (retryCount < MaxRetries)
            {
                try
                {
                    var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);

                    if (cart == null || cart.IsCheckedOut)
                    {
                        return false;
                    }

                    // Verify product exists
                    var product = await _productService.GetProductByIdAsync(request.ProductId);
                    if (product == null)
                    {
                        return false;
                    }

                    var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

                    if (existingItem != null)
                    {
                        existingItem.Quantity += request.Quantity;
                        // Extend reservation by 15 minutes
                        existingItem.ReservationExpiresAt = DateTime.UtcNow.AddMinutes(15);
                    }
                    else
                    {
                        cart.Items.Add(new CartItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = request.ProductId,
                            Quantity = request.Quantity,
                            AddedAt = DateTime.UtcNow,
                            // Set 15-minute reservation
                            ReservationExpiresAt = DateTime.UtcNow.AddMinutes(15)
                        });
                    }

                    cart.UpdatedAt = DateTime.UtcNow;
                    cart.LastActivityAt = DateTime.UtcNow;

                    await _cartRepository.UpdateAsync(cart, cancellationToken);
                    return true;
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

            return false;
        }
    }
}
