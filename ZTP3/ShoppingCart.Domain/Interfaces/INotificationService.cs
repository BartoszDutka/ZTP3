namespace ShoppingCart.Domain.Interfaces
{
    /// <summary>
    /// Interface for sending notifications about orders.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends notification about order creation to external notification system.
        /// Implementation should handle failures with retry logic.
        /// </summary>
        Task SendOrderCreatedNotificationAsync(string userId, Guid orderId, decimal totalAmount, CancellationToken cancellationToken = default);
    }
}
