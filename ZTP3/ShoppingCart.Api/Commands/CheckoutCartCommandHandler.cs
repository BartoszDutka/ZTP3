using MediatR;
using MongoDB.Driver;
using ShoppingCart.Api.Data;
using ShoppingCart.Api.Models;
using ShoppingCart.Api.Services;

namespace ShoppingCart.Api.Commands
{
    public class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, Guid?>
    {
        private readonly ShoppingCartDbContext _context;
        private readonly IProductService _productService;

        public CheckoutCartCommandHandler(ShoppingCartDbContext context, IProductService productService)
        {
            _context = context;
            _productService = productService;
        }

        public async Task<Guid?> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
        {
            var cart = await _context.Carts
                .Find(c => c.Id == request.CartId)
                .FirstOrDefaultAsync(cancellationToken);

            if (cart == null || cart.IsCheckedOut || !cart.Items.Any())
            {
                return null;
            }

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                UserId = cart.UserId,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>()
            };

            decimal totalAmount = 0;

            foreach (var item in cart.Items)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                if (product == null)
                {
                    continue;
                }

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * item.Quantity
                };

                order.Items.Add(orderItem);
                totalAmount += orderItem.TotalPrice;
            }

            order.TotalAmount = totalAmount;

            cart.IsCheckedOut = true;
            cart.UpdatedAt = DateTime.UtcNow;

            await _context.Orders.InsertOneAsync(order, cancellationToken: cancellationToken);
            
            var filter = Builders<Cart>.Filter.Eq(c => c.Id, cart.Id);
            await _context.Carts.ReplaceOneAsync(filter, cart, cancellationToken: cancellationToken);

            return order.Id;
        }
    }
}
