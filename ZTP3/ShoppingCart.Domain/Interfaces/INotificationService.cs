namespace ShoppingCart.Domain.Interfaces
{
    public interface INotificationService
    {
        Task SendOrderCreatedNotificationAsync(string userId, Guid orderId, decimal totalAmount, CancellationToken cancellationToken = default);
    }
}
