using MediatR;
using ShoppingCart.Application.Commands;
using ShoppingCart.Domain.Entities;
using ShoppingCart.Domain.Interfaces;

namespace ShoppingCart.Application.Handlers
{
    public class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, Guid?>
    {
        private readonly ICartRepository _cartRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IProductService _productService;

        public CheckoutCartCommandHandler(
            ICartRepository cartRepository,
            IOrderRepository orderRepository,
            IProductService productService)
        {
            _cartRepository = cartRepository;
            _orderRepository = orderRepository;
            _productService = productService;
        }

        public async Task<Guid?> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
        {
            var cart = await _cartRepository.GetByIdAsync(request.CartId, cancellationToken);

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

            await _orderRepository.CreateAsync(order, cancellationToken);
            await _cartRepository.UpdateAsync(cart, cancellationToken);

            return order.Id;
        }
    }
}
