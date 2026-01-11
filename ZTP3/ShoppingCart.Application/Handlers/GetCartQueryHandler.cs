using MediatR;
using ShoppingCart.Application.DTOs;
using ShoppingCart.Application.Queries;
using ShoppingCart.Domain.Interfaces;

namespace ShoppingCart.Application.Handlers
{
    public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto?>
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductService _productService;

        public GetCartQueryHandler(
            ICartRepository cartRepository,
            IProductService productService)
        {
            _cartRepository = cartRepository;
            _productService = productService;
        }

        public async Task<CartDto?> Handle(GetCartQuery request, CancellationToken cancellationToken)
        {
            var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);
            if (cart == null)
                return null;

            CartDto result = new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt,
                Items = new List<CartItemDto>()
            };

            decimal total = 0;
            foreach (var item in cart.Items)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                if (product == null)
                    continue;

                decimal itemTotal = product.Price * item.Quantity;
                result.Items.Add(new CartItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = itemTotal
                });
                total += itemTotal;
            }

            result.TotalAmount = total;
            return result;
        }
    }
}
