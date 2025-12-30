using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShoppingCart.Domain.Interfaces;

namespace ShoppingCart.Infrastructure.Services
{
    /// <summary>
    /// Notification service with retry logic for reliable notification delivery.
    /// Integrates with Lab2 notification system via HTTP API.
    /// </summary>
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
            var retryCount = 0;
            Exception? lastException = null;

            while (retryCount < MaxRetries)
            {
                try
                {
                    var notificationRequest = new
                    {
                        Content = $"Twoje zamówienie {orderId} o wartości {totalAmount:C} zostało przyjęte do realizacji.",
                        Channel = "Email",
                        Recipient = userId,
                        TimeZone = "UTC",
                        ScheduledAt = DateTime.UtcNow.AddMinutes(1),
                        Priority = 5
                    };

                    var response = await _httpClient.PostAsJsonAsync(
                        _notificationApiUrl, 
                        notificationRequest, 
                        cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation(
                            "Notification sent successfully for order {OrderId} to user {UserId}", 
                            orderId, 
                            userId);
                        return;
                    }

                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "Notification API returned status {StatusCode}: {Content}", 
                        response.StatusCode, 
                        errorContent);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(
                        ex,
                        "Failed to send notification (attempt {Attempt}/{MaxRetries}) for order {OrderId}", 
                        retryCount + 1, 
                        MaxRetries, 
                        orderId);
                }

                retryCount++;
                if (retryCount < MaxRetries)
                {
                    // Exponential backoff: 1s, 2s, 4s
                    var delayMs = (int)Math.Pow(2, retryCount - 1) * 1000;
                    await Task.Delay(delayMs, cancellationToken);
                }
            }

            _logger.LogError(
                lastException,
                "Failed to send notification after {MaxRetries} attempts for order {OrderId}", 
                MaxRetries, 
                orderId);
        }
    }
}
