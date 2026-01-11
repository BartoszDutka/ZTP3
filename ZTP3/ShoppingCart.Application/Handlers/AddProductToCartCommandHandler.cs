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
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    Cart cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);
                    if (cart == null || cart.IsCheckedOut)
                        return false;

                    var product = await _productService.GetProductByIdAsync(request.ProductId);
                    if (product == null)
                        return false;

                    var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
                    if (item != null)
                    {
                        item.Quantity += request.Quantity;
                        item.ReservationExpiresAt = DateTime.UtcNow.AddMinutes(15);
                    }
                    else
                    {
                        cart.Items.Add(new CartItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = request.ProductId,
                            Quantity = request.Quantity,
                            AddedAt = DateTime.UtcNow,
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
                    if (attempt >= MaxRetries - 1)
                        throw;
                    
                    await Task.Delay(100, cancellationToken);
                }
            }

            return false;
        }
    }
}
