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
        private const int CheckIntervalMinutes = 5;
        private const int InactivityMinutes = 15;

        public CartCleanupService(
            ILogger<CartCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cart cleanup started");
            await Task.Delay(10000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupCarts(stoppingToken);
                    await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cleanup error");
                }
            }

            _logger.LogInformation("Cart cleanup stopped");
        }

        private async Task CleanupCarts(CancellationToken token)
        {
            using var scope = _serviceProvider.CreateScope();
            ICartRepository repo = scope.ServiceProvider.GetRequiredService<ICartRepository>();

            DateTime threshold = DateTime.UtcNow.AddMinutes(-InactivityMinutes);
            List<Cart> carts = await repo.GetInactiveCartsAsync(threshold, token);

            if (!carts.Any())
                return;

            _logger.LogInformation("Cleaning {Count} inactive carts", carts.Count);

            foreach (var cart in carts)
            {
                try
                {
                    DateTime now = DateTime.UtcNow;
                    var expired = cart.Items.Where(i => i.ReservationExpiresAt < now).ToList();
                    
                    foreach (var item in expired)
                        cart.Items.Remove(item);

                    if (!cart.Items.Any())
                    {
                        await repo.DeleteAsync(cart.Id, token);
                        _logger.LogDebug("Deleted cart {CartId}", cart.Id);
                    }
                    else
                    {
                        cart.UpdatedAt = now;
                        await repo.UpdateAsync(cart, token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cleanup cart {CartId}", cart.Id);
                }
            }
        }
    }
}
