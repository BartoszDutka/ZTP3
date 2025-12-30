using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShoppingCart.Domain.Interfaces;

namespace ShoppingCart.Infrastructure.BackgroundServices
{
    public class CartCleanupService : BackgroundService
    {
        private readonly ILogger<CartCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes
        private readonly TimeSpan _inactivityThreshold = TimeSpan.FromMinutes(15); // 15 minutes inactivity

        public CartCleanupService(
            ILogger<CartCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cart Cleanup Service is starting.");

            // Wait a bit before starting to allow other services to initialize
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupInactiveCartsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up inactive carts.");
                }

                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Cart Cleanup Service is stopping.");
        }

        private async Task CleanupInactiveCartsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var cartRepository = scope.ServiceProvider.GetRequiredService<ICartRepository>();

            var inactiveThreshold = DateTime.UtcNow - _inactivityThreshold;
            var inactiveCarts = await cartRepository.GetInactiveCartsAsync(inactiveThreshold, cancellationToken);

            if (inactiveCarts.Any())
            {
                _logger.LogInformation("Found {Count} inactive carts to clean up.", inactiveCarts.Count);

                foreach (var cart in inactiveCarts)
                {
                    try
                    {
                        // Remove items with expired reservations
                        var itemsToRemove = cart.Items
                            .Where(i => i.ReservationExpiresAt.HasValue && i.ReservationExpiresAt.Value < DateTime.UtcNow)
                            .ToList();

                        foreach (var item in itemsToRemove)
                        {
                            cart.Items.Remove(item);
                            _logger.LogDebug("Removed expired item {ProductId} from cart {CartId}",
                                item.ProductId, cart.Id);
                        }

                        // If cart has no items after cleanup or hasn't been active, delete it
                        if (!cart.Items.Any())
                        {
                            await cartRepository.DeleteAsync(cart.Id, cancellationToken);
                            _logger.LogInformation("Deleted inactive cart {CartId}", cart.Id);
                        }
                        else
                        {
                            // Update cart to reflect removed items
                            cart.UpdatedAt = DateTime.UtcNow;
                            await cartRepository.UpdateAsync(cart, cancellationToken);
                            _logger.LogInformation("Updated cart {CartId} after removing expired items", cart.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error cleaning up cart {CartId}", cart.Id);
                    }
                }
            }
        }
    }
}
