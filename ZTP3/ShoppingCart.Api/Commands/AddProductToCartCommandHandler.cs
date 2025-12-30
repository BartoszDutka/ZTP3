using MediatR;
using MongoDB.Driver;
using ShoppingCart.Api.Data;
using ShoppingCart.Api.Models;
using ShoppingCart.Api.Services;

namespace ShoppingCart.Api.Commands
{
    public class AddProductToCartCommandHandler : IRequestHandler<AddProductToCartCommand, bool>
    {
        private readonly ShoppingCartDbContext _context;
        private readonly IProductService _productService;

        public AddProductToCartCommandHandler(ShoppingCartDbContext context, IProductService productService)
        {
            _context = context;
            _productService = productService;
        }

        public async Task<bool> Handle(AddProductToCartCommand request, CancellationToken cancellationToken)
        {
            var cart = await _context.Carts
                .Find(c => c.Id == request.CartId)
                .FirstOrDefaultAsync(cancellationToken);

            if (cart == null || cart.IsCheckedOut)
            {
                return false;
            }

            var product = await _productService.GetProductByIdAsync(request.ProductId);
            if (product == null)
            {
                return false;
            }

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            
            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                });
            }

            cart.UpdatedAt = DateTime.UtcNow;
            
            var filter = Builders<Cart>.Filter.Eq(c => c.Id, cart.Id);
            await _context.Carts.ReplaceOneAsync(filter, cart, cancellationToken: cancellationToken);

            return true;
        }
    }
}
