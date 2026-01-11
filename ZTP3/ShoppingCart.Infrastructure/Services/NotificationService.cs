using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShoppingCart.Domain.Interfaces;

namespace ShoppingCart.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NotificationService> _logger;
        private readonly string _notificationApiUrl;
        private const int MaxRetries = 3;

        public NotificationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<NotificationService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _notificationApiUrl = configuration["NotificationService:ApiUrl"] 
                ?? "http://localhost:5000/api/notifications";
        }

        public async Task SendOrderCreatedNotificationAsync(
            string userId, 
            Guid orderId, 
            decimal totalAmount, 
            CancellationToken cancellationToken = default)
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var request = new
                    {
                        Content = $"Twoje zamówienie {orderId} o wartości {totalAmount:C} zostało przyjęte do realizacji.",
                        Channel = "Email",
                        Recipient = userId,
                        TimeZone = "UTC",
                        ScheduledAt = DateTime.UtcNow.AddMinutes(1),
                        Priority = 5
                    };

                    HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                        _notificationApiUrl, request, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Notification sent for order {OrderId}", orderId);
                        return;
                    }

                    _logger.LogWarning("Notification failed with status {Status}", response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send notification (attempt {Attempt})", attempt);
                    
                    if (attempt == MaxRetries)
                    {
                        _logger.LogError("Giving up on notification for order {OrderId}", orderId);
                        return;
                    }
                }

                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
