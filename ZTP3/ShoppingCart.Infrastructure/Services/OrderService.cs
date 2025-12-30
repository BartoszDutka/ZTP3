using ShoppingCart.Domain.Entities;
using ShoppingCart.Domain.Interfaces;

namespace ShoppingCart.Infrastructure.Services
{
    /// <summary>
    /// Order module service - handles all order-related operations.
    /// Separated from Cart module - communicates through IOrderService interface.
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductService _productService;
        private readonly INotificationService _notificationService;

        public OrderService(
            IOrderRepository orderRepository,
            IProductService productService,
            INotificationService notificationService)
        {
            _orderRepository = orderRepository;
            _productService = productService;
            _notificationService = notificationService;
        }

        public async Task<Guid> CreateOrderFromCartAsync(
            Guid cartId, 
            string userId, 
            List<CartItem> cartItems, 
            CancellationToken cancellationToken = default)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CartId = cartId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>()
            };

            decimal totalAmount = 0;

            // Fetch current prices from product service
            foreach (var item in cartItems)
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
                    UnitPrice = product.Price, // Current price from product service
                    TotalPrice = product.Price * item.Quantity
                };

                order.Items.Add(orderItem);
                totalAmount += orderItem.TotalPrice;
            }

            order.TotalAmount = totalAmount;

            // Save order to database
            await _orderRepository.CreateAsync(order, cancellationToken);

            // Send notification about order creation (with retry logic in notification service)
            // Fire and forget - notification failure should not block order creation
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.SendOrderCreatedNotificationAsync(
                        userId, 
                        order.Id, 
                        totalAmount, 
                        CancellationToken.None);
                }
                catch
                {
                    // Logged in notification service
                }
            }, cancellationToken);

            return order.Id;
        }
    }
}
