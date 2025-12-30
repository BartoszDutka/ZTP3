using MediatR;
using MongoDB.Driver;
using ShoppingCart.Api.Data;
using ShoppingCart.Api.DTOs;
using ShoppingCart.Api.Services;

namespace ShoppingCart.Api.Queries
{
    public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto?>
    {
        private readonly ShoppingCartDbContext _context;
        private readonly IProductService _productService;

        public GetCartQueryHandler(ShoppingCartDbContext context, IProductService productService)
        {
            _context = context;
            _productService = productService;
        }

        public async Task<CartDto?> Handle(GetCartQuery request, CancellationToken cancellationToken)
        {
            var cart = await _context.Carts
                .Find(c => c.Id == request.CartId)
                .FirstOrDefaultAsync(cancellationToken);

            if (cart == null)
            {
                return null;
            }

            var cartDto = new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt,
                Items = new List<CartItemDto>()
            };

            decimal totalAmount = 0;

            foreach (var item in cart.Items)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                if (product == null)
                {
                    continue;
                }

                var itemDto = new CartItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * item.Quantity
                };

                cartDto.Items.Add(itemDto);
                totalAmount += itemDto.TotalPrice;
            }

            cartDto.TotalAmount = totalAmount;

            return cartDto;
        }
    }
}
